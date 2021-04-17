using System.Device.Gpio;

namespace BrctcSpaceLibrary.Device
{
    public interface IGPIO
    {
        public void RegisterCallbackForPinValueChangedEvent(PinEventTypes eventTypes, PinChangeEventHandler callback);
        public void UnregisterCallbackForPinValueChangedEvent(PinChangeEventHandler callback);
    }
}