﻿using System;
using System.Device.Spi;
using System.Linq;
using System.Runtime.InteropServices;

namespace BrctcSpaceLibrary.Device
{
    public class Gyroscope : IDisposable
    {
        private SpiConnectionSettings _settings;
        private bool _isdisposing = false;

        //Container used to quickly request data without further allocations being necessary
        //0 and 1 are used for requests, 2 and 3 are used for replies
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
            _settings = new SpiConnectionSettings(0, 1) { Mode = SpiMode.Mode3, ClockFrequency = 1000000 };
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
                settings = new SpiConnectionSettings(0, 1) { Mode = SpiMode.Mode3, ClockFrequency = 1000000 };
            }

            _settings = settings;

            _gyro = SpiDevice.Create(_settings);
        }

        /// <summary>
        /// returns an array of burst data
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

            //Only for quick data retrieval
            //Convert the byte array to an int array -- Efficient, but will require using the exact opposite to retrieve correct values
            //Example: Convert result such that newResult = MemoryMarshal.Cast<int,byte>(result)
            //Note that the cast takes data type length into account. An int is 4 bytes.
            //Note: 20 bytes = 5 int array. 22 bytes also = 5 int array. 20 is perfect for multiplicity
            return MemoryMarshal.Cast<byte, int>(burstData); //remove the leading empty bytes
        }

        public void BurstRead(Span<byte> buffer)
        {
            _gyro.TransferFullDuplex(BurstReadRegisterBuffer.Span, BurstReadDataBuffer.Span);

            var span = BurstReadDataBuffer.Span;

            //reverse endianness for ADIS16460, start at second set as the first set is only a reply to the reg call
            buffer[1] = span[2];
            buffer[0] = span[3];

            buffer[3] = span[4];
            buffer[2] = span[5];

            buffer[5] = span[6];
            buffer[4] = span[7];

            buffer[7] = span[8];
            buffer[6] = span[9];

            buffer[9] = span[10];
            buffer[8] = span[11];

            buffer[11] = span[12];
            buffer[10] = span[13];

            buffer[13] = span[14];
            buffer[12] = span[15];

            buffer[15] = span[16];
            buffer[14] = span[17];

            buffer[17] = span[18];
            buffer[16] = span[19];

            buffer[19] = span[20];
            buffer[18] = span[21];

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

        public short RegisterRead(Register regAddr)
        {
            short result;
            byte[] reply = new byte[2];
            //ADIS is a 16 bit device. Append a 0 byte to the address
            _gyro.TransferFullDuplex(new byte[] { (byte)regAddr, 0x00 }, reply);

            _gyro.TransferFullDuplex(new byte[] { 0x00, 0x00 }, reply);

            reply = reply.Reverse().ToArray();
            result = BitConverter.ToInt16(reply);

            return result;
        }

        private Span<byte> FastRegisterRead(Register regAddr)
        {
            Span<byte> reply = new byte[2];
            _gyro.TransferFullDuplex(new byte[] { (byte)regAddr, 0x00 }, reply);
            _gyro.TransferFullDuplex(new byte[] { 0x00, 0x00 }, reply);
            return reply;
        }

        private void FastRegisterRead(Register regAddr, Span<byte> replyBuffer)
        {
            var span = FastBuffer.Span.Slice(0,2);
            span[0] = (byte)regAddr;
            _gyro.TransferFullDuplex(span, replyBuffer);
            span.Fill(0);
            _gyro.TransferFullDuplex(span, replyBuffer);
        }


        public enum Register : byte
        {
            FLASH_CNT = 0x00,  //Flash memory write count
            DIAG_STAT = 0x02,  //Diagnostic and operational status
            X_GYRO_LOW = 0x04,  //X-axis gyroscope output, lower word
            X_GYRO_OUT = 0x06,  //X-axis gyroscope output, upper word
            Y_GYRO_LOW = 0x08,  //Y-axis gyroscope output, lower word
            Y_GYRO_OUT = 0x0A,  //Y-axis gyroscope output, upper word
            Z_GYRO_LOW = 0x0C,  //Z-axis gyroscope output, lower word
            Z_GYRO_OUT = 0x0E,  //Z-axis gyroscope output, upper word
            X_ACCL_LOW = 0x10,  //X-axis accelerometer output, lower word
            X_ACCL_OUT = 0x12,  //X-axis accelerometer output, upper word
            Y_ACCL_LOW = 0x14,  //Y-axis accelerometer output, lower word
            Y_ACCL_OUT = 0x16,  //Y-axis accelerometer output, upper word
            Z_ACCL_LOW = 0x18,  //Z-axis accelerometer output, lower word
            Z_ACCL_OUT = 0x1A,  //Z-axis accelerometer output, upper word
            SMPL_CNTR = 0x1C,  //Sample Counter, MSC_CTRL[3:2]=11
            TEMP_OUT = 0x1E,  //Temperature output (internal, not calibrated)
            X_DELT_ANG = 0x24,  //X-axis delta angle output
            Y_DELT_ANG = 0x26,  //Y-axis delta angle output
            Z_DELT_ANG = 0x28,  //Z-axis delta angle output
            X_DELT_VEL = 0x2A,  //X-axis delta velocity output
            Y_DELT_VEL = 0x2C,  //Y-axis delta velocity output
            Z_DELT_VEL = 0x2E,  //Z-axis delta velocity output
            MSC_CTRL = 0x32,  //Miscellaneous control
            SYNC_SCAL = 0x34,  //Sync input scale control
            DEC_RATE = 0x36,  //Decimation rate control
            FLTR_CTRL = 0x38,  //Filter control, auto-null record time
            GLOB_CMD = 0x3E,  //Global commands
            XGYRO_OFF = 0x40,  //X-axis gyroscope bias offset error
            YGYRO_OFF = 0x42,  //Y-axis gyroscope bias offset error
            ZGYRO_OFF = 0x44,  //Z-axis gyroscope bias offset factor
            XACCL_OFF = 0x46,  //X-axis acceleration bias offset factor
            YACCL_OFF = 0x48,  //Y-axis acceleration bias offset factor
            ZACCL_OFF = 0x4A,  //Z-axis acceleration bias offset factor
            LOT_ID1 = 0x52,  //Lot identification number
            LOT_ID2 = 0x54,  //Lot identification number
            PROD_ID = 0x56,  //Product identifier
            SERIAL_NUM = 0x58,  //Lot-specific serial number
            CAL_SGNTR = 0x60,  //Calibration memory signature value
            CAL_CRC = 0x62,  //Calibration memory CRC values
            CODE_SGNTR = 0x64,  //Code memory signature value
            CODE_CRC = 0x66  //Code memory CRC values
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
