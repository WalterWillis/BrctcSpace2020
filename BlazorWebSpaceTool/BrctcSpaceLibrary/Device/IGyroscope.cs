using System;

namespace BrctcSpaceLibrary.Device
{
    public interface IGyroscope
    {
        /// <summary>
        /// Reads data from the gyroscope and stores it in the provided buffer.
        /// </summary>
        public void AcquireData(Span<byte> buffer);

        /// <summary>
        /// Reads burst data from the gyroscope and returns it as an array of integers.
        /// </summary>
        public Span<int> BurstRead();

        /// <summary>
        /// Reads burst data from the gyroscope and stores it in the provided buffer.
        /// </summary>
        public void BurstRead(Span<byte> buffer);

        /// <summary>
        /// Disposes of the resources used by the implementing class, such as the SpiDevice object.
        /// </summary>
        public void Dispose();

        /// <summary>
        /// Reads data from a specific register in a more efficient way and returns it as a span of bytes.
        /// </summary>
        public Span<byte> FastRegisterRead(Register regAddr);

        /// <summary>
        /// Reads data from a specific register in a more efficient way and stores it in the provided buffer.
        /// </summary>
        public void FastRegisterRead(Register regAddr, Span<byte> replyBuffer);

        /// <summary>
        /// Reads data from a specific register and returns it as an integer.
        /// </summary>
        public int RegisterRead(byte regAddr);

        /// <summary>
        /// Writes a short value to a specific register.
        /// </summary>
        public void RegisterWrite(byte regAddr, short value);
    }
}
