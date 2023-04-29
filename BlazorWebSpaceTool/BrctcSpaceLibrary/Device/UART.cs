﻿using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace BrctcSpaceLibrary.Device
{
    /// <summary>
    /// The UART class is used for communication between the host device and other devices through a serial interface.
    /// </summary>
    public class UART : IDisposable, IUART
    {
        private bool _isdisposing = false;
        private SerialPort _serialDevice;
        private bool testCanSend = true;
        private SerialDataReceivedEventHandler _toUnsubscribe; //used to automatically unsubscribe 

        /// <summary>
        /// Initializes a new UART object with default settings.
        /// </summary>
        public UART()
        {
            _serialDevice = new SerialPort("/dev/ttyAMA0", 57600);
            //_serialDevice.WriteTimeout = 1000;
            //_serialDevice.ReadTimeout = 1000;
            _serialDevice.Handshake = Handshake.None;
            _serialDevice.Open();
        }

        /// <summary>
        /// Initializes a new UART object with custom settings.
        /// </summary>
        public UART(string port, int writeTimeout = 1000, int readTimeout = 1000)
        {
            _serialDevice = new SerialPort(port, 57600);
            //_serialDevice.WriteTimeout = writeTimeout;
            //_serialDevice.ReadTimeout = readTimeout;
            _serialDevice.Handshake = Handshake.None;
            _serialDevice.Open();
        }

        /// <summary>
        /// Sends a string message through the UART connection.
        /// </summary>
        public void SerialSend(string message)
        {
            if (!_serialDevice.IsOpen)
                _serialDevice.Open();

            _serialDevice.WriteLine(message);
        }

        /// <summary>
        /// Reads a string message from the UART connection.
        /// </summary>
        public string SerialRead()
        {
            if (!_serialDevice.IsOpen)
                _serialDevice.Open();

            string message = _serialDevice.ReadLine();

            return message;
        }

        /// <summary>
        /// Sends an array of bytes through the UART connection.
        /// </summary>
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
            if (_toUnsubscribe == null) //if we already subscribed the event, ignore
            {
                _serialDevice.DataReceived += interruptToAttach;
                _toUnsubscribe = interruptToAttach;
            }
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
                while (!testCanSend) { }
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
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to Dispose UART!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
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

        /// <summary>
        /// Used to recreate a UART device if an exception occurs during program run
        /// </summary>
        /// <returns></returns>
        public IUART GetUART()
        {
            return new UART();
        }

        /// <summary>
        /// Sends a string message through the UART connection asynchronously.
        /// </summary>
        public Task SerialSendAsync(string message)
        {
            return Task.Run(() => { SerialSend(message); });
        }
    }
}

/*
 * This UART class provides an interface for communicating with other devices through a serial connection. 
 * It supports sending and receiving both string messages and byte arrays, 
 * subscribing and unsubscribing to data received events, and disposing resources when no longer needed. 
 * It also includes methods for self-testing and recreating a UART object if needed.
*/ 