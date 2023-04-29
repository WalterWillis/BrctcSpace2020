﻿using System;
using System.Device.Spi;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace BrctcSpaceLibrary.Device
{
    public class Gyroscope : IDisposable, IGyroscope
    {
        private SpiConnectionSettings _settings;
        private bool _isdisposing = false;
        // Container used to quickly request data without further allocations being necessary
        // 0 and 1 are used for requests, 2 and 3 are used for replies
        private Memory<byte> FastBuffer = new Memory<byte>(new byte[4]);
        private ReadOnlyMemory<byte> BurstReadRegisterBuffer = new ReadOnlyMemory<byte>(
            new byte[22] { 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        private Memory<byte> BurstReadDataBuffer = new Memory<byte>(new byte[22]);
        SpiDevice _gyro;

        /// <summary>
        /// ADIS16460 Gyroscope
        /// </summary>
        public Gyroscope()
        {
            _settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 1000000 };
            _gyro = SpiDevice.Create(_settings);
        }

        /// <summary>
        /// ADIS16460 Gyroscope
        /// </summary>
        /// <param name="settings">Define customized settings or set null to allow default</param>
        public Gyroscope(SpiConnectionSettings settings)
        {
            if (settings == null)
            {
                settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 1000000 };
            }

            _settings = settings;

            _gyro = SpiDevice.Create(_settings);
        }

        /// <summary>
        /// Returns an array of burst data (Will be removed)
        /// </summary>
        /// <returns></returns>
        public Span<int> BurstRead()
        {
            Span<byte> Diag = FastRegisterRead(Register.DIAG_STAT);
            Span<byte> GyroX = FastRegisterRead(Register.X_GYRO_OUT);
            Span<byte> GyroY = FastRegisterRead(Register.Y_GYRO_OUT);
            Span<byte> GyroZ = FastRegisterRead(Register.Z_GYRO_OUT);
            Span<byte> AccelX = FastRegisterRead(Register.X_ACCL_OUT);
            Span<byte> AccelY = FastRegisterRead(Register.Y_ACCL_OUT);
            Span<byte> AccelZ = FastRegisterRead(Register.Z_ACCL_OUT);
            Span<byte> Temp = FastRegisterRead(Register.TEMP_OUT);
            Span<byte> Sample = FastRegisterRead(Register.SMPL_CNTR);
            Span<byte> Checksum = FastRegisterRead(Register.CAL_CRC);

            Span<byte> burstData = new byte[20]
            {
                Diag[0], Diag[1],
                GyroX[0], GyroX[1],
                GyroY[0], GyroY[1],
                GyroZ[0], GyroZ[1],
                AccelX[0], AccelX[1],
                AccelY[0], AccelY[1],
                AccelZ[0], AccelZ[1],
                Temp[0], Temp[1],
                Sample[0], Sample[1],
                Checksum[0], Checksum[1],
             };

            // Only for quick data retrieval
            // Convert the byte array to an int array -- Efficient, but will require using the exact opposite to retrieve correct values
            // Example: Convert result such that newResult = MemoryMarshal.Cast<int,byte>(result)
            // Note that the cast takes data type length into account. An int is 4 bytes.
            // Note: 20 bytes = 5 int array. 22 bytes also = 5 int array. 20 is perfect for multiplicity
            return MemoryMarshal.Cast<byte, int>(burstData); // Remove the leading empty bytes
        }

        // Reads burst data and stores it in the provided buffer
        public void BurstRead(Span<byte> buffer)
        {
            _gyro.TransferFullDuplex(BurstReadRegisterBuffer.Span, BurstReadDataBuffer.Span);

            var span = BurstReadDataBuffer.Span;
            // Endianness is inverted from the ADIS' expectation. This is taken care of via the GyroConversionHelper static class in the UI.
            buffer[0] = span[2];
            buffer[1] = span[3];

            buffer[2] = span[4];
            buffer[3] = span[5];

            buffer[4] = span[6];
            buffer[5] = span[7];

            buffer[6] = span[8];
            buffer[7] = span[9];

            buffer[8] = span[10];
            buffer[9] = span[11];

            buffer[10] = span[12];
            buffer[11] = span[13];

            buffer[12] = span[14];
            buffer[13] = span[15];

            buffer[14] = span[16];
            buffer[15] = span[17];

            buffer[16] = span[18];
            buffer[17] = span[19];

            buffer[18] = span[20];
            buffer[19] = span[21];

            span.Clear();
        }

        public void AcquireData(Span<byte> buffer)
        {
            Span<byte> deviceBuffer = FastBuffer.Span.Slice(2, 2);

            FastRegisterRead(Register.DIAG_STAT, deviceBuffer);
            buffer[0] = deviceBuffer[0];
            buffer[1] = deviceBuffer[1];

            FastRegisterRead(Register.X_GYRO_OUT, deviceBuffer);
            buffer[2] = deviceBuffer[0];
            buffer[3] = deviceBuffer[1];

            FastRegisterRead(Register.Y_GYRO_OUT, deviceBuffer);
            buffer[4] = deviceBuffer[0];
            buffer[5] = deviceBuffer[1];

            FastRegisterRead(Register.Z_GYRO_OUT, deviceBuffer);
            buffer[6] = deviceBuffer[0];
            buffer[7] = deviceBuffer[1];

            FastRegisterRead(Register.X_ACCL_OUT, deviceBuffer);
            buffer[8] = deviceBuffer[0];
            buffer[9] = deviceBuffer[1];

            FastRegisterRead(Register.Y_ACCL_OUT, deviceBuffer);
            buffer[10] = deviceBuffer[0];
            buffer[11] = deviceBuffer[1];

            FastRegisterRead(Register.Z_ACCL_OUT, deviceBuffer);
            buffer[12] = deviceBuffer[0];
            buffer[13] = deviceBuffer[1];

            FastRegisterRead(Register.TEMP_OUT, deviceBuffer);
            buffer[14] = deviceBuffer[0];
            buffer[15] = deviceBuffer[1];

            FastRegisterRead(Register.SMPL_CNTR, deviceBuffer);
            buffer[16] = deviceBuffer[0];
            buffer[17] = deviceBuffer[1];

            FastRegisterRead(Register.CAL_CRC, deviceBuffer);
            buffer[18] = deviceBuffer[0];
            buffer[19] = deviceBuffer[1];
        }

        public int RegisterRead(byte regAddr)
        {
            byte[] reply = new byte[2];
            // ADIS is a 16-bit device. Append a 0 byte to the address
            _gyro.Write(new byte[] { regAddr, 0x00 });
            Thread.SpinWait(40); // Delay approximately 40 microseconds
            _gyro.Read(reply);
            Thread.SpinWait(40); // Delay approximately 40 microseconds

            int result = (reply[0] << 8) | (reply[1] & 0xFF);

            Console.WriteLine($"Read Register {regAddr.ToString("x2")} with result {reply[0]} and {reply[1]} reversed and combined into {result}");
            return result;
        }

        public Span<byte> FastRegisterRead(Register regAddr)
        {
            Span<byte> reply = new byte[2];
            _gyro.TransferFullDuplex(new byte[] { (byte)regAddr, 0x00 }, reply);
            _gyro.TransferFullDuplex(new byte[] { 0x00, 0x00 }, reply);
            return reply;
        }

        public void FastRegisterRead(Register regAddr, Span<byte> replyBuffer)
        {
            var span = FastBuffer.Span.Slice(0, 2);
            span[0] = (byte)regAddr;
            _gyro.TransferFullDuplex(span, replyBuffer);
            span.Fill(0);
            _gyro.TransferFullDuplex(span, replyBuffer);
        }

        public void RegisterWrite(byte regAddr, short value)
        {
            UInt16 addr = (UInt16)(((regAddr & 0x7F) | 0x80) << 8); // Toggle sign bit, and check that the address is 8 bits
            UInt16 lowWord = (UInt16)(addr | (value & 0xFF)); // OR Register address (A) with data(D) (AADD)
            UInt16 highWord = (UInt16)((addr | 0x100) | ((value >> 8) & 0xFF)); // OR Register address with data and increment address

            // Split words into chars
            byte highBytehighWord = (byte)(highWord >> 8);
            byte lowBytehighWord = (byte)(highWord & 0xFF);
            byte highBytelowWord = (byte)(lowWord >> 8);
            byte lowBytelowWord = (byte)(lowWord & 0xFF);

            _gyro.Write(new byte[] { highBytelowWord, lowBytelowWord });
            Thread.SpinWait(40); // Delay approximately 40 microseconds
            _gyro.Write(new byte[] { highBytehighWord, lowBytehighWord });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isdisposing)
                return;

            if (disposing)
            {
                _gyro.Dispose();
            }

            _isdisposing = true;
        }
    }
}

/*
The Gyroscope class provides a high-level interface for interacting with the ADIS16460 gyroscope sensor.
This class allows you to create a gyroscope object, customize the connection settings, read
register values, and write data to registers. It also includes methods for acquiring burst data and disposing of the resources when they are no longer needed.

Here's a quick explanation of the class's main features
1. Constructors: The class provides two constructors, one with default connection settings and one that allows you to provide custom settings.
2. BurstRead: This method reads burst data from the gyroscope, which includes diagnostic information, gyro and accelerometer data, temperature, sample counter, and checksum. It returns an array of integers.
3. AcquireData: This method reads data from the gyroscope and stores it in the provided buffer. It retrieves the same data as BurstRead, but with a more efficient approach.
4. RegisterRead and FastRegisterRead: These methods allow you to read data from a specific register. FastRegisterRead provides a more efficient approach.
5. RegisterWrite: This method writes a short value to a specific register.
6. Dispose: This method disposes of the resources used by the class, such as the SpiDevice object.

*/