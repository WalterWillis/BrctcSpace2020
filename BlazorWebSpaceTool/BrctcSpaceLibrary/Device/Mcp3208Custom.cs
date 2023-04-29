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
    /// A more efficient implementation of Mcp3208 for high-speed data-driven tasks.
    /// Designed specifically for three ADXL-1002Z accelerometers configured in X, Y, and Z axes.
    /// </summary>
    public class Mcp3208Custom : IMcp3208, IDisposable
    {
        private SpiDevice _spiDevice;
        private const int bufferSize = 3;
        // Pre-allocate buffers to minimize memory allocations
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
        /// Constructor that initializes the SPI device and sets up the request buffers for each axis.
        /// </summary>
        /// <param name="spiDevice">The SPI device used for communication</param>
        /// <param name="channelX">The channel for the X axis of the accelerometer</param>
        /// <param name="channelY">The channel for the Y axis of the accelerometer</param>
        /// <param name="channelZ">The channel for the Z axis of the accelerometer</param>
        public Mcp3208Custom(SpiDevice spiDevice, int channelX, int channelY, int channelZ)
        {
            if (spiDevice == null)
            {
                throw new ArgumentNullException(nameof(spiDevice));
            }

            // Convert the requests to byte arrays
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
        /// Disposes of the SPI device when the instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            _spiDevice?.Dispose();
            _spiDevice = null;
        }

        /// <summary>
        /// Reads data from the X, Y, and Z axis of the accelerometers and stores the results directly in the provided memory buffer.
        /// </summary>
        /// <param name="buffer">A 12-byte buffer to store the results</param>
        public void Read(Span<byte> buffer)
        {
            Span<byte> x_responseBuffer = x_ResponseBuffer.Span;
            Span<byte> y_responseBuffer = y_ResponseBuffer.Span;
            Span<byte> z_responseBuffer = z_ResponseBuffer.Span;

            // Perform full-duplex SPI transfer for X axis data
            _spiDevice.TransferFullDuplex(x_RequestBuffer.Span, x_responseBuffer);
            // Format the X axis data and store it in the union
            x_union.integer = ((x_responseBuffer[1] & 15) << 8) + x_responseBuffer[2];

            // Perform full-duplex SPI transfer for Y axis data
            _spiDevice.TransferFullDuplex(y_RequestBuffer.Span, y_responseBuffer);
            // Format the Y axis data and store it in the union
            y_union.integer = ((y_responseBuffer[1] & 15) << 8) + y_responseBuffer[2];

            // Perform full-duplex SPI transfer for Z axis data
            _spiDevice.TransferFullDuplex(z_RequestBuffer.Span, z_responseBuffer);
            // Format the Z axis data and store it in the union
            z_union.integer = ((z_responseBuffer[1] & 15) << 8) + z_responseBuffer[2];

            // Copy the formatted data from the unions to the output buffer
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

            // Clear the response buffers to prepare for the next read operation
            x_responseBuffer.Clear();
            y_responseBuffer.Clear();
            z_responseBuffer.Clear();
        }
    }
}

/*
This code defines a class called `Mcp3208Custom` that provides a more efficient implementation of the MCP3208 analog-to-digital converter (ADC) for super fast data-driven tasks. It is specifically designed to work with three ADXL-1002Z accelerometers in an X, Y, and Z axis configuration.

The class contains a constructor that takes an `SpiDevice` object for SPI communication, and three integers representing the channels for X, Y, and Z axes of the accelerometer. It initializes the request buffers for each axis and converts them into byte arrays.

The class has a `Dispose` method that disposes the SPI device when the instance is no longer needed. The `Read` method reads data from the X, Y, and Z axes of the accelerometers and adds the data directly to the memory buffer passed as a parameter.

Here's a breakdown of the code:

1. The class inherits from the `IMcp3208` interface and implements the `IDisposable` interface for proper resource management.
2. Private fields for the SPI device and buffers are declared.
3. The constructor initializes the SPI device and sets up the request buffers for each axis.
4. The `Dispose` method disposes of the SPI device when it's no longer needed.
5. The `Read` method reads data from all three axes and adds the data to a passed-in memory buffer. It uses the `IntUnion` struct to convert the data into bytes with performance in mind.

As a student, you should understand that this class offers a more efficient way to work with the MCP3208 ADC for high-speed data processing tasks. The `Read` method is specifically optimized to read data from three accelerometers and store the data directly into a memory buffer.
*/