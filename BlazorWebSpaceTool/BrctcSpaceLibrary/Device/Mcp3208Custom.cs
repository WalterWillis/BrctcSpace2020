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
   public class Mcp3208Custom : IMcp3208, IDisposable
    {
        private SpiDevice _spiDevice;
        private const int bufferSize = 3;
        //Only allocate arrays once
        private Memory<byte> x_RequestBuffer = new byte[bufferSize];
        private Memory<byte> y_RequestBuffer = new byte[bufferSize];
        private Memory<byte> z_RequestBuffer = new byte[bufferSize];

        private Memory<byte> x_ResponseBuffer = new byte[bufferSize];
        private Memory<byte> y_ResponseBuffer = new byte[bufferSize];
        private Memory<byte> z_ResponseBuffer = new byte[bufferSize];

        private IntUnion x_union = new IntUnion();
        private IntUnion y_union = new IntUnion();
        private IntUnion z_union = new IntUnion();

        /// <summary>
        /// Creates an instance of the ADC and sets the requests to be sent based on the Accelerometer channel setup
        /// </summary>
        /// <param name="spiDevice">Device used for SPI communication</param>
        /// <param name="channelX">The X axis of the Accelerometer</param>
        /// <param name="channelY">The Y axis of the Accelerometer</param>
        /// <param name="channelZ">The Z axis of the Accelerometer</param>
        public Mcp3208Custom(SpiDevice spiDevice, int channelX, int channelY, int channelZ)
        {
            if (spiDevice == null)
            {
                throw new ArgumentNullException(nameof(spiDevice));
            }

            //Convert the requests to byte arrays
            var spanX = x_RequestBuffer.Span;
            spanX[0] = (byte)(6 + ((channelX & 4) >> 2));
            spanX[1] = (byte)((channelX & 3) << 6);
            spanX[2] = 0;

            var spanY = y_RequestBuffer.Span;
            spanY[0] = (byte)(6 + ((channelY & 4) >> 2));
            spanY[1] = (byte)((channelY & 3) << 6);
            spanY[2] = 0;

            var spanZ = z_RequestBuffer.Span;
            spanZ[0] = (byte)(6 + ((channelZ & 4) >> 2));
            spanZ[1] = (byte)((channelZ & 3) << 6);
            spanZ[2] = 0;

            _spiDevice = spiDevice;
        }

        /// <summary>
        /// Disposes instance
        /// </summary>
        public void Dispose()
        {
            _spiDevice?.Dispose();
            _spiDevice = null;
        }

        /// <summary>
        /// Reads the X, Y, and Z axis of our accelerometers and adds them directly to the memory buffer passed
        /// </summary>
        /// <param name="buffer">12 byte buffer</param>
        public void Read(Span<byte> buffer)
        {
            Span<byte> x_responseBuffer = x_ResponseBuffer.Span;
            Span<byte> y_responseBuffer = y_ResponseBuffer.Span;
            Span<byte> z_responseBuffer = z_ResponseBuffer.Span;

            _spiDevice.TransferFullDuplex(x_RequestBuffer.Span, x_responseBuffer);

            //format the current value
            x_union.integer = ((x_responseBuffer[1] & 15) << 8) + x_responseBuffer[2];
       
            _spiDevice.TransferFullDuplex(y_RequestBuffer.Span, y_responseBuffer);
            y_union.integer = ((y_responseBuffer[1] & 15) << 8) + y_responseBuffer[2];

            _spiDevice.TransferFullDuplex(z_RequestBuffer.Span, z_responseBuffer);
            z_union.integer = ((z_responseBuffer[1] & 15) << 8) + z_responseBuffer[2];

            buffer[0] = x_union.byte0;
            buffer[1] = x_union.byte1;
            buffer[2] = x_union.byte2;
            buffer[3] = x_union.byte3;

            buffer[4] = y_union.byte0;
            buffer[5] = y_union.byte1;
            buffer[6] = y_union.byte2;
            buffer[7] = y_union.byte3;

            buffer[8] = z_union.byte0;
            buffer[9] = z_union.byte1;
            buffer[10] = z_union.byte2;
            buffer[11] = z_union.byte3;

            //ensure integrity of next value
            x_responseBuffer.Clear();
            y_responseBuffer.Clear();
            z_responseBuffer.Clear();

        }
    }
}

