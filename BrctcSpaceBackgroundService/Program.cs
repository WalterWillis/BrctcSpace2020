using BrctcSpaceLibrary.Device;
using BrctcSpaceLibrary.Systems;
using BrctcSpaceLibrary.Device.Mocks;
using System;
using System.Threading;
using System.IO;

namespace BrctcSpaceBackgroundService
{
    class Program
    {
        static void Main(string[] args)
        {
            IRTC rtc = new MockRTC();
            IMcp3208 mcp = new MockAccelerometer();
            IGyroscope gyro = new MockGyroscope();
            IUART uart = new MockUart();
            IGPIO gpio = new MockGpio();

           

            FullSystem system = new FullSystem(mcp, gyro, rtc, uart, gpio);
            FileInfo file = new FileInfo(system.AccelFileName);
            MockUart.FileName = Path.Combine(file.Directory.FullName, "Telemetry.csv");
            system.SetChunkAmount(1);

            CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            system.Run(source.Token);
        }
    }
}
