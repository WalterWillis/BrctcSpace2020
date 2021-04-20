using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;

namespace BrctcSpaceLibrary.Device
{
    public class GPIO : IGPIO
    {
        private GpioController _gpio;
        private const int DR_PIN = 27; //physical pin scheme;
        private const int RST_PIN = 22;

        public GPIO()
        {
            _gpio = new GpioController(PinNumberingScheme.Logical);
            _gpio.OpenPin(DR_PIN, PinMode.Input);
            _gpio.OpenPin(RST_PIN, PinMode.Output);
            _gpio.Write(RST_PIN, PinValue.High);  //RST pin should always be High unless we want to reset the gyro
        }

        public void RegisterCallbackForPinValueChangedEvent(PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            _gpio.RegisterCallbackForPinValueChangedEvent(DR_PIN, PinEventTypes.Rising, callback);
        }

        public void UnregisterCallbackForPinValueChangedEvent(PinChangeEventHandler callback)
        {
            _gpio.UnregisterCallbackForPinValueChangedEvent(DR_PIN, callback);
        }
    }

    /*
     If the error below is recieved, the issue is due to the compatibility of the PinNumberingScheme and the current revision of the pi.
    For example: PinNumberingScheme.Board works on Rev 1.1, but not Rev 1.2. We had to move to PinNumberingScheme.Logical on a Rev 1.2 Pi4
    
     Unhandled exception. System.PlatformNotSupportedException: This driver is generic so it can not perform conversions between pin numbering schemes.
    */
}
