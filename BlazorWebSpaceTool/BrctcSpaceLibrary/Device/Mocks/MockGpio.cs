using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrctcSpaceLibrary.Device.Mocks
{
    public class MockGpio : IGPIO
    {
        private event PinChangeEventHandler GpioTriggerEvent;
        private bool isRegistered = false;

        private Task task;

        private void RunMockLoop()
        {
            while (isRegistered)
            {
                GpioTriggerEvent?.Invoke(this, new PinValueChangedEventArgs(PinEventTypes.None, 0));
                Thread.Sleep(10);
            }
        }
        public void RegisterCallbackForPinValueChangedEvent(PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            GpioTriggerEvent += callback;
            isRegistered = true;
            task = Task.Run(RunMockLoop);
        }

        public void UnregisterCallbackForPinValueChangedEvent(PinChangeEventHandler callback)
        {
            isRegistered = false;
            task.Wait();
            GpioTriggerEvent -= callback;
        }
    }
}
