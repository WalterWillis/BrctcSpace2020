using System;
using System.Device.I2c;
using System.Diagnostics;
using Iot.Device.Rtc;

namespace GrpcSpaceServer.Device
{
    public class RTC
    {
        private I2cConnectionSettings _settings;

        /// <summary>
        /// Ds3231 RTC
        /// </summary>
        public RTC()
        {
            _settings = new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress);
        }

        /// <summary>
        /// Ds3231 RTC
        /// </summary>
        /// <param name="settings">Define customized settings or set null to allow default</param>
        public RTC(I2cConnectionSettings settings)
        {
            if(settings == null)
            {
                settings = new I2cConnectionSettings(1, Ds3231.DefaultI2cAddress);
            }

            _settings = settings;
        }

        public DateTime GetCurrentDate()
        {
            using (I2cDevice device = I2cDevice.Create(_settings))
            {
                using (Ds1307 rtc = new Ds1307(device))
                {
                    return rtc.DateTime;
                }
            }
        }

        public void SetDate(DateTime newDate)
        {
            using (I2cDevice device = I2cDevice.Create(_settings))
            {
                using (Ds1307 rtc = new Ds1307(device))
                {
                    rtc.DateTime = newDate;
                }
            }
        }

        /// <summary>
        /// Gets the formatted timestamp for efficient replies, uses system time if there is an error
        /// </summary>
        /// <returns></returns>
        public Google.Protobuf.WellKnownTypes.Timestamp GetTimeStamp()
        {
            DateTime currentTime;

            try
            {
                currentTime = GetCurrentDate();
            }
            catch
            {
                Debug.WriteLine("RTC call failure");
                currentTime = DateTime.Now;
            }
            return Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(currentTime);
        }
    }
}
