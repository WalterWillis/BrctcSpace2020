using System;
using System.Device.I2c;
using System.Diagnostics;
using Iot.Device.Rtc;

namespace BrctcSpaceLibrary.Device
{
    public class RTC : IDisposable
    {
        private bool _isdisposing = false;
        private I2cConnectionSettings _settings;
        private Ds1307 _rtc;

        /// <summary>
        /// Ds3231 RTC
        /// </summary>
        public RTC()
        {
            _settings = new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress);

            using (I2cDevice device = I2cDevice.Create(_settings))
            {
                _rtc = new Ds1307(device);
            }
        }

        /// <summary>
        /// Ds3231 RTC
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
                _rtc = new Ds1307(device);
            }
        }

        public DateTime GetCurrentDate()
        {
            return _rtc.DateTime;
        }
        public void GetCurrentDate(Span<byte> buffer)
        {
            byte[] bytes = BitConverter.GetBytes(_rtc.DateTime.Ticks);

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

        public void SetDate(DateTime newDate)
        {
            _rtc.DateTime = newDate;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="disposing"></param>
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
