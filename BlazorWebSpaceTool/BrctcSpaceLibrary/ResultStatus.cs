using System;

namespace BrctcSpace
{
    [Flags]
    public enum ResultStatus
    {
        Unknown = 0,
        AccelerometerFailure = 1,
        GyroscopeFailure = 2,
        RTCFailure = 4,
        Good = 8
    }
}
