using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BrctcSpaceLibrary.WriteTests
{
    public class PureMemoryTest
    {
        private Accelerometer _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private CpuTemperature _cpuDevice;

        public long DataSetCounter { get; set; } = 0;

        public PureMemoryTest()
        {
            _accelerometerDevice = new Accelerometer();
            _gyroscopeDevice = new Gyroscope();
            _cpuDevice = new CpuTemperature();
        }

        public void Start(System.Threading.CancellationToken token)
        {
            const int segmentSize = 48;
            const int chunkSize = (4096 / segmentSize);
            const int accelBytes = 12;
            const int gyroBytes = 20;
            const int rtcBytes = 8;
            const int cpuBytes = 8;

            Span<byte> data = new Span<byte>(new byte[segmentSize]);

            Span<byte> accelSegment = data.Slice(0, accelBytes);
            Span<byte> gyroSegment = data.Slice(accelBytes, gyroBytes);
            Span<byte> rtcSegment = data.Slice(accelBytes + gyroBytes, rtcBytes);
            Span<byte> cpuSegment = data.Slice(accelBytes + gyroBytes + rtcBytes, cpuBytes);

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
                            //_gyroscopeDevice.AquireData(gyroSegment);
                           // GetCurrentDate(rtcSegment);
                           // GetCpuTemp(cpuSegment);

                            stream.Write(data);
                            DataSetCounter++;
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
