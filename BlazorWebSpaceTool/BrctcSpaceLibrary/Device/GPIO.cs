using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;

namespace BrctcSpaceLibrary.Device
{
    public class GPIO : IGPIO
    {
        private GpioController _gpio;
        private const int DR_PIN = 13; //physical pin scheme;
        private const int RST_PIN = 15;

        public GPIO()
        {
            _gpio = new GpioController(PinNumberingScheme.Board);
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
}
