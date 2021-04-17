using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrctcSpaceLibrary
{
    /// <summary>
    /// Static class that holds a single instance of each device so that all data is shared.
    /// </summary>
    public static class Devices
    {
        public static IMcp3208 Accelerometer { get; set; }
        public static IGyroscope Gyroscope { get; set; }
        public static IGPIO GPIO { get; set; }
        public static IRTC RTC { get; set; }
        public static IUART UART { get; set; }
        public static CpuTemperature CPUTemp { get; set; } = new CpuTemperature();
    }
}
