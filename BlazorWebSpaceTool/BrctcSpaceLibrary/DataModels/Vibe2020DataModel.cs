using System;
using System.Collections.Generic;
using System.Text;

namespace BrctcSpaceLibrary.DataModels
{
    /// <summary>
    /// Unified class for Vibe2020 data
    /// </summary>
    public class Vibe2020DataModel
    {
        public int[] AccelData_Raw { get; set; }

        public double[] AccelData { get; set; }

        public short[] GyroData_Raw { get; set; }

        public double[] GyroData { get; set; }

        public DateTime TransactionTime { get; set; }

        public double CpuTemp { get; set; }

        public ResultStatus ResultStatus { get; set; }

        /// <summary>
        /// Generates a line of text from the properties above
        /// </summary>
        /// <returns></returns>
        public string ToCsvLine()
        {
            string line = "";

            const string accelEmpty = "NA,NA,NA";
            const string gyroEmpty = "NA,NA,NA,NA,NA,NA,NA,NA,NA,NA";
            const char comma = ',';

            if (AccelData_Raw != null && AccelData_Raw.Length > 0)
            {
                line += string.Join(comma, AccelData_Raw);
            }
            else
            {
                line += accelEmpty;
            }

            line += comma;

            if (AccelData != null && AccelData.Length > 0)
            {
                line += string.Join(comma, AccelData);
            }
            else
            {
                line += accelEmpty;
            }

            line += comma;

            if (GyroData_Raw != null && GyroData_Raw.Length > 0)
            {
                line += string.Join(comma, GyroData_Raw);
            }
            else
            {
                line += gyroEmpty;
            }

            line += comma;

            if (GyroData != null && GyroData.Length > 0)
            {
                line += string.Join(comma, GyroData);
            }
            else
            {
                line += gyroEmpty;
            }

            line += comma;

            line += TransactionTime.Ticks;
            line += comma;
            line += CpuTemp;
            line += comma;
            line += (int)ResultStatus;

            return line;
        }

        /// <summary>
        /// Gets a CSV-style header
        /// </summary>
        /// <returns></returns>
        public static string GetHeader()
        {
            const string header = "ACCEL_X_RAW,ACCEL_Y_RAW,ACCEL_Z_RAW,ACCEL_X,ACCEL_Y,ACCEL_Z," +
                 "DIAG_STAT_RAW,GYRO_X_Raw,GYRO_Y_RAW,GYRO_Z_RAW,ACCEL_X_RAW,ACCEL_Y_RAW,ACCEL_Z_RAW,TEMP_RAW,SPS_RAW,CHECKSUM_RAW," +
                 "DIAG_STAT,GYRO_X,GYRO_Y,GYRO_Z,ACCEL_X,ACCEL_Y,ACCEL_Z,TEMP,SPS,CHECKSUM," +
                 "TRANSACTION_TIME_TICKS,CPU_TEMP,RESULT_STATUS";

            return header;
        }

    }
}
