using FFTW.NET;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// Test to verify that we can change thread priority from within a task when the task is running
        /// </summary>
        [TestMethod]
        public void RunTasks()
        {
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 16; i++)
            {
                tasks.Add(Task.Run(MyTask));
            }

            Task.WaitAll(tasks.ToArray());

            Assert.IsTrue(true);
        }

        private void MyTask()
        {
            var thread = Thread.CurrentThread;
            Console.WriteLine($"Thread ID: {thread.ManagedThreadId}");
            Console.WriteLine($"Thread Priority: {thread.Priority}");
            Console.WriteLine($"Increasing Thread priority!");

            thread.Priority = ThreadPriority.Highest;
            Thread.SpinWait(5000);
            Console.WriteLine($"New Thread Priority: {thread.Priority}");
        }

        /// <summary>
        /// Modified version of the 1D example from the FFTW.NET Wrapper's example code
        /// </summary>
        [TestMethod]
        public void FFTWTest()
        {          
            Complex[] input = new Complex[8192];
            Complex[] output = new Complex[input.Length];

            GetData(input, input.Length);
            //for (int i = 0; i < input.Length; i++)
            //    input[i] = Math.Sin(i * 2 * Math.PI * 128 / input.Length);

            using (var pinIn = new PinnedArray<Complex>(input))
            using (var pinOut = new PinnedArray<Complex>(output))
            {
                DFT.FFT(pinIn, pinOut, nThreads: 12);
                DFT.IFFT(pinOut, pinOut, nThreads: 12);
            }

            Console.WriteLine("Real, Imaginary, Magnitude, Phase, ToString");
            for (int i = 0; i < input.Length; i++)
            {
                Complex result = output[i] / input[i];
                Console.WriteLine($"{result.Real},{result.Imaginary}, {result.Magnitude},{result.Phase},{result.ToString()}");
            }
        }

        /// <summary>
        /// Gets a set of data for FFT
        /// </summary>
        /// <param name="data"></param>
        /// <param name="arrayLength"></param>
        /// <param name="startingIndex"></param>
        /// <returns>The index the data stopped at</returns>
        private int GetData(Complex[] data, int arrayLength, int startingIndex = 0)
        {
            int lineNumber = 0;
            using (FileStream stream = File.OpenRead("accelerometer.csv"))
            {
                using(StreamReader reader = new StreamReader(stream))
                {
                    reader.ReadLine(); //skip the header

                    while(lineNumber++ < startingIndex)
                    {
                        reader.ReadLine();
                    }

                    for(int i = 0; i < arrayLength; i++)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');
                        data[i] = new Complex( Convert.ToDouble(values[3]), 0); //imaginary part should always be 0... i think
                        lineNumber++;
                    }
                }
            }

            return lineNumber;
        }
    }
}
