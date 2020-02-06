using System;
using System.IO.Ports;

namespace GrpcSpaceServer.Device
{
    public class UART: IDisposable
    {
        private bool _isdisposing = false;
        private SerialPort _serialDevice;

        public UART()
        {
            _serialDevice = new SerialPort("UART0", 57600, Parity.None, 8, StopBits.One);
            _serialDevice.WriteTimeout = 1000;
            _serialDevice.ReadTimeout = 1000;
        }
        public void SerialSend(string message)
        {
            _serialDevice.Open();

            _serialDevice.WriteLine(message);

            _serialDevice.Close();
        }

        public void SendBytes(byte[] message)
        {
            _serialDevice.Open();

            _serialDevice.Write(message, 0, message.Length);

            _serialDevice.Close();
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
                _serialDevice.Dispose();
            }

            _isdisposing = true;
        }
    }
}
