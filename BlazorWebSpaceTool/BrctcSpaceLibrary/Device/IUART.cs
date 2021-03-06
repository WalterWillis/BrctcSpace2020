using System;
using System.Threading.Tasks;

namespace BrctcSpaceLibrary.Device
{
    public interface IUART
    {
        public void Dispose();
        public void SendBytes(Span<byte> buffer);
        public string SerialRead();
        public void SerialSend(string message);
        public Task SerialSendAsync(string message);
        public IUART GetUART();
    }
}