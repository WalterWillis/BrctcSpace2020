using FFTW.NET;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace BrctcCalculations
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

        [TestMethod]
        public void GetReliableDataTest()
        {
            Complex[] input = GetDataAtSecond(4);
            Complex[] output = new Complex[input.Length];

            //for (int i = 0; i < input.Length; i++)
            //    input[i] = Math.Sin(i * 2 * Math.PI * 128 / input.Length);

            using (var pinIn = new PinnedArray<Complex>(input))
            using (var pinOut = new PinnedArray<Complex>(output))
            {
                DFT.FFT(pinIn, pinOut, nThreads: 12);
               
            }

            Console.WriteLine("Frequency,\tReal,\tImaginary,\tMagnitude,\tPhase");
            for (int i = 0; i < input.Length; i++)
            {
                Complex result = output[i] / input[i];
                Console.WriteLine($"{output[i].Real},\t{result.Real},\t{result.Imaginary},\t{result.Magnitude},\t{result.Phase}");
            }
        }

        [TestMethod]
        public void GetFrequencyAssociations()
        {
            Complex[] input = GetDataAtSecond(4);
            Complex[] output = new Complex[input.Length];

            //for (int i = 0; i < input.Length; i++)
            //    input[i] = Math.Sin(i * 2 * Math.PI * 128 / input.Length);

            using (var pinIn = new PinnedArray<Complex>(input))
            using (var pinOut = new PinnedArray<Complex>(output))
            {
                DFT.FFT(pinIn, pinOut, nThreads: 12);

            }

            Dictionary<int, Complex> results = new Dictionary<int, Complex>();
            Console.WriteLine("Frequency,\tReal,\tImaginary,\tMagnitude,\tPhase");
            for (int i = 0; i < input.Length; i++)
            {
                results.Add(i, output[i] / input[i]);
            }

            int amount = 20;

            var max = (from result in results
                       where true
                       orderby result.Value.Magnitude descending
                       select result).Take(amount);

            for(int i = 0; i < amount; i++)
            {
                Console.WriteLine($"Frequency (MAX): {max.ElementAt(i).Key}");
                Console.WriteLine($"Magnitude (MAX): {max.ElementAt(i).Value.Magnitude}");
                Console.WriteLine();
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

        private Complex[] GetDataAtSecond(int second)
        {
            List<Complex> data = new List<Complex>();

            using (FileStream stream = File.OpenRead("accel_WithSeconds.csv"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    List<string> headers = reader.ReadLine().Split(',').ToList();
                    bool allLinesFound = false;
                    string line;


                    while ((line = reader.ReadLine()) != null && !allLinesFound) //ensure we don't exceed the lines on accident by checking for null
                    {
                        List<string> values = line.Split(',').ToList();
                        int currentSecond = Convert.ToInt32(values[headers.IndexOf("TRANSACTION_TIME_TICKS")]);
                        int accelYIndex = Convert.ToInt32(headers.IndexOf("ACCEL_Y"));

                        if (currentSecond == second)
                        {
                            data.Add(new Complex(Convert.ToDouble(values[accelYIndex]), 0)); //imaginary part should always be 0... i think
                        }
                        else if(currentSecond > second)
                        {
                            //break loop if we exceed wanted amount of seconds
                            allLinesFound = true;
                        }
                    }
                }
            }
            return data.ToArray();
        }

        public static double Index2Freq(int i, double samples, int nFFT)
        {
            return (double)i * (samples / nFFT / 2.0);
        }

        public static int Freq2Index(double freq, double samples, int nFFT)
        {
            return (int)(freq / (samples / nFFT / 2.0));
        }
    }
}
