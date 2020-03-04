using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Generic;
using System.Device.Spi;
using System.IO;
using System.Text;
using static BrctcSpaceLibrary.Device.Accelerometer;

namespace BrctcSpaceLibrary.WriteTests
{
    public class BinaryChunkWriterTest
    {
        private Mcp3208Custom _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private RTC _rtcDevice;
        private CpuTemperature _cpuDevice;
        private UART _uart;

        private FileStream fs;

        /// <summary>
        /// Keeps track of the amount of data sets for our test
        /// </summary>
        public long DataSetCounter { get; set; } = 0;

        public BinaryChunkWriterTest(string fileName)
        {
            using (SpiDevice spi = SpiDevice.Create(new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 2000000 }))
            {
                _accelerometerDevice = new Mcp3208Custom(spi, (int)Channel.X, (int)Channel.Y, (int)Channel.Z);
            }
           // _accelerometerDevice = new Accelerometer(new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 });
            _gyroscopeDevice = new Gyroscope(new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1000000 });
            _cpuDevice = new CpuTemperature();
            _rtcDevice = new RTC();
            _uart = new UART();

            fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        }

        public void Start(System.Threading.CancellationToken token)
        {
            const int segmentSize = 48;
            const int chunkSize = (4096 / segmentSize) * 1024;
            const int accelBytes = 12;
            const int gyroBytes = 20;
            const int rtcBytes = 8;
            const int cpuBytes = 8;

            Span<byte> accelSegment = new Span<byte>(new byte[accelBytes]);
            Span<byte> gyroSegment = new Span<byte>(new byte[gyroBytes]);
            Span<byte> rtcSegment = new Span<byte>(new byte[rtcBytes]);
            Span<byte> cpuSegment = new Span<byte>(new byte[cpuBytes]);

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
                            //clear before getting new data to ensure integrity
                            accelSegment.Clear();
                            _accelerometerDevice.Read(accelSegment);
                            stream.Write(accelSegment);

                            if (secondaryDataCounter++ >= secondaryDataTrigger)
                            {
                                gyroSegment.Clear();
                                _gyroscopeDevice.AcquireData(gyroSegment);

                                rtcSegment.Clear();
                                _rtcDevice.GetCurrentDate(rtcSegment);

                                cpuSegment.Clear();
                                GetCpuTemp(cpuSegment);

                                secondaryDataCounter = 0;
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
                        }
                        stream.WriteTo(fs);
                        fs.Flush();
                        stream.Position = 0;                      
                    }
                }
            }
            catch (OperationCanceledException)
            {
                fs.Close();
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

        ~BinaryChunkWriterTest()
        {
            _accelerometerDevice.Dispose();
            _gyroscopeDevice.Dispose();
            fs.Dispose();
        }

    }
}
