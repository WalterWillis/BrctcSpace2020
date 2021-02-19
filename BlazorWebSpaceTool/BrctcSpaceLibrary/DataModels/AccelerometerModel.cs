using System;
using System.Collections.Generic;
using System.Text;

namespace BrctcSpaceLibrary.DataModels
{
    /// <summary>
    /// Model to hold all three axis of Accelerometer Data
    /// </summary>
    public class AccelerometerModel
    {
        public const double resRatio = 5D / 4095;

        public AccelerometerModel(int[] rawData)
        {
            X_Raw = rawData[0];
            Y_Raw = rawData[1];
            Z_Raw = rawData[2];
        }

        public int X_Raw { get; set; }
        public int Y_Raw { get; set; }
        public int Z_Raw { get; set; }

        public double X { get => ScaleAccelerometer(X_Raw); }
        public double Y { get => ScaleAccelerometer(Y_Raw); }
        public double Z { get => ScaleAccelerometer(Z_Raw);  }

        private static double ScaleAccelerometer(int value)
        {
            return value * resRatio;
        }
    }
}
