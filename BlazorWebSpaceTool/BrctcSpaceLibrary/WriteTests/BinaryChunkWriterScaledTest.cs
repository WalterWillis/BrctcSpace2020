using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BrctcSpaceLibrary.WriteTests
{
    public class BinaryChunkWriterScaledTest
    {
        private Accelerometer _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private RTC _rtcDevice;
        private CpuTemperature _cpuDevice;

        private FileStream fs;

        /// <summary>
        /// Keeps track of the amount of data sets for our test
        /// </summary>
        public long DataSetCounter { get; set; } = 0;

        public BinaryChunkWriterScaledTest(string fileName)
        {
            _accelerometerDevice = new Accelerometer();
            _gyroscopeDevice = new Gyroscope();
            _cpuDevice = new CpuTemperature();
            _rtcDevice = new RTC();

            fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        }

        public void Start(System.Threading.CancellationToken token)
        {
            const int segmentSize = 60;
            const int chunkSize = (4096 / segmentSize) * 1024;
            const int accelBytes = 24;
            const int gyroBytes = 20;
            const int rtcBytes = 8;
            const int cpuBytes = 8;

            Span<byte> accelSegment = new Span<byte>(new byte[accelBytes]);
            Span<byte> gyroSegment = new Span<byte>(new byte[gyroBytes]);
            Span<byte> rtcSegment = new Span<byte>(new byte[rtcBytes]);
            Span<byte> cpuSegment = new Span<byte>(new byte[cpuBytes]);

            bool shown = false;
            int gyroCounter = 0;

            const int gyroTrigger = 200;


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
                            _accelerometerDevice.GetScaledValues(accelSegment);
                            stream.Write(accelSegment);

                            if (gyroCounter++ >= gyroTrigger)
                            {
                                _gyroscopeDevice.AquireData(gyroSegment);
                                gyroCounter = 0;
                            }
                            else
                                gyroSegment.Fill(0);
                            stream.Write(gyroSegment);

                            GetCurrentDate(rtcSegment);
                            stream.Write(rtcSegment);

                            GetCpuTemp(cpuSegment);
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
                        stream.WriteTo(fs);
                        fs.Flush();

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

        ~BinaryChunkWriterScaledTest()
        {
            _accelerometerDevice.Dispose();
            _gyroscopeDevice.Dispose();
            fs.Dispose();
        }
    }
}
