using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Device.Gpio;
using System.IO;
using System.Threading;

namespace BrctcSpaceLibrary.Systems
{
    public class GyroscopeSystem
    {
        public FileStream GyroStream { get; set; }

        private int _gyroSegmentLength;

        private const int _gyroBytes = 20;
        private const int _rtcBytes = 8;
        private const int _cpuBytes = 8;

        public int GyroBytes { get => _gyroBytes; }
        public int Rtcbytes { get => _rtcBytes; }
        public int CpuBytes { get => _cpuBytes; }

        private string _gyroFileName;
        public string FileName { get => _gyroFileName; }

        private long _gyroDatasetCounter = 0;

        public int GyroSegmentLength { get => _gyroSegmentLength; }

        public long GyroDataSetCounter { get => _gyroDatasetCounter; }

        public GyroscopeSystem(string fileName)
        {
            _gyroFileName = fileName;
            _gyroSegmentLength = _gyroBytes + _rtcBytes + _cpuBytes;
        }

        public void DataAquisitionCallback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Span<byte> data = new Span<byte>(new byte[_gyroSegmentLength]);

            Span<byte> gyroSegment = data.Slice(0, _gyroBytes);
            Span<byte> rtcSegment = data.Slice(_gyroBytes, _rtcBytes);
            Span<byte> cpuSegment = data.Slice(_gyroBytes + _rtcBytes, _cpuBytes);

            Devices.Gyroscope.BurstRead(gyroSegment);
            GetRTCTime(rtcSegment);
            GetCPUTemp(cpuSegment);

            _gyroDatasetCounter++;

            try
            {
                GyroStream.Write(data);
                GyroStream.Flush();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error writing gyroscope data!\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        private void GetRTCTime(Span<byte> buffer)
        {
            Monitor.Enter(Devices.RTC);
            Devices.RTC.GetCurrentDate(buffer);
            Monitor.Exit(Devices.RTC);
        }

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
    }
}
