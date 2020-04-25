using System;
using System.IO.Ports;

namespace BrctcSpaceLibrary.Device
{
    public class UART: IDisposable
    {
        private bool _isdisposing = false;
        private SerialPort _serialDevice;

        private bool testCanSend = true;
        private SerialDataReceivedEventHandler _toUnsubscribe; //used to automatically unsubscribe 

        public UART()
        {
            _serialDevice = new SerialPort("/dev/ttyAMA0", 57600);
            _serialDevice.WriteTimeout = 1000;
            _serialDevice.ReadTimeout = 1000;
            _serialDevice.Open();
        }

        public UART(string port, int writeTimeout = 1000, int readTimeout = 1000)
        {
            _serialDevice = new SerialPort(port, 57600);
            _serialDevice.WriteTimeout = writeTimeout;
            _serialDevice.ReadTimeout = readTimeout;
            _serialDevice.Open();
        }

        public void SerialSend(string message)
        {
            if(!_serialDevice.IsOpen)
                _serialDevice.Open();

            _serialDevice.WriteLine(message);
        }

        public string SerialRead()
        {
            if (!_serialDevice.IsOpen)
                _serialDevice.Open();

            string message = _serialDevice.ReadLine();

            return message;
        }

        public void SendBytes(Span<byte> buffer)
        {
            if (!_serialDevice.IsOpen)
                _serialDevice.Open();

            byte[] message = buffer.ToArray();

            _serialDevice.Write(message, 0, message.Length);
        }

        /// <summary>
        /// Subscribe to the DataRecieved event of the current UART connection
        /// </summary>
        /// <param name="interruptToAttach"></param>
        public void Subscribe(SerialDataReceivedEventHandler interruptToAttach)
        {
            _serialDevice.DataReceived += interruptToAttach;
            _toUnsubscribe = interruptToAttach;
        }

        /// <summary>
        /// Unsubscribe to the DataRecieved event of the current UART connection
        /// </summary>
        public void Unsubscribe()
        {
            _serialDevice.DataReceived -= _toUnsubscribe;
        }


        /// <summary>
        /// This function will test the rx/tx pins of the current device if the rx and tx are shorted to each other.
        /// </summary>
        public void SelfTest(int iterations)
        {
            if (iterations < 1)
                throw new ArgumentOutOfRangeException("SelfTest iterations must be a positive nonzero number");

            _serialDevice.DataReceived += Received;

            for (int i = 0; i < iterations; i++)
            {
                //wait until data is recieved
                while(!testCanSend) { }

                if (testCanSend)
                {
                    testCanSend = false;
                    _serialDevice.WriteLine($"Test{i}!");
                }                
            }

            _serialDevice.DataReceived -= Received;
        }

        private void Received(object sender, SerialDataReceivedEventArgs e)
        {
            Console.WriteLine($"Event Type: {e.EventType}");
            Console.WriteLine($"Data: {_serialDevice.ReadLine()}");
            testCanSend = true;
        }

        public static string[] GetPorts()
        {
            return SerialPort.GetPortNames();
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
                if (_serialDevice.IsOpen)
                    _serialDevice.Close();
                _serialDevice.Dispose();
            }

            _isdisposing = true;
        }
    }
}
