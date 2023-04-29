using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;

namespace BrctcSpaceLibrary.Device
{
    /// <summary>
    /// GPIO class for managing general-purpose input/output (GPIO) pins.
    /// </summary>
    public class GPIO : IGPIO
    {
        private GpioController _gpio;
        private const int DR_PIN = 27; // Data ready pin, using the physical pin numbering scheme
        private const int RST_PIN = 22; // Reset pin

        /// <summary>
        /// Initializes a new instance of the GPIO class, opens the DR and RST pins, and sets the RST pin to High.
        /// </summary>
        public GPIO()
        {
            // Initialize the GPIO controller using the logical pin numbering scheme
            _gpio = new GpioController(PinNumberingScheme.Logical);
            // Open the data ready pin as input
            _gpio.OpenPin(DR_PIN, PinMode.Input);
            // Open the reset pin as output
            _gpio.OpenPin(RST_PIN, PinMode.Output);
            // Set the reset pin to High (the gyro will not reset unless the pin is set to Low)
            _gpio.Write(RST_PIN, PinValue.High);
        }

        /// <summary>
        /// Registers a callback method to be invoked when a pin value change event occurs.
        /// </summary>
        /// <param name="eventTypes">The types of events to listen for (e.g., Rising or Falling edges)</param>
        /// <param name="callback">The callback method to be executed when the event occurs</param>
        public void RegisterCallbackForPinValueChangedEvent(PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            // Register the callback for pin value change events on the data ready pin
            _gpio.RegisterCallbackForPinValueChangedEvent(DR_PIN, PinEventTypes.Rising, callback);
        }

        /// <summary>
        /// Unregisters a previously registered callback method for pin value change events.
        /// </summary>
        /// <param name="callback">The callback method to be unregistered</param>
        public void UnregisterCallbackForPinValueChangedEvent(PinChangeEventHandler callback)
        {
            // Unregister the callback for pin value change events on the data ready pin
            _gpio.UnregisterCallbackForPinValueChangedEvent(DR_PIN, callback);
        }
    }

    /*
     If the error below is received, the issue is due to the compatibility of the PinNumberingScheme and the current revision of the Raspberry Pi.
     For example: PinNumberingScheme.Board works on Rev 1.1, but not Rev 1.2. We had to switch to PinNumberingScheme.Logical on a Rev 1.2 Raspberry Pi 4.

     Unhandled exception. System.PlatformNotSupportedException: This driver is generic so it cannot perform conversions between pin numbering schemes.
    */

}

/*

This code defines a `GPIO` class for managing general-purpose input/output (GPIO) pins on a Raspberry Pi. 
The class implements the `IGPIO` interface. It initializes a new instance of the `GpioController` class, 
opens the data ready (DR) and reset (RST) pins, and sets the RST pin to High.

The `RegisterCallbackForPinValueChangedEvent` method is used to register a callback method that will be invoked when a pin value change event occurs. 
It listens for rising edges on the data ready pin. 
The `UnregisterCallbackForPinValueChangedEvent` method is used to unregister a previously registered callback method for pin value change events on the data ready pin.
*/
