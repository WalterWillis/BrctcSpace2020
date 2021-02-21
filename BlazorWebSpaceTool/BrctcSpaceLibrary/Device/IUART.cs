using System;

namespace BrctcSpaceLibrary.Device
{
    public interface IUART
    {
        void Dispose();
        void SendBytes(Span<byte> buffer);
        string SerialRead();
        void SerialSend(string message);
    }
}