using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using BrctcSpace;

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
            Device.Accelerometer accelerometerDevice = new Device.Accelerometer();
            Device.Gyroscope gyroscopeDevice = new Device.Gyroscope();
            Device.RTC rtcDevice = new Device.RTC();

            return Task.FromResult(new ResultReply
            {
                ResultStatus = (int)ResultStatus.Good,
                ResultSet = new ResultSet
                {
                    AccelerometerResults =  accelerometerDevice.GetAccelerometerResults(request.ScaleAccelerometer),
                    GyroscopeResults = new GyroscopeResults { BurstResults = gyroscopeDevice.GetBurstResults() },
                    CpuTemperature = new Iot.Device.CpuTemperature.CpuTemperature().Temperature.Fahrenheit,
                    CurrentTime = rtcDevice.GetTimeStamp()                   
                }
            });
        }
    }
}
