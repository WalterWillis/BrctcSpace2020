using System;
using System.Collections.Generic;
using System.Text;

namespace BrctcSpaceLibrary.Device.Mocks
{
    public class MockRTC : IRTC
    {
        public void Dispose()
        {
            //Do nothing
        }

        public DateTime GetCurrentDate()
        {
            return DateTime.Now;
        }

        public void GetCurrentDate(Span<byte> buffer)
        {
            byte[] bytes = BitConverter.GetBytes(DateTime.Now.Ticks);

            //assign the values, not the variable for reference assignment
            buffer[0] = bytes[0];
            buffer[1] = bytes[1];
            buffer[2] = bytes[2];
            buffer[3] = bytes[3];
            buffer[4] = bytes[4];
            buffer[5] = bytes[5];
            buffer[6] = bytes[6];
            buffer[7] = bytes[7];
        }

        public void SetDate(DateTime newDate)
        {
            //Do nothing
        }
    }
}
