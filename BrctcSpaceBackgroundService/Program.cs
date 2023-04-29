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
    /// <summary>
    /// The Program class is the entry point of the BrctcSpaceBackgroundService application.
    /// It initializes and configures the required devices, sets up the FullSystem, and manages
    /// the disposal of resources.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Check the current runtime architecture
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                // Use mock devices for testing on x64 architecture
                Devices.RTC = new MockRTC();
                Devices.Accelerometer = new MockAccelerometer();
                Devices.Gyroscope = new MockGyroscope();
                Devices.UART = new MockUart();
                Devices.GPIO = new MockGpio();
            }
            else
            {
                // Configure and initialize real devices for other architectures. This should be ARM64 on the Raspberry Pi 4.
                var settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

                using (SpiDevice spi = SpiDevice.Create(settings))
                {
                    Devices.Accelerometer = new Mcp3208Custom(spi, (int)Channel.X, (int)Channel.Y, (int)Channel.Z);
                }

                Devices.Gyroscope = new Gyroscope(new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 900000 });
                Devices.GPIO = new GPIO();
                Devices.RTC = new RTC();
                Devices.UART = new UART();
            }

            // Initialize the FullSystem and configure its properties
            FullSystem system = new FullSystem();
            FileInfo file = new FileInfo(system.AccelFileName);

            MockUart.FileName = Path.Combine(file.Directory.FullName, "Telemetry.csv");

            system.SetChunkAmount(1);

            // Set up the CancellationTokenSource for the system
            // CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // for quick tests
            CancellationTokenSource source = new CancellationTokenSource();
            system.Run(source.Token);

            // Dispose of device resources
            Devices.Accelerometer.Dispose();
            Devices.Gyroscope.Dispose();
            Devices.RTC.Dispose();
            Devices.UART.Dispose();
        }
    }
}

/*
This class serves as the entry point for the BrctcSpaceBackgroundService application, 
which is responsible for initializing, configuring, and managing various devices and systems, 
such as accelerometers, gyroscopes, RTC, UART, and GPIO. 

The class sets up the FullSystem to perform data acquisition and processing tasks using these devices. 
Based on the runtime architecture, the class either initializes mock devices (for testing purposes) or real devices for deployment. 

Additionally, the class takes care of disposing the resources used by these devices once the application is done running. 

This class ensures that the necessary hardware components are properly set up and managed for the proper functioning of the entire application.
*/