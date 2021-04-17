using BrctcSpaceLibrary.Helpers;
using System;
using System.Threading;

namespace BrctcSpaceLibrary.Device.Mocks
{
    public class MockAccelerometer : IMcp3208
    {
        private IntUnion _union = new IntUnion();
        public void Dispose()
        {
            //Do nothing
        }

        public void Read(Span<byte> buffer)
        {
            Random random = new Random();
            //the ADC range is 0 - 4095, but normal range might be between 1900-2400 when testing a small motor
            //but values should normally be within the mid range during normal use
            _union.integer = random.Next(1900, 2400);
            buffer[0] = _union.byte0;
            buffer[1] = _union.byte1;
            buffer[2] = _union.byte2;
            buffer[3] = _union.byte3;

            _union.integer = random.Next(1900, 2400);
            buffer[4] = _union.byte0;
            buffer[5] = _union.byte1;
            buffer[6] = _union.byte2;
            buffer[7] = _union.byte3;

            _union.integer = random.Next(1900, 2400);
            buffer[8] = _union.byte0;
            buffer[9] = _union.byte1;
            buffer[10] = _union.byte2;
            buffer[11] = _union.byte3;

            Thread.SpinWait(1000);
        }
    }
}
