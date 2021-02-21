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
        public void Read(Span<byte> buffer);
        public void Dispose();
    }
}
