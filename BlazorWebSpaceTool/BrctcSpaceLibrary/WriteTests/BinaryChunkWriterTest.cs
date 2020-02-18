using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BrctcSpaceLibrary.WriteTests
{
    public class BinaryChunkWriterTest
    {
        private Accelerometer _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private CpuTemperature _cpuDevice;

        private FileStream fs;

        /// <summary>
        /// Keeps track of the amount of data sets for our test
        /// </summary>
        public long DataSetCounter { get; set; } = 0;

        public BinaryChunkWriterTest(string fileName)
        {
            _accelerometerDevice = new Accelerometer();
            _gyroscopeDevice = new Gyroscope();
            _cpuDevice = new CpuTemperature();

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
                            _gyroscopeDevice.AquireData(gyroSegment);
                            stream.Write(gyroSegment);
                            GetCurrentDate(rtcSegment);
                            stream.Write(rtcSegment);
                            GetCpuTemp(cpuSegment);
                            stream.Write(cpuSegment);

                            DataSetCounter++; ;
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

        ~BinaryChunkWriterTest()
        {
            _accelerometerDevice.Dispose();
            _gyroscopeDevice.Dispose();
            fs.Dispose();
        }

    }
}
