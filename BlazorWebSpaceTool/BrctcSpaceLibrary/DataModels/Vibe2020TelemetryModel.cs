using System;
using System.Collections.Generic;
using System.Text;

namespace BrctcSpaceLibrary.DataModels
{
    class Vibe2020TelemetryModel
    {
        /// <summary>
        /// Index of line in entire dataset
        /// </summary>
        public long Index { get; set; }

        /// <summary>
        /// The accelerometer values in the order of X, Y, Z with no scaling
        /// </summary>
        public int[] AccelData_Raw { get; set; }

        /// <summary>
        /// Timestamp of dataread by DateTime ticks
        /// </summary>
        public long TransactionTime { get; set; }

        /// <summary>
        /// Temperature of the RPI CPU
        /// </summary>
        public double CpuTemp { get; set; }

        /// <summary>
        /// Converts all values to a comma-delimited line
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Index},{string.Join(',', AccelData_Raw)},{TransactionTime},{CpuTemp}";
        }
    }
}
