using System;

namespace BrctcSpaceLibrary.Device
{
    public interface IGyroscope
    {
        public void AcquireData(Span<byte> buffer);
        public Span<int> BurstRead();
        public void BurstRead(Span<byte> buffer);
        public void Dispose();
        public Span<byte> FastRegisterRead(Register regAddr);
        public void FastRegisterRead(Register regAddr, Span<byte> replyBuffer);
        public int RegisterRead(byte regAddr);
        public void RegisterWrite(byte regAddr, short value);
    }
}