using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrctcSpaceLibrary
{
    /// <summary>
    /// Helper class for gyroscope conversions
    /// </summary>
    /// <remarks>Thanks to juchong's library for his work on the ADIS16460 example code</remarks>
    /// <see cref="https://github.com/juchong/ADIS16460-Arduino-Teensy/blob/master/ADIS16460/ADIS16460.cpp"/>
    class GyroConversionHelper
    {
        /// <summary>
        /// Converts accelerometer data to acceleration in mg's
        /// </summary>
        /// <param name="sensorData">16 bit value from device</param>
        /// <returns></returns>
        public static double ScaleAccelData(short sensorData)
        {
            return sensorData * 0.00025; // Multiply by accel sensitivity (25 mg/LSB)
        }

        /// <summary>
        /// Converts gyro data to gyro rate in deg/sec
        /// </summary>
        /// <param name="sensorData">16 bit value from device</param>
        /// <returns></returns>
        public static double ScaleGyroData(short sensorData)
        {
            return sensorData * 0.005;
        }

        /// <summary>
        /// Converts temperature data to gyro rate in deg/sec
        /// </summary>
        /// <param name="sensorData">16 bit value from device</param>
        /// <param name="isFahrenheit">Conversion metric</param>
        /// <returns></returns>
        public static double ScaleTemperatureData(short sensorData, bool isFahrenheit = true)
        {
            int signedData = 0;
            int isNeg = sensorData & 0x8000;
            if (isNeg == 0x8000) // If the number is negative, scale and sign the output
                signedData = sensorData - 0xFFFF;
            else
                signedData = sensorData;
            double finalData = (signedData * 0.05) + 25; // Multiply by temperature scale and add 25 to equal 0x0000

            return isFahrenheit ? ((finalData * 9/5) + 32) : finalData;
        }

        /// <summary>
        /// Converts angle data to degrees
        /// </summary>
        /// <param name="sensorData">16 bit value from device</param>
        /// <returns></returns>
        public static double ScaleDeltaAngle(short sensorData)
        {
            return sensorData * 0.005; // Multiply by delta angle scale (0.005 degrees/LSB)
        }

        /// <summary>
        /// Converts scale data to velocity in mm/sec
        /// </summary>
        /// <param name="sensorData">16 bit value from device</param>
        /// <returns></returns>
        public static double ScaleDeltaVelocity(short sensorData)
        {
            return sensorData * 2.5; // Multiply by velocity scale (2.5 mm/sec/LSB)
        }

        /// <summary>
        /// Combine bytes into the 16bit values we expect
        /// </summary>
        /// <param name="burstdata"></param>
        /// <returns></returns>
        public static Span<short> CombineBytes(Span<byte> burstdata)
        {
            int counter = 0;
            Span<short> burstwords = new short[10];

            for (int i = 0; i < burstdata.Length; i += 2)
            {
                Span<byte> bytes = burstdata.Slice(i, 2);
                bytes.Reverse();
                burstwords[counter++] = BitConverter.ToInt16(bytes.ToArray(), 0);
            }
            #region Array Details
            /*
            burstwords[0]; //DIAG_STAT
            burstwords[1];//XGYRO
            burstwords[2]; //YGYRO
            burstwords[3]; //ZGYRO
            burstwords[4]; //XACCEL
            burstwords[5]; //YACCEL
            burstwords[6]; //ZACCEL
            burstwords[7]; //TEMP_OUT
            burstwords[8]; //SMPL_CNTR
            burstwords[9]; //CHECKSUM
            */
            #endregion

            return burstwords;
        }
    }
}
