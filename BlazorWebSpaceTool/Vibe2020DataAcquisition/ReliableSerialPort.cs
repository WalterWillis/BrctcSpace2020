using System;
using System.IO.Ports;
using System.Text;

namespace Vibe2020DataAcquisition
{
    /// <summary>
    /// In case the SerialPort class breaks.
    /// </summary>
    /// <see cref="https://www.vgies.com/a-reliable-serial-port-in-c/"/>
    public class ReliableSerialPort : SerialPort
    {
        public ReliableSerialPort(string portName, int baudRate)
        {
            PortName = portName;
            BaudRate = baudRate;
            DtrEnable = false;
            Handshake = Handshake.None;
            NewLine = Environment.NewLine;
            ReceivedBytesThreshold = 1024;
            DiscardNull = true;
        }
        public ReliableSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            PortName = portName;
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
            Handshake = Handshake.None;
            DtrEnable = true;
            NewLine = Environment.NewLine;
            ReceivedBytesThreshold = 1024;
            DiscardNull = true;
        }

        new public void Open()
        {
            base.Open();
            ContinuousRead();
        }

        private void ContinuousRead()
        {
            byte[] buffer = new byte[4096];
            Action kickoffRead = null;

            kickoffRead = async () =>
            {
                try
                {
                    int count = await BaseStream.ReadAsync(buffer, 0, buffer.Length);

                    byte[] dst = new byte[count];

                    Buffer.BlockCopy(buffer, 0, dst, 0, count);
                    OnDataReceived(dst);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                    Console.WriteLine(exception.StackTrace);                   
                }
                kickoffRead();
            }; 
            
            kickoffRead();
        }

        public delegate void DataReceivedEventHandler(object sender, DataReceivedArgs e);

        public virtual void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(this, new DataReceivedArgs { Data = data });
        }


#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public event DataReceivedEventHandler DataReceived;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

    }

    public class DataReceivedArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public string Line { get => Encoding.UTF8.GetString(Data); }
    }
}