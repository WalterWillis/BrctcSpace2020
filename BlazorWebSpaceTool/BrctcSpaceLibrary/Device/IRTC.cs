using System;

namespace BrctcSpaceLibrary.Device
{
    public interface IRTC
    {
        public void Dispose();
        public DateTime GetCurrentDate();
        public void GetCurrentDate(Span<byte> buffer);
        public void SetDate(DateTime newDate);
    }
}