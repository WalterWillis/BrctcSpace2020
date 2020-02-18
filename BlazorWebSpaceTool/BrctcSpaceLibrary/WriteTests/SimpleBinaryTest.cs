using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace BrctcSpaceLibrary.WriteTests
{
    public class SimpleBinaryTest
    {
        private Accelerometer _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private CpuTemperature _cpuDevice;

        private FileStream stream;

        public long DataSetCounter { get; set; } = 0;

        public SimpleBinaryTest(string fileName)
        {
            _accelerometerDevice = new Accelerometer();
            _gyroscopeDevice = new Gyroscope();
            _cpuDevice = new CpuTemperature();

            stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        }

        public void Start(System.Threading.CancellationToken token)
        {
            const int segmentSize = 48;
            const int chunkSize = 4096 / segmentSize;

            try
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (token.IsCancellationRequested)
                            token.ThrowIfCancellationRequested();

                        for (int i = 0; i < chunkSize; i++)
                        {
                            var accl = _accelerometerDevice.GetRaws();
                            writer.Write(accl[0]);
                            writer.Write(accl[1]);
                            writer.Write(accl[2]);
                            var gyro = _gyroscopeDevice.BurstRead();
                            writer.Write(gyro[0]);
                            writer.Write(gyro[1]);
                            writer.Write(gyro[2]);
                            writer.Write(gyro[3]);

                            writer.Write(DateTime.Now.Ticks);

                            writer.Write(_cpuDevice.Temperature.Fahrenheit);

                            DataSetCounter++;
                        }
                        writer.Flush();
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
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

            ~SimpleBinaryTest()
        {
            _accelerometerDevice.Dispose();
            _gyroscopeDevice.Dispose();
            stream.Dispose();
        }

    }
}
