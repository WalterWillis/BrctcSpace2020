using System;
using System.Collections.Generic;
using System.Text;

namespace BrctcSpaceLibrary.DataModels
{
    public class TemperatureModel
    {
        private int N = 1; //Iteration number
        public double AverageCPUTemp { get; set; } = 1; //initialize at 1 to set value to self on first calculation
        public void GetNextAverage(double value)
        {
            double a = 1 / N++;
            double b = 1 - a;

            AverageCPUTemp = a * value + b * AverageCPUTemp;
        }

        internal void Reset()
        {
            N = 1;
            AverageCPUTemp = 1;
        }
    }
}
