using System;
using System.Device.I2c;
using System.Diagnostics;
using Iot.Device.Rtc;

namespace BrctcSpaceLibrary.Device
{
    public class RTC : IDisposable, IRTC
    {
        private bool _isdisposing = false;
        private I2cConnectionSettings _settings;
        private Ds3231 _rtc;

        /// <summary>
        /// Initializes a new instance of the RTC class with the default I2C settings.
        /// </summary>
        public RTC()
        {
            _settings = new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress);

            using (I2cDevice device = I2cDevice.Create(_settings))
            {
                _rtc = new Ds3231(device);
            }
        }

        /// <summary>
        /// Initializes a new instance of the RTC class with the specified I2C settings.
        /// </summary>
        /// <param name="settings">Define customized settings or set null to allow default</param>
        public RTC(I2cConnectionSettings settings)
        {
            if (settings == null)
            {
                settings = new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress);
            }

            _settings = settings;

            using (I2cDevice device = I2cDevice.Create(_settings))
            {
                _rtc = new Ds3231(device);
            }
        }

        /// <summary>
        /// Gets the current date and time from the RTC device.
        /// </summary>
        /// <returns>The current date and time.</returns>
        public DateTime GetCurrentDate()
        {
            DateTime dateTime;
            try
            {
                dateTime = _rtc.DateTime;
            }
            catch (Exception ex)
            {
                dateTime = DateTime.Now;
            }

            return dateTime;
        }

        /// <summary>
        /// Gets the current date and time from the RTC device and stores it in the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the date and time.</param>
        public void GetCurrentDate(Span<byte> buffer)
        {
            byte[] bytes;
            try
            {
                bytes = BitConverter.GetBytes(_rtc.DateTime.Ticks);
            }
            catch
            {
                bytes = BitConverter.GetBytes(DateTime.Now.Ticks);
            }

            //assign the values, not the variable for reference assignment
            buffer[0] = bytes[0];
            buffer[1] = bytes[1];
            buffer[2] = bytes[2];
            buffer[3] = bytes[3];
            buffer[4] = bytes[4];
            buffer[5] = bytes[5];
            buffer[6] = bytes[6];
            buffer[7] = bytes[7];
        }

        /// <summary>
        /// Sets the date and time of the RTC device.
        /// </summary>
        /// <param name="newDate">The new date and time to set.</param>
        public void SetDate(DateTime newDate)
        {
            _rtc.DateTime = newDate;
        }

        /// <summary>
        /// Disposes of the resources used by the RTC class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources used by the RTC class.
        /// </summary>
        /// <param name="disposing">Indicates whether to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isdisposing)
                return;

            if (disposing)
            {
                _rtc.Dispose();
            }

            _isdisposing = true;
        }
    }
}

/*
This code defines a class called `RTC` that represents a Real-Time Clock (RTC) device. 
An RTC is a hardware device that keeps track of the current date and time, even when the main system is powered off. 
In this case, the specific RTC model being used is the DS3231. 
The class implements the `IRTC` interface and the `IDisposable` interface to ensure proper resource cleanup.

Here's a brief explanation of the methods and properties in the `RTC` class:

1. Constructor `RTC()`: This constructor initializes the `RTC` object with default I2cConnectionSettings. 
It creates an I2cDevice instance and then creates a Ds3231 instance using the I2cDevice.

2. Constructor `RTC(I2cConnectionSettings settings)`: This constructor initializes the `RTC` object with custom I2cConnectionSettings. 
If the settings parameter is null, it uses the default settings. Similar to the default constructor, 
it creates an I2cDevice instance and then creates a Ds3231 instance using the I2cDevice.

3. `GetCurrentDate()`: This method returns the current date and time stored in the RTC as a DateTime object. 
If there's an exception while reading the RTC, it returns the system's current DateTime instead.

4. `GetCurrentDate(Span<byte> buffer)`: This method writes the current date and time stored in the RTC as a byte array to the provided buffer. 
If there's an exception while reading the RTC, it writes the system's current DateTime as bytes to the buffer.

5. `SetDate(DateTime newDate)`: This method sets the RTC to the specified DateTime value.

6. `Dispose()`: This method disposes of the resources used by the RTC class, such as the Ds3231 object, and suppresses finalization.

7. `Dispose(bool disposing)`: This protected method is called by the `Dispose()` method to perform the actual disposing of resources. 
It checks whether the object is already disposing and, if not, disposes of the Ds3231 object if the `disposing` parameter is true.

In summary, the `RTC` class allows you to interact with a DS3231 Real-Time Clock device using I2C communication. 
It provides methods for getting and setting the current date and time and properly manages the resources used by the class.
*/