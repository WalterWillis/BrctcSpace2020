using BrctcSpaceLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrctcSpaceLibrary.Device.Mocks
{
    public class MockGyroscope : IGyroscope
    {
        private IntUnion _union = new IntUnion();
        public void AcquireData(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public Span<int> BurstRead()
        {
            throw new NotImplementedException();
        }

        public void BurstRead(Span<byte> buffer)
        {
            Random random = new Random();
            _union.integer = 0;
            buffer[0] = _union.byte0;
            buffer[1] = _union.byte1;

            _union.integer = random.Next(-4096, 4096);
            buffer[2] = _union.byte0;
            buffer[3] = _union.byte1;

            _union.integer = random.Next(-4096, 4096);
            buffer[4] = _union.byte0;
            buffer[5] = _union.byte1;

            _union.integer = random.Next(-4096, 4096);
            buffer[6] = _union.byte0;
            buffer[7] = _union.byte1;

            _union.integer = random.Next(-4096, 4096);
            buffer[8] = _union.byte0;
            buffer[9] = _union.byte1;

            _union.integer = random.Next(-4096, 4096);
            buffer[10] = _union.byte0;
            buffer[11] = _union.byte1;

            _union.integer = random.Next(-4096, 4096);
            buffer[12] = _union.byte0;
            buffer[13] = _union.byte1;

            _union.integer = random.Next(70, 120);
            buffer[14] = _union.byte0;
            buffer[15] = _union.byte1;

            _union.integer = 0;
            buffer[16] = _union.byte0;
            buffer[17] = _union.byte1;

            _union.integer = 0;
            buffer[18] = _union.byte0;
            buffer[19] = _union.byte1;
        }

        public void Dispose()
        {
           //do nothing
        }

        public Span<byte> FastRegisterRead(Register regAddr)
        {
            throw new NotImplementedException();
        }

        public void FastRegisterRead(Register regAddr, Span<byte> replyBuffer)
        {
            throw new NotImplementedException();
        }

        public int RegisterRead(byte regAddr)
        {
            throw new NotImplementedException();
        }

        public void RegisterWrite(byte regAddr, short value)
        {
            throw new NotImplementedException();
        }
    }
}
