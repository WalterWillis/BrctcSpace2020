using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Device.Spi;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static BrctcSpaceLibrary.Device.Accelerometer;

namespace BrctcSpaceLibrary.Vibe2020Programs
{
    public class FullSystemDeviceAbstraction
    {
        private IMcp3208 _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private RTC _rtcDevice;
        private CpuTemperature _cpuDevice;
        private const int accelBytes = 12;
        private const int gyroBytes = 20;
        private const int rtcBytes = 8;
        private const int cpuBytes = 8;

        private Memory<byte> AccelSharedMemory = new Memory<byte>(new byte[accelBytes]);
        private Memory<byte> GyroSharedMemory = new Memory<byte>(new byte[gyroBytes]);

        public int SegmentLength { get; private set; } = 48;

        //Only used by outside methods. Should always be the same as the internal filename
        public string FileName { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "FullSystemDeviceAbstraction.binary");

        // Keeps track of the amount of data sets for our test
        public long DatasetCounter { get; private set; } = 0;


        // Byte chunk size for the MemoryStream -- amount of bytes to hold in memory beforing writing to disk
        private long _chunkSize;

        public FullSystemDeviceAbstraction(bool useCustomAdcCode = true)
        {
            var settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

            if (useCustomAdcCode)
            {
                using (SpiDevice spi = SpiDevice.Create(settings))
                {
                    _accelerometerDevice = new Mcp3208Custom(spi, (int)Channel.X, (int)Channel.Y, (int)Channel.Z);
                }
            }
            else
            {
                _accelerometerDevice = new Accelerometer(settings);
            }

            _gyroscopeDevice = new Gyroscope(new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 900000 });

            //Initialize to approximately 1 MB (or as close it as possible given the segment size)
            _chunkSize = (4096 / SegmentLength) * 256;
        }

        public void Run(double timeLimit, System.Threading.CancellationToken token)
        {
            Span<byte> buffer = new Span<byte>(new byte[SegmentLength]);
            Span<byte> accelSegment = buffer.Slice(0, accelBytes);
            Span<byte> gyroSegment = buffer.Slice(accelBytes, gyroBytes);
            Span<byte> rtcSegment = buffer.Slice(accelBytes + gyroBytes, rtcBytes);
            Span<byte> cpuSegment = buffer.Slice(accelBytes + gyroBytes + rtcBytes, cpuBytes);

            Task accelTask;
            Task gyroTask;
            CancellationToken threadToken = new CancellationToken();

            Stopwatch stopwatch = new Stopwatch();

            using (FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    stopwatch.Start();

                    while (stopwatch.Elapsed.TotalMinutes < timeLimit && !token.IsCancellationRequested)
                    {

                        for (int i = 0; i < _chunkSize; i++)
                        {
                            GetAccelerometerSegment(accelSegment);
                            GetGyroscopeSegment(gyroSegment);

                            _accelerometerDevice.Read(accelSegment);
                            _gyroscopeDevice.AcquireData(gyroSegment);
                            _rtcDevice.GetCurrentDate(rtcSegment);
                            GetCpuTemp(cpuSegment);

                            DatasetCounter++;

                            stream.Write(buffer);
                            buffer.Clear();
                        }
                        stream.WriteTo(fs);
                        fs.Flush();
                        stream.Position = 0;
                    }

                    stopwatch.Stop();

                    Console.WriteLine($"AccelerometerOnly program ran for {stopwatch.Elapsed.TotalSeconds} seconds" +
                        $" creating {DatasetCounter} datasets at {DatasetCounter / stopwatch.Elapsed.TotalSeconds} datasets per second");
                }
            }
        }

        private Task AbstractAccelerometer(CancellationToken token)
        {
            const int unsyncedInt = accelBytes;
            return Task.Run(() =>
            {
                Span<byte> fillerSpan = new Span<byte>(new byte[unsyncedInt]);
                while (!token.IsCancellationRequested)
                {
                    _accelerometerDevice.Read(fillerSpan);

                    if (Monitor.TryEnter(AccelSharedMemory))
                    {                     
                        AccelSharedMemory.Span.Clear();
                        fillerSpan.CopyTo(AccelSharedMemory.Span);
                        Monitor.Exit(AccelSharedMemory);
                    }               
                }
            });
        }

        private Task AbstractGyroscope(CancellationToken token)
        {
            const int unsyncedInt = gyroBytes;
            return Task.Run(() =>
            {
                Span<byte> fillerSpan = new Span<byte>(new byte[unsyncedInt]);
                while (!token.IsCancellationRequested)
                {
                    _gyroscopeDevice.AcquireData(fillerSpan);

                    if (Monitor.TryEnter(GyroSharedMemory))
                    {
                        GyroSharedMemory.Span.Clear();
                       fillerSpan.CopyTo(GyroSharedMemory.Span);
                        Monitor.Exit(GyroSharedMemory);
                    }           
                }
            });
        }

        private void GetAccelerometerSegment(Span<byte> buffer)
        {
            Monitor.Enter(AccelSharedMemory);
            AccelSharedMemory.Span.CopyTo(buffer);
            Monitor.Exit(AccelSharedMemory);
        }

        private void GetGyroscopeSegment(Span<byte> buffer)
        {
            Monitor.Enter(GyroSharedMemory);
            GyroSharedMemory.Span.CopyTo(buffer);
            Monitor.Exit(GyroSharedMemory);
        }

        private void GetCpuTemp(Span<byte> buffer)
        {
            var temp = BitConverter.GetBytes(_cpuDevice.Temperature.Fahrenheit);

            buffer[0] = temp[0];
            buffer[1] = temp[1];
            buffer[2] = temp[2];
            buffer[3] = temp[3];
            buffer[4] = temp[4];
            buffer[5] = temp[5];
            buffer[6] = temp[6];
            buffer[7] = temp[7];
        }

        ~FullSystemDeviceAbstraction()
        {
            _accelerometerDevice.Dispose();
        }
    }
}
