using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using BrctcSpace;
using System;

namespace GrpcSpaceServer
{
    public class Vibe2020Service : Vibe.VibeBase
    {
        private readonly ILogger<Vibe2020Service> _logger;
        public Vibe2020Service(ILogger<Vibe2020Service> logger)
        {
            _logger = logger;
        }

        public override Task<ResultReply> GetResultSet(ResultRequest request, ServerCallContext context)
        {
            
            //create switch case
            return Task.FromResult(GetFullResults(request.ScaleAccelerometer));
        }

        public override async Task GetResultStream(ResultRequest request, IServerStreamWriter<ResultReply> responseStream, ServerCallContext context)
        {
            Int64 counter = 0;
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    ResultReply resultReply = GetFullResults(request.ScaleAccelerometer);

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

        private ResultReply GetFullResults(bool scaleAccelerometer)
        {
            ResultReply results = new ResultReply();
            results.ResultSet = new ResultSet();
            
            ResultStatus status = GetAccelerometerResults(ref results, scaleAccelerometer);
            status |= GetGyroscopeResults(ref results);
            status |= GetRTCResults(ref results);
            status |= GetCPUTemperatureResults(ref results);

            // Convert to integer for transmission
            results.ResultStatus = (int)status;

            return results;
        }

        private ResultStatus GetAccelerometerResults(ref ResultReply results, bool scaleAccelerometer)
        {
            Device.Accelerometer accelerometerDevice = new Device.Accelerometer();
            ResultStatus status;

            try
            {
                results.ResultSet.AccelerometerResults = accelerometerDevice.GetAccelerometerResults(scaleAccelerometer);
                status = ResultStatus.AccelerometerSuccess;
            }
            catch
            {
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
            catch
            {
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
            catch
            {
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
            catch
            {
                status = ResultStatus.CpuTempReadFailure;
            }

            return status;
        }
    }
}
