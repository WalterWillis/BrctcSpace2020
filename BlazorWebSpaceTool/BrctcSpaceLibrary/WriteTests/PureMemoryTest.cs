using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Generic;
using System.Device.Spi;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static BrctcSpaceLibrary.Device.Accelerometer;

namespace BrctcSpaceLibrary.WriteTests
{
    public class PureMemoryTest
    {
        private Accelerometer _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private RTC _rtcDevice;
        private CpuTemperature _cpuDevice;

        public long DataSetCounter { get; set; } = 0;

        public PureMemoryTest()
        {
            //using (SpiDevice spi = SpiDevice.Create(new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode0, ClockFrequency = 2000000 }))
            //{
            //    _accelerometerDevice = new Mcp3208Custom(spi, (int)Channel.X, (int)Channel.Y, (int)Channel.Z);
            //}
            _accelerometerDevice = new Accelerometer(new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode0, ClockFrequency = 2000000 });
            _gyroscopeDevice = new Gyroscope(new SpiConnectionSettings(0, 1) { Mode = SpiMode.Mode3, ClockFrequency = 2000000 });
            _rtcDevice = new RTC();
            _cpuDevice = new CpuTemperature();
        }

        public void Start(System.Threading.CancellationToken token)
        {
            const int segmentSize = 48;
            const int chunkSize = (4096 / segmentSize) * 1024;
            const int accelBytes = 12;
            const int gyroBytes = 20;
            const int rtcBytes = 8;
            const int cpuBytes = 8;

            Span<byte> data = new Span<byte>(new byte[segmentSize]);

            Span<byte> accelSegment = data.Slice(0, accelBytes);
            Span<byte> gyroSegment = data.Slice(accelBytes, gyroBytes);
            Span<byte> rtcSegment = data.Slice(accelBytes + gyroBytes, rtcBytes);
            Span<byte> cpuSegment = data.Slice(accelBytes + gyroBytes + rtcBytes, cpuBytes);

            bool shown = false;
            int secondaryDataCounter = 0;

            const int secondaryDataTrigger = 200;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (token.IsCancellationRequested)
                            token.ThrowIfCancellationRequested();

                        for (int i = 0; i < chunkSize; i++)
                        {
                            _accelerometerDevice.GetRaws(accelSegment);
                            stream.Write(accelSegment);

                            if (secondaryDataCounter++ >= secondaryDataTrigger)
                            {
                                _gyroscopeDevice.AquireData(gyroSegment);
                                _rtcDevice.GetCurrentDate(rtcSegment);
                                GetCpuTemp(cpuSegment);
                                secondaryDataCounter = 0;
                            }
                            else
                            {
                                gyroSegment.Fill(0);
                                rtcSegment.Fill(0);
                                cpuSegment.Fill(0);
                            }

                            stream.Write(gyroSegment);

                            stream.Write(rtcSegment);

                            stream.Write(cpuSegment);

                            DataSetCounter++;

                            if (!shown)
                            {
                                Console.WriteLine($"{string.Join(',', accelSegment.ToArray())}\t{BitConverter.ToInt32(accelSegment.ToArray(), 0)}\t{BitConverter.ToInt32(accelSegment.ToArray(), 4)}\t{BitConverter.ToInt32(accelSegment.ToArray(), 8)}");
                                Console.WriteLine(string.Join(',', gyroSegment.ToArray()));
                                Console.WriteLine(string.Join(',', rtcSegment.ToArray()));
                                Console.WriteLine(string.Join(',', cpuSegment.ToArray()));
                                shown = true;
                            }

                            accelSegment.Clear();
                            gyroSegment.Clear();
                            rtcSegment.Clear();
                            cpuSegment.Clear();
                        }

                        stream.Position = 0;
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }

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

        public void GetCurrentDate(Span<byte> buffer)
        {
            byte[] bytes = BitConverter.GetBytes(DateTime.Now.Ticks);

            //assign the values, not the variable for reference assignment
            buffer[0] = bytes[0];
            buffer[1] = bytes[1];
            buffer[2] = bytes[2];
            buffer[3] = bytes[3];
            buffer[4] = bytes[4];
            buffer[5] = bytes[5];
            buffer[6] = bytes[6];
        }

        ~PureMemoryTest()
        {
            _accelerometerDevice.Dispose();
            _gyroscopeDevice.Dispose();
        }
    }
}
