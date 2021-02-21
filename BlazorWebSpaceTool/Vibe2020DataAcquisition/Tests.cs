using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text;
using BrctcSpaceLibrary.Processes;

namespace Vibe2020Tests
{
    public class Tests
    {
        public void AccelerometerProcessorTest()
        {
            AccelerometerDataAnalysis processor = new AccelerometerDataAnalysis();
            for (int i = 0; i < 8000; i++)
            {
                processor.ProcessData(GenerateData());
            }

            processor.PerformFFTAnalysis();

            Console.WriteLine("X Values:");
            Console.WriteLine("\t" + processor.X_Magnitudes);

            Console.WriteLine("Y Values:");
            Console.WriteLine("\t" + processor.Y_Magnitudes);

            Console.WriteLine("Z Values:");
            Console.WriteLine("\t" + processor.Z_Magnitudes);
        }

        private Span<byte> GenerateData()
        {
            Random random = new Random();
            int[] values = new int[3];
            values[0] = random.Next(1900, 2400);
            values[1] = random.Next(1900, 2400);
            values[2] = random.Next(1900, 2400);

            return System.Runtime.InteropServices.MemoryMarshal.Cast<int, byte>(values);
        }
    }
}
