
using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Device.Spi;
using System.IO;
using System.Threading;
using static BrctcSpaceLibrary.Device.Accelerometer;

namespace BrctcSpaceLibrary.Systems
{
    /// <summary>
    /// The AccelerometerSystem class is responsible for handling accelerometer data collection, RTC, and CPU temperature.
    /// </summary>
    public class AccelerometerSystem
    {
        private readonly int _secondaryDataTrigger; // Subtract one from expected/wanted samples per second since counter starts at 0
        public int SPS { get => _secondaryDataTrigger; }

        private int _accelSegmentLength;

        private const int _accelBytes = 12;
        private const int _rtcBytes = 8;
        private const int _cpuBytes = 8;

        public int AccelBytes { get => _accelBytes; }
        public int Rtcbytes { get => _rtcBytes; }
        public int CpuBytes { get => _cpuBytes; }

        private string _accelFileName;
        public string FileName { get => _accelFileName; }
        public long AccelDatasetCounter { get; set; } = 0;

        /// <summary>
        /// Defines the maximum number of chunks per file, default is 25 for approximately 1,000,000 lines per file.
        /// </summary>
        public int MaxChunksPerFile { get; set; } = 25;

        private ConcurrentQueue<string> _fileQueue = new ConcurrentQueue<string>();

        public ConcurrentQueue<string> FileQueue { get => _fileQueue; }

        /// <summary>
        /// Initializes the AccelerometerSystem with a specified file name and expected samples per second (default: 7999).
        /// </summary>
        public AccelerometerSystem(string fileName, int expectedSPS = 7999)
        {
            _accelFileName = fileName;
            _secondaryDataTrigger = expectedSPS;
            _accelSegmentLength = _accelBytes + _rtcBytes + _cpuBytes;
        }

        /// <summary>
        /// Collects and stores accelerometer, RTC, and CPU temperature data until the cancellation token is triggered.
        /// </summary>
        public void RunAccelerometer(CancellationToken token)
        {
            Span<byte> data = new Span<byte>(new byte[_accelSegmentLength]);

            Span<byte> accelSegment = data.Slice(0, _accelBytes);
            Span<byte> rtcSegment = data.Slice(_accelBytes, _rtcBytes);
            Span<byte> cpuSegment = data.Slice(_accelBytes + _rtcBytes, _cpuBytes);

            int secondaryDataCounter = 1;

            int fileCounter = 0;

            // Initialize to approximately 256 KB (or as close it as possible given the segment size)
            int chunkSize = _secondaryDataTrigger * 8; //(4096 / _accelSegmentLength) * 256;

            int maxLines = chunkSize * MaxChunksPerFile; // Arbitrary amount of iterations per buffer cycle

            int iterations = maxLines / chunkSize;

            while (!token.IsCancellationRequested)
            {
                string fileName = _accelFileName + fileCounter.ToString();

                using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        // Initialize
                        GetRTCTime(rtcSegment);
                        GetCPUTemp(cpuSegment);

                        for (int iteration = 0; iteration < iterations; iteration++)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            for (int i = 0; i < chunkSize; i++)
                            {
                                Devices.Accelerometer.Read(accelSegment);
                                if (secondaryDataCounter++ >= _secondaryDataTrigger)
                                {
                                    GetRTCTime(rtcSegment);
                                    GetCPUTemp(cpuSegment);
                                    secondaryDataCounter = 1;
                                }
                                stream.Write(data);

                                AccelDatasetCounter++;

                                accelSegment.Clear(); // Only clear the accelerometer values as they must be ensured to be precise
                            }

                            stream.WriteTo(fs);
                            fs.Flush();
                            stream.Position = 0;
                        }
                    }
                }
                fileCounter++;
                _fileQueue.Enqueue(fileName);
            }
        }

        /// <summary>
        /// Retrieves the current RTC time and stores it in the provided buffer.
        /// </summary>
        private void GetRTCTime(Span<byte> buffer)
        {
            Monitor.Enter(Devices.RTC);
            Devices.RTC.GetCurrentDate(buffer);
            Monitor.Exit(Devices.RTC);
        }

        /// <summary>
        /// Retrieves the current CPU temperature and stores it in the provided buffer.
        /// </summary>
        private void GetCPUTemp(Span<byte> buffer)
        {
            Monitor.Enter(Devices.CPUTemp);
            if (Devices.CPUTemp.IsAvailable)
            {
                var temp = BitConverter.GetBytes(Devices.CPUTemp.Temperature.DegreesFahrenheit);

                buffer[0] = temp[0];
                buffer[1] = temp[1];
                buffer[2] = temp[2];
                buffer[3] = temp[3];
                buffer[4] = temp[4];
                buffer[5] = temp[5];
                buffer[6] = temp[6];
                buffer[7] = temp[7];
            }
            Monitor.Exit(Devices.CPUTemp);
        }

        /// <summary>
        /// Returns a normal MCP class for the accelerometer.
        /// </summary>
        public static IMcp3208 GetNormalMCPClass()
        {
            var settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

            return new Accelerometer(settings); ;
        }

        /// <summary>
        /// Returns a custom MCP class for the accelerometer.
        /// </summary>
        public static IMcp3208 GetCustomMCPClass()
        {
            var settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

            Mcp3208Custom mcp;

            using (SpiDevice spi = SpiDevice.Create(settings))
            {
                mcp = new Mcp3208Custom(spi, (int)Channel.X, (int)Channel.Y, (int)Channel.Z);
            }

            return mcp;
        }

        /// <summary>
        /// Returns a mock MCP class for the accelerometer.
        /// </summary>
        public static IMcp3208 GetMockMCPClass()
        {
            return new Device.Mocks.MockAccelerometer(); ;
        }
    }
}

/*
In this AccelerometerSystem class, the primary purpose is to handle the collection and storage of accelerometer data, Real-Time Clock (RTC) data, and CPU temperature data.

The class starts by defining necessary variables and properties, such as the number of bytes needed for accelerometer, RTC, and CPU data. It also initializes a ConcurrentQueue to store file names.

The RunAccelerometer method is responsible for continuously collecting and storing the data until the CancellationToken is triggered. 
Inside the method, it creates a file for each data set and writes the data to the file in chunks. It also updates the AccelDatasetCounter for each data point collected.

The GetRTCTime and GetCPUTemp methods are used to retrieve the RTC time and CPU temperature, respectively, and store them in the provided buffers. 
These methods are called within the `RunAccelerometer` method during data collection.

Additionally, the class provides three static methods to create different types of MCP (Microchip Analog-to-Digital Converter) classes for the accelerometer:

1. `GetNormalMCPClass` returns a normal MCP class for the accelerometer.
2. `GetCustomMCPClass` returns a custom MCP class for the accelerometer.
3. `GetMockMCPClass` returns a mock MCP class for the accelerometer, which can be useful for testing purposes.

Overall, the AccelerometerSystem class is responsible for managing the data collection and storage process for the accelerometer, RTC, and CPU temperature data. 
The class provides methods to initialize, run, and retrieve data from the system, making it a crucial component in the data acquisition process.
*/