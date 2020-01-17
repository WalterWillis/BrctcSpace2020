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

        public ResultReply GetFullResults(bool scaleAccelerometer)
        {
            Device.Accelerometer accelerometerDevice = new Device.Accelerometer();
            Device.Gyroscope gyroscopeDevice = new Device.Gyroscope();
            Device.RTC rtcDevice = new Device.RTC();

            ResultReply results = new ResultReply();

            ResultStatus status = ResultStatus.Unknown;
            results.ResultSet.GyroscopeResults = new GyroscopeResults();

            try
            {
                results.ResultSet.AccelerometerResults = accelerometerDevice.GetAccelerometerResults(scaleAccelerometer);
            }
            catch
            {
                status |= ResultStatus.AccelerometerFailure;
            }

            try
            {
                results.ResultSet.GyroscopeResults.BurstResults = gyroscopeDevice.GetBurstResults();
            }
            catch
            {
                status |= ResultStatus.GyroscopeFailure;
            }

            try
            {
                results.ResultSet.CurrentTime = rtcDevice.GetTimeStamp();
            }
            catch
            {
                results.ResultSet.CurrentTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.Now);
                status |= ResultStatus.RTCFailure;
            }

            try
            {
                results.ResultSet.CpuTemperature = new Iot.Device.CpuTemperature.CpuTemperature().Temperature.Fahrenheit;
            }
            catch
            {
                status |= ResultStatus.CpuTempReadfailure;
            }

            // Convert to integer for transmission
            results.ResultStatus = (int)status;

            return results;
        }
    }
}
