using System;

namespace BrctcSpaceLibrary
{
    [Flags]
    public enum ResultStatus
    {
        None = 1,
        AccelerometerFailure = 2,
        GyroscopeFailure = 4,
        RTCFailure = 8,
        CpuTempReadFailure = 16,
        AccelerometerSuccess = 32,
        GyroscopeSuccess = 64,
        RTCSuccess = 128,
        CpuTempReadSuccess = 256,
    }
}
