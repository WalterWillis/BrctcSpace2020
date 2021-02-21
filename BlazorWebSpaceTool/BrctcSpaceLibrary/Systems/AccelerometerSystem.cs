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
    public class AccelerometerSystem
    {
        private readonly int _secondaryDataTrigger; //subtract one from expected/wanted samples per second since counter starts at 0

        private IMcp3208 _accelerometerDevice;
        private IRTC _rtcDevice;
        private CpuTemperature _cpuDevice;

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

        private ConcurrentQueue<string> _fileQueue = new ConcurrentQueue<string>();

        public ConcurrentQueue<string> FileQueue { get => _fileQueue; }
        public AccelerometerSystem(IMcp3208 accel_Type, string fileName, 
            CpuTemperature cpuTemperature, IRTC rtc, int expectedSPS = 7999)
        {
            _accelerometerDevice = accel_Type;
            _accelFileName = fileName;
            _secondaryDataTrigger = expectedSPS;

            _cpuDevice = cpuTemperature;
            _rtcDevice = rtc;

            _accelSegmentLength = _accelBytes + _rtcBytes + _cpuBytes;
        }

        public  void RunAccelerometer(CancellationToken token)
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            Span<byte> data = new Span<byte>(new byte[_accelSegmentLength]);

            Span<byte> accelSegment = data.Slice(0, _accelBytes);
            Span<byte> rtcSegment = data.Slice(_accelBytes, _rtcBytes);
            Span<byte> cpuSegment = data.Slice(_accelBytes + _rtcBytes, _cpuBytes);

            
            int secondaryDataCounter = 0;

            int fileCounter = 0;

            //Initialize to approximately 256 KB (or as close it as possible given the segment size)
            int chunkSize = (4096 / _accelSegmentLength) * 256;

            int maxLines = chunkSize * 25; //arbitrary amount of iterations per buffer cycle

            int iterations = maxLines / chunkSize;

            while (!token.IsCancellationRequested)
            {
                string fileName = _accelFileName + fileCounter.ToString();

                using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        //initialize
                        GetRTCTime(rtcSegment);
                        GetCPUTemp(cpuSegment);

                        for (int iteration = 0; iteration < iterations; iteration++)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            for (int i = 0; i < chunkSize; i++)
                            {
                                _accelerometerDevice.Read(accelSegment);
                                if (secondaryDataCounter++ >= _secondaryDataTrigger)
                                {
                                    GetRTCTime(rtcSegment);
                                    GetCPUTemp(cpuSegment);
                                    secondaryDataCounter = 0;
                                }

                                stream.Write(data);

                                AccelDatasetCounter++;

                                accelSegment.Clear(); // only clear the accelerometer values as they must be ensured to be precise
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

        private void GetRTCTime(Span<byte> buffer)
        {
            Monitor.Enter(_rtcDevice);
            _rtcDevice.GetCurrentDate(buffer);
            Monitor.Exit(_rtcDevice);
        }

        private void GetCPUTemp(Span<byte> buffer)
        {
            Monitor.Enter(_cpuDevice);
            if (_cpuDevice.IsAvailable)
            {
                var temp = BitConverter.GetBytes(_cpuDevice.Temperature.DegreesFahrenheit);

                buffer[0] = temp[0];
                buffer[1] = temp[1];
                buffer[2] = temp[2];
                buffer[3] = temp[3];
                buffer[4] = temp[4];
                buffer[5] = temp[5];
                buffer[6] = temp[6];
                buffer[7] = temp[7];
            }
            Monitor.Exit(_cpuDevice);
        }

        public static IMcp3208 GetNormalMCPClass()
        {
            var settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

            return new Accelerometer(settings); ;
        }

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

        public static IMcp3208 GetMockMCPClass()
        {
            return new Device.Mocks.MockAccelerometer(); ;
        }

    }
}
