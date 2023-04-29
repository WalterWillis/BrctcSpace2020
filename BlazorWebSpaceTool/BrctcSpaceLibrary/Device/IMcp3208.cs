using System;
using System.Collections.Generic;
using System.Text;

namespace BrctcSpaceLibrary.Device
{
    /// <summary>
    /// Join the ADC/Accelerometer classes for generalizing purposes.
    /// </summary>
    public interface IMcp3208
    {
        /// <summary>
        /// Reads data from the X, Y, and Z axis of the accelerometers and stores the results directly in the provided memory buffer.
        /// </summary>
        /// <param name="buffer">A 12-byte buffer to store the results</param>
        public void Read(Span<byte> buffer);

        /// <summary>
        /// Disposes of the SPI device when the instance is no longer needed.
        /// </summary>
        public void Dispose();
    }
}
