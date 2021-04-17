using BrctcSpaceLibrary.Device;
using BrctcSpaceLibrary.Systems;
using BrctcSpaceLibrary.Device.Mocks;
using System;
using System.Threading;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using BrctcSpaceLibrary;
using static BrctcSpaceLibrary.Device.Accelerometer;
using System.Device.Spi;
using System.Device.Gpio;
using System.Runtime.InteropServices;

namespace BrctcSpaceBackgroundService
{
    class Program
    {
        static void Main(string[] args)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                Devices.RTC = new MockRTC();
                Devices.Accelerometer = new MockAccelerometer();
                Devices.Gyroscope = new MockGyroscope();
                Devices.UART = new MockUart();
                Devices.GPIO = new MockGpio();
            }
            else
            {
                var settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

                using (SpiDevice spi = SpiDevice.Create(settings))
                {
                    Devices.Accelerometer = new Mcp3208Custom(spi, (int)Channel.X, (int)Channel.Y, (int)Channel.Z);
                }

                Devices.Gyroscope = new Gyroscope(new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 900000 });
                Devices.GPIO = new GPIO();
                Devices.RTC = new RTC();
                Devices.UART = new UART(); //mock for now
            }

            FullSystem system = new FullSystem();
            FileInfo file = new FileInfo(system.AccelFileName);

            MockUart.FileName = Path.Combine(file.Directory.FullName, "Telemetry.csv"); ;

            system.SetChunkAmount(1);

            //CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // for quick tests
            CancellationTokenSource source = new CancellationTokenSource();
            system.Run(source.Token);

            Devices.Accelerometer.Dispose();
            Devices.Gyroscope.Dispose();
            Devices.RTC.Dispose();
            Devices.UART.Dispose();
        }
    }
}
