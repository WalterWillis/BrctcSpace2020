using System.IO.Ports;

namespace GrpcSpaceServer.Device
{
    public class UART
    {
        private SerialPort serialDevice;

        public UART()
        {
            serialDevice = new SerialPort("UART0", 57600, Parity.None, 8, StopBits.One);
            serialDevice.WriteTimeout = 1000;
            serialDevice.ReadTimeout = 1000;
        }
        public void SerialSend(string message)
        {
            serialDevice.Open();

            serialDevice.WriteLine(message);

            serialDevice.Close();
        }

        public void SendBytes(byte[] message)
        {
            serialDevice.Open();

            serialDevice.Write(message, 0, message.Length);

            serialDevice.Close();
        }
    }
}
