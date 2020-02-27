using BrctcSpaceLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.Device.Spi;
using System.Text;

namespace BrctcSpaceLibrary.Device
{
    // MCP3208
    // Byte        2        1        0
    // ==== ======== ======== ========
    // Req  00000SMC CCxxxxxx xxxxxxxx
    // Resp xxxxxxxx xxDNRRRR RRRRRRRR

    /// <summary>
    /// More efficient version of Mcp3208 for super fast data driven tasks
    /// </summary>
    /// <seealso cref="https://github.com/dotnet/iot/blob/master/src/devices/Mcp3xxx/Mcp3Base.cs"/>
    class Mcp3208Custom : IDisposable
    {
        private SpiDevice _spiDevice;
        private const byte singleEndedByte = 0b1_1000;
        private const int adcResolutionBits = 12;
        private const int bufferSize = 3;
        //Only allocate arrays once
        private Memory<byte> x_RequestBuffer = new byte[bufferSize];
        private Memory<byte> y_RequestBuffer = new byte[bufferSize];
        private Memory<byte> z_RequestBuffer = new byte[bufferSize];

        private Memory<byte> x_ResponseBuffer = new byte[bufferSize];
        private Memory<byte> y_ResponseBuffer = new byte[bufferSize];
        private Memory<byte> z_ResponseBuffer = new byte[bufferSize];

        private Memory<int> workingValues = new int[bufferSize];

        private IntUnion union = new IntUnion();

        private const long checkInt = 1L;

        /// <summary>
        /// Constructs Mcp3Base instance
        /// </summary>
        /// <param name="spiDevice">Device used for SPI communication</param>
        public Mcp3208Custom(SpiDevice spiDevice, int channelX, int channelY, int channelZ)
        {
            if (spiDevice == null)
            {
                throw new ArgumentNullException(nameof(spiDevice));
            }

            // shift the requests left to make space in the response for the number of bits in the
            // response plus the conversion delay and plus 1 for a null bit.

            int x_Request = (singleEndedByte | channelX) << 14;
            int y_Request = (singleEndedByte | channelY) << 14;
            int z_Request = (singleEndedByte | channelZ) << 14;


            //Convert the requests to byte arrays
            for (int i = 0; i < bufferSize; i++)
            {
                x_RequestBuffer.Span[i] = (byte)(x_Request >> (bufferSize - i - 1) * 8);
            }

            for (int i = 0; i < bufferSize; i++)
            {
                y_RequestBuffer.Span[i] = (byte)(y_Request >> (bufferSize - i - 1) * 8);
            }

            for (int i = 0; i < bufferSize; i++)
            {
                z_RequestBuffer.Span[i] = (byte)(z_Request >> (bufferSize - i - 1) * 8);
            }

            _spiDevice = spiDevice;
        }

        /// <summary>
        /// Disposes Mcp3Base instances
        /// </summary>
        public void Dispose()
        {
            _spiDevice?.Dispose();
            _spiDevice = null;
        }

        /// <summary>
        /// Reads a value from the device
        /// </summary>
        /// <param name="adcRequest">A bit pattern to be sent to the ADC.</param>
        /// <param name="adcResolutionBits">The number of bits in the returned value</param>
        /// <param name="delayBits">The number of bits to be delayed between the request and the response being read.</param>
        /// <returns>A value corresponding to a voltage level on the input pin described by the request.</returns>
        public void Read(Span<byte> buffer)
        {
            Span<int> retval = workingValues.Span;
            Span<byte> x_responseBuffer = x_ResponseBuffer.Span;
            Span<byte> y_responseBuffer = y_ResponseBuffer.Span;
            Span<byte> z_responseBuffer = z_ResponseBuffer.Span;

            _spiDevice.TransferFullDuplex(x_RequestBuffer.Span, x_responseBuffer);

            //format the current value
            retval[0] <<= 8;
            retval[0] += x_responseBuffer[0];
            retval[0] <<= 8;
            retval[0] += x_responseBuffer[1];
            retval[0] <<= 8;
            retval[0] += x_responseBuffer[2];
       
            _spiDevice.TransferFullDuplex(y_RequestBuffer.Span, y_responseBuffer);
            retval[1] <<= 8;
            retval[1] += y_responseBuffer[0];
            retval[1] <<= 8;
            retval[1] += y_responseBuffer[1];
            retval[1] <<= 8;
            retval[1] += y_responseBuffer[2];
           
            _spiDevice.TransferFullDuplex(z_RequestBuffer.Span, z_responseBuffer);
            retval[2] <<= 8;
            retval[2] += z_responseBuffer[0];
            retval[2] <<= 8;
            retval[2] += z_responseBuffer[1];
            retval[2] <<= 8;
            retval[2] += z_responseBuffer[2];         

            // test the response from the ADC to check that the null bit is actually 0
            if (((retval[0] & (1 << adcResolutionBits)) != 0) || ((retval[1] & (1 << adcResolutionBits)) != 0) || ((retval[2] & (1 << adcResolutionBits)) != 0))
            {
                throw new InvalidOperationException("Invalid data was read from the sensor");
            }

            retval[0] &= (int)((checkInt << adcResolutionBits) - 1);
            retval[1] &= (int)((checkInt << adcResolutionBits) - 1);
            retval[2] &= (int)((checkInt << adcResolutionBits) - 1);

            union.integer = retval[0];
            buffer[0] = union.byte0;
            buffer[1] = union.byte1;
            buffer[2] = union.byte2;
            buffer[3] = union.byte3;

            union.integer = retval[1];
            buffer[4] = union.byte0;
            buffer[5] = union.byte1;
            buffer[6] = union.byte2;
            buffer[7] = union.byte3;

            union.integer = retval[2];
            buffer[8] = union.byte0;
            buffer[9] = union.byte1;
            buffer[10] = union.byte2;
            buffer[11] = union.byte3;

            //ensure integrity of next value
            x_responseBuffer.Clear();
            y_responseBuffer.Clear();
            z_responseBuffer.Clear();
            retval.Clear();

        }
    }
}

