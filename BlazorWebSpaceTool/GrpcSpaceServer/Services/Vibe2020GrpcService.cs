using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using BrctcSpace;
using System;
using GrpcSpaceServer.Services.Interfaces;
using System.Threading;
using BrctcSpaceLibrary;

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

            LogGyroRegisters();

            //Start the data thread
            //_dataService.Initialize();
        }

        public override Task<ResultReply> GetResultSet(ResultRequest request, ServerCallContext context)
        {            
            //create switch case
            return Task.FromResult(GetFullResults());
        }

        public override async Task GetResultStream(ResultRequest request, IServerStreamWriter<ResultReply> responseStream, ServerCallContext context)
        {
            Int64 counter = 0;
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    ResultReply resultReply = GetFullResults();

                    _logger.LogInformation($"Sending response #{counter++}");

                    if (context.CancellationToken.IsCancellationRequested)
                        context.CancellationToken.ThrowIfCancellationRequested();
                    await responseStream.WriteAsync(resultReply);
                }
            }
            catch(OperationCanceledException)
            {
                _logger.LogInformation("Stream cancelled by user request.");
            }
        }

        public override Task<DeviceDataArray> PollVibe2020DataService(DeviceDataRequest request, ServerCallContext context)
        {
            var response = new DeviceDataArray();
            _dataService.Initialize(request.UseAccelerometer, request.UseGyroscope, request.UseRtc, request.UseCpuTemperature);
            response.Items.AddRange(_dataService.GetData());
            return Task.FromResult(response);
        }


        private ResultReply GetFullResults()
        {
            ResultReply results = new ResultReply();
            results.ResultSet = new ResultSet();
            
            ResultStatus status = GetAccelerometerResults(ref results);
            status |= GetGyroscopeResults(ref results);
            status |= GetRTCResults(ref results);
            status |= GetCPUTemperatureResults(ref results);

            // Convert to integer for transmission
            results.ResultStatus = (int)status;

            return results;
        }

        private ResultStatus GetAccelerometerResults(ref ResultReply results)
        {
            Device.Accelerometer accelerometerDevice = new Device.Accelerometer();
            ResultStatus status;

            try
            {
                results.ResultSet.AccelerometerResults = accelerometerDevice.GetAccelerometerResults();
                status = ResultStatus.AccelerometerSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error with Accelerometer");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                status = ResultStatus.AccelerometerFailure;
            }

            return status;
        }

        private ResultStatus GetGyroscopeResults(ref ResultReply results)
        {
            Device.Gyroscope gyroscopeDevice = new Device.Gyroscope();
            results.ResultSet.GyroscopeResults = new GyroscopeResults();
            ResultStatus status;

            try
            {
                results.ResultSet.GyroscopeResults.BurstResults = gyroscopeDevice.GetBurstResults();
                status = ResultStatus.GyroscopeSuccess;
            }
            catch(Exception ex)
            {
                _logger.LogError("Error with Gyro");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                status = ResultStatus.GyroscopeFailure;
            }

            return status;
        }

        private ResultStatus GetRTCResults(ref ResultReply results)
        {
            Device.RTC rtcDevice = new Device.RTC();
            ResultStatus status;

            try
            {
                results.ResultSet.CurrentTime = rtcDevice.GetTimeStamp();
                status = ResultStatus.RTCSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error with RTC");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                //Timestamp must be in UTC
                results.ResultSet.CurrentTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
                status = ResultStatus.RTCFailure;
            }

            return status;
        }

        private ResultStatus GetCPUTemperatureResults(ref ResultReply results)
        {
            ResultStatus status;

            try
            {
                results.ResultSet.CpuTemperature = new Iot.Device.CpuTemperature.CpuTemperature().Temperature.Fahrenheit;
                if(!double.IsNaN(results.ResultSet.CpuTemperature))
                    status = ResultStatus.CpuTempReadSuccess;
                else
                    status = ResultStatus.CpuTempReadFailure;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error with CPU Temp");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                status = ResultStatus.CpuTempReadFailure;
            }

            return status;
        }

        private void LogGyroRegisters()
        {
            try
            {
                Device.Gyroscope gyroscopeDevice = new Device.Gyroscope();
                _logger.LogInformation($"Control Register MSC: {gyroscopeDevice.RegisterRead(Device.Gyroscope.Register.MSC_CTRL)}");
                _logger.LogInformation($"Control Register FLTR: {gyroscopeDevice.RegisterRead(Device.Gyroscope.Register.FLTR_CTRL)}");
                _logger.LogInformation($"Control Register DECR: {gyroscopeDevice.RegisterRead(Device.Gyroscope.Register.DEC_RATE)}");

                _logger.LogInformation($"Product ID: {gyroscopeDevice.RegisterRead(Device.Gyroscope.Register.PROD_ID)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error with Logging Gyro Registers");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
            }
        }
    }
}
