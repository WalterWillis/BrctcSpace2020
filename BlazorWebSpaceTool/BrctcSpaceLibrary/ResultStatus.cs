using System;

namespace BrctcSpace
{
    [Flags]
    public enum ResultStatus
    {
        /// <summary>
        /// Also used as None
        /// </summary>
        Unknown = 0,
        AccelerometerFailure = 1,
        GyroscopeFailure = 2,
        RTCFailure = 4,
        CpuTempReadfailure = 8,
        Good = 16
    }
}
