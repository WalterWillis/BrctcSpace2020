using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using BrctcSpace;
using System;
using GrpcSpaceServer.Services.Interfaces;
using BrctcSpaceLibrary.Vibe2020Programs;
using System.IO;
using System.Collections.Generic;
using BrctcSpaceLibrary.Device;

namespace GrpcSpaceServer.Services
{
    public class Vibe2020GrpcService : Vibe.VibeBase
    {
        private readonly ILogger<Vibe2020GrpcService> _logger;
        private readonly IVibe2020DataService _dataService;

        public Vibe2020GrpcService(ILogger<Vibe2020GrpcService> logger, IVibe2020DataService dataService)
        {
            _logger = logger;
            _dataService = dataService;
        }

        public override Task<DeviceDataModel> GetSingleDeviceData(DeviceDataRequest request, ServerCallContext context)
        {
            _dataService.GetSingleReading();

            return Task.FromResult(_dataService.GetSingleReading());
        }

        public override async Task StreamDeviceData(DeviceDataRequest request, IServerStreamWriter<DeviceDataModel> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    DeviceDataModel resultReply = _dataService.GetSingleReading();

                    if (context.CancellationToken.IsCancellationRequested)
                        context.CancellationToken.ThrowIfCancellationRequested();
                    await responseStream.WriteAsync(resultReply);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Stream cancelled by user request.");
            }
        }

        public override Task<DeviceDataArray> GetBulkDeviceData(DeviceDataRequest request, ServerCallContext context)
        {
            var response = new DeviceDataArray();
            response.Items.AddRange(_dataService.GetReadings(request.DataIterations));
            return Task.FromResult(response);
        }

        public override Task<DeviceStatus> GetDeviceStatus(DeviceStatusRequest request, ServerCallContext context)
        {
            return Task.FromResult(new DeviceStatus() { GyroStatus = _dataService.IsGyroValid() });
        }

        public override Task<SingleDeviceResponse> RunTimedProgram(SingleDeviceRequest request, ServerCallContext context)
        {
            ISingleDevice program = null;

            //If multple devices are set, only accelerometer will run
            if (request.RunAccelerometer)
            {
                _logger.LogInformation($"Running AccelerometerOnly program for {request.MinutesToRun} minutes.");
                program = new AccelerometerOnly(request.UseCustomeADC);
            }
            else if (request.RunGyroscope)
            {               
                _logger.LogInformation($"Running GyroscopeOnly program for {request.MinutesToRun} minutes.");
                program = new GyroscopeOnly();
            }

            program.Run(request.MinutesToRun, context.CancellationToken);

            var response = new SingleDeviceResponse();

            response.DataSets = program.GetDataSetCount();
            response.SegmentSize = program.GetSegmentLength();

            return Task.FromResult(response);
        }

        public override Task<DeviceDataArray> GetProgramResults(ProgramPageRequest request, ServerCallContext context)
        {
            List<DeviceDataModel> modelList = new List<DeviceDataModel>();

            var response = new DeviceDataArray();
            string filename = request.RunAccelerometer ? AccelerometerOnly.FileName : GyroscopeOnly.FileName;

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                _logger.LogInformation($"Request: {request}");
                long startIndex = request.DataSetStart * request.SegmentSize;
                long endIndex = startIndex + (request.Rows * request.SegmentSize);
                long rows = request.Rows;

                _logger.LogInformation($"FileStream size: {fs.Length}. Start Index: {startIndex}. End Index: {endIndex}");
                if (startIndex > fs.Length)
                {
                    throw new IndexOutOfRangeException("Requested starting index is larger than the file's size.");
                }

                if (endIndex > fs.Length)
                {
                    long bytesLeft = fs.Length - startIndex;
                    rows = (bytesLeft / request.SegmentSize);
                }

                fs.Seek(startIndex, SeekOrigin.Begin);

                for (long i = 0; i < rows; i++)
                {
                    byte[] bytes = new byte[request.SegmentSize];

                    fs.Read(bytes, 0, request.SegmentSize);

                    DeviceDataModel model = new DeviceDataModel();

                    if (request.RunAccelerometer)
                    {
                        int[] accelData = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>(bytes).ToArray();

                        model.AccelData.Add(accelData);
                    }
                    else if (request.RunGyroscope)
                    {
                        int[] gyroData = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>(bytes).ToArray();

                        model.GyroData.Add(gyroData);
                    }

                    modelList.Add(model);
                }
            }

            response.Items.AddRange(modelList);

            return Task.FromResult(response);
        }

        public override Task<GyroReply> SetGyroRegister(GyroRegisterData request, ServerCallContext context)
        {
            var response = new GyroReply();
            _dataService.SetGyroRegister((byte)request.Register, (short)request.Value);
            response.Result = _dataService.GetGyroRegister((byte)request.Register);
            return Task.FromResult(response);
        }

        /// <summary>
        /// Cycles through the list of registers, returning registers successfully read with their values
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<GyroRegisterList> GetGyroRegisters(GyroRegisterList request, ServerCallContext context)
        {
            var response = new GyroRegisterList();

            foreach(var register in request.RegisterList)
            {
                try
                {
                    response.RegisterList.Add(new GyroRegisterData()
                    {
                        Register = register.Register,
                        Value = _dataService.GetGyroRegister((byte)register.Register)
                    });
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Cannot read register {register.Register}.");
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                }
            }

            return Task.FromResult(response);
        }

        public override Task<FullSystemResponse> RunFullSystemSharedRTC(FullSystemRequest request, ServerCallContext context)
        {
           
            FullSystemSharedRTC program = new FullSystemSharedRTC(request.UseCustomADC, true);

            _logger.LogInformation($"Running FullSystemSharedRTC program for {request.MinutesToRun} minutes.");
            try
            {
                program.Run(request.MinutesToRun, context.CancellationToken);
            }
            catch(Exception ex)
            {
                _logger.LogError("Error running FullSystemSharedRTC");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
            }

            var response = new FullSystemResponse();

            response.AccelDataSets = program.AccelDataSetCounter;
            response.GyroDataSets = program.GyroDataSetCounter;
            response.AccelSegmentSize = program.AccelSegmentLength;
            response.GyroSegmentSize = program.GyroSegmentLength;

            return Task.FromResult(response);
        }

        public override Task<DeviceDataArray> GetFullSystemResults(ProgramPageRequest request, ServerCallContext context)
        {
            List<DeviceDataModel> modelList = new List<DeviceDataModel>();

            var response = new DeviceDataArray();
            string filename = request.RunAccelerometer ? FullSystemSharedRTC.TestAccelFile : FullSystemSharedRTC.TestGyroFile;

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {      
                long startIndex = request.DataSetStart * request.SegmentSize;
                long endIndex = startIndex + (request.Rows * request.SegmentSize);
                long rows = request.Rows;

              
                if (startIndex > fs.Length)
                {
                    _logger.LogInformation($"Request: {request}");
                    _logger.LogInformation($"FileStream size: {fs.Length}. Start Index: {startIndex}. End Index: {endIndex}");
                    throw new IndexOutOfRangeException("Requested starting index is larger than the file's size.");
                }

                if (endIndex > fs.Length)
                {
                    long bytesLeft = fs.Length - startIndex;
                    rows = (bytesLeft / request.SegmentSize);
                }

                fs.Seek(startIndex, SeekOrigin.Begin);

                for (long i = 0; i < rows; i++)
                {
                    byte[] bytes = new byte[request.SegmentSize];

                    fs.Read(bytes, 0, request.SegmentSize);

                    DeviceDataModel model = new DeviceDataModel();
                    const int accelBytes = 12;
                    const int gyroBytes = 20;
                    const int rtcBytes = 8;
                    const int cpuBytes = 8;

                    if (request.RunAccelerometer)
                    {
                        Span<byte> data = bytes;
                        Span<byte> accelSegment = data.Slice(0, accelBytes);
                        Span<byte> rtcSegment = data.Slice(accelBytes, rtcBytes);
                        Span<byte> cpuSegment = data.Slice(accelBytes + rtcBytes, cpuBytes);
                        model.AccelData.Add(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>(accelSegment).ToArray());
                        model.TransactionTime = BitConverter.ToInt64(rtcSegment);
                        model.CpuTemp = BitConverter.ToDouble(cpuSegment);
                    }
                    else if (request.RunGyroscope)
                    {
                        Span<byte> data = bytes;
                        Span<byte> gyroSegment = data.Slice(0, gyroBytes);
                        Span<byte> rtcSegment = data.Slice(gyroBytes, rtcBytes);
                        Span<byte> cpuSegment = data.Slice(gyroBytes + rtcBytes, cpuBytes);
                        model.GyroData.Add(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>(gyroSegment).ToArray());
                        model.TransactionTime = BitConverter.ToInt64(rtcSegment);
                        model.CpuTemp = BitConverter.ToDouble(cpuSegment);
                    }

                    modelList.Add(model);
                }
            }

            response.Items.AddRange(modelList);

            return Task.FromResult(response);
        }

        public override Task<UartMessage> SendUartMessage(UartMessage request, ServerCallContext context)
        {
            UartMessage returnMessage = new UartMessage() { Message = "Success" };
            try
            {
                using (var comms = new UART())
                {
                    comms.SerialSend(request.Message);
                }
            }
            catch (Exception ex)
            {
                returnMessage.Message = "Failed";
                _logger.LogError("UART communication failure.");
                _logger.LogInformation($"Available Ports: {string.Join(',', UART.GetPorts())}");
                _logger.LogInformation(ex.Message);
                _logger.LogInformation(ex.StackTrace);
            }
            return Task.FromResult(returnMessage);
        }
    }
}
