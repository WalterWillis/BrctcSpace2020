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
        public int[] AccelData { get; set; }

        public short[] GyroData { get; set; }

        public DateTime TransactionTime { get; set; }

        public double CpuTemp { get; set; }

    }
}
