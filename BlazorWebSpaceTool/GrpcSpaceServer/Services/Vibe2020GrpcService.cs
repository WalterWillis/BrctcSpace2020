using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using BrctcSpace;
using System;
using GrpcSpaceServer.Services.Interfaces;
using System.Linq;
using BrctcSpaceLibrary;
using BrctcSpaceLibrary.Vibe2020Programs;
using System.IO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Security.AccessControl;

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
            return Task.FromResult(new DeviceStatus() { GyroStatus = _dataService.isGyroValid() });
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
                //Not used yet
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
            string filename = request.RunAccelerometer ? AccelerometerOnly.FileName : "Not Used Yet";

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                _logger.LogInformation($"Request: {request}");
                long startIndex = request.DataSetStart * request.SegmentSize;
                long endIndex = startIndex + (request.Rows * request.SegmentSize);
                int rows = request.Rows;

                _logger.LogInformation($"FileStream size: {fs.Length}. Start Index: {startIndex}. End Index: {endIndex}");
                if (startIndex > fs.Length)
                {
                    throw new IndexOutOfRangeException("Requested starting index is larger than the file's size.");
                }

                if (endIndex > fs.Length)
                {
                    long bytesLeft = fs.Length - startIndex;
                    rows = (int)(bytesLeft / request.SegmentSize); //not likely a long at this point
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
    }
}
