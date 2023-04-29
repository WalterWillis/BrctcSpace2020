using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Device.Gpio;
using System.IO;
using System.Threading;

namespace BrctcSpaceLibrary.Systems
{
    /// <summary>
    /// GyroscopeSystem is responsible for handling the data acquisition process for a gyroscope device, storing the data related to gyroscope, RTC, and CPU temperature in a file.
    /// </summary>
    public class GyroscopeSystem
    {
        /// <summary>
        /// FileStream object to write gyroscope data to a file.
        /// </summary>
        public FileStream GyroStream { get; set; }

        // Other properties related to the segments lengths, file names, and dataset counters.
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

        /// <summary>
        /// Constructor initializes the class with a given file name and calculates the total segment length.
        /// </summary>
        /// <param name="fileName"></param>
        public GyroscopeSystem(string fileName)
        {
            _gyroFileName = fileName;
            _gyroSegmentLength = _gyroBytes + _rtcBytes + _cpuBytes;
        }

        /// <summary>
        /// Callback function raised when there's a change in the pin value of the gyroscope. Reads the data from gyroscope, RTC, and CPU temperature, and writes the data to the GyroStream.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="pinValueChangedEventArgs"></param>
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing gyroscope data!\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reads the current date and time from the RTC device and writes the result to the provided buffer, 
        /// ensuring thread safety using a monitor since this same object is used by the accelerometer system.
        /// </summary>
        /// <param name="buffer"></param>
        private void GetRTCTime(Span<byte> buffer)
        {
            Monitor.Enter(Devices.RTC);
            Devices.RTC.GetCurrentDate(buffer);
            Monitor.Exit(Devices.RTC);
        }

        /// <summary>
        /// Reads the current date and time from the RTC device and writes the result to the provided buffer, 
        /// ensuring thread safety using a monitor since this same object is used by the accelerometer system.
        /// </summary>
        /// <param name="buffer"></param>
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

/*
The `GyroscopeSystem` class handles the data acquisition process for a gyroscope device. 
This class stores data related to the gyroscope, Real-Time Clock (RTC), and CPU temperature in a file. 
The class has several properties, such as segment lengths, file names, and dataset counters.

1. The constructor `GyroscopeSystem(string fileName)` initializes the class with a given file name, 
and it also calculates the total segment length by adding the lengths of the gyroscope, RTC, and CPU temperature segments.

2. The `DataAquisitionCallback` method is called when there's a change in the pin value of the gyroscope. 
This method reads the data from the gyroscope, RTC, and CPU temperature, and then writes the data to the `GyroStream`. It also increments the `_gyroDatasetCounter`.

3. The `GetRTCTime(Span<byte> buffer)` method is a private helper method that reads the current date and time from the RTC device and writes the result to the provided buffer. 
This method uses a monitor to ensure thread safety while accessing the RTC device.

4. The `GetCPUTemp(Span<byte> buffer)` method is another private helper method that reads the current CPU temperature and writes the result to the provided buffer. 
This method also uses a monitor to ensure thread safety while accessing the CPU temperature device.

In summary, the `GyroscopeSystem` class manages the data collection and storage process for the gyroscope, RTC, and CPU temperature data. 
The class provides methods to handle the data acquisition callback and retrieve data from the system, making it an essential component in the data acquisition process.
*/ 