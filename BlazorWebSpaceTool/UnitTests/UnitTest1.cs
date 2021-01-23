using FFTW.NET;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void GetSummaryData()
        {
            /*plan
            we should perform the fft
            take the min, max, avg, sd and any other relevant data for a point
            when we analyze we make a graph of the min, max and avg and estimate the signal via those points
            
            The format of the data to return for telemetry:
            The segment of the data, ie. the nth array computed. So if i'm computing the second array of 8k values, all values are associated with segment 2.
            This should allow me to understand the identity of any estimated values and perform statistic calculations better

            So segment, original data(min, max, mean, sd, (median?), frequency domain data(min, max, mean, sd, (median?), timestamp start of array, timestamp last ofarray

            */

            int size = 8192;
            int complexSize = (size / 2) + 1; //for some reason is half the size of the input data... plus 1
            //alligned arrays are far more efficient... although the .net implementation may not be as performant that it could be
            const int alignmentByteSize = 16; //I believe this is the amount of bytes that make up a double, which is then used to make a fully contiguous array.

            using (var timeDomain = new AlignedArrayDouble(alignmentByteSize, size))
            using (var frequencyDomain = new AlignedArrayComplex(alignmentByteSize, complexSize))
            using (var fft = FftwPlanRC.Create(timeDomain, frequencyDomain, DftDirection.Forwards))
            {
                // Set the input after the plan was created as the input may be overwritten
                // during planning
                FillData(timeDomain, size);

                // timeDomain -> frequencyDomain
                fft.Execute();
                Complex[] list = new Complex[frequencyDomain.Length];
                double[] values = new double[timeDomain.Length];

                for (int i = 0; i < frequencyDomain.Length; i++)
                {
                    values[i] = frequencyDomain[i].Real;
                }
                double mean = values.Average();
                double stdev = StdDev(values, mean);
                var max = values.Max();
                var min = values.Min();

                Console.WriteLine("Time Domain");
                Console.WriteLine($"Mean: {mean} | STDev: {stdev} | Max: {max} | Min: {min}");

                values = new double[frequencyDomain.Length];
                for (int i = 0; i < frequencyDomain.Length; i++)
                {
                    list[i] = frequencyDomain[i];
                    values[i] = frequencyDomain[i].Real;
                }
                mean = values.Average();
                stdev = StdDev(values, mean);
                max = values.Max();
                min = values.Min();

                Console.WriteLine("Frequency Domain");
                Console.WriteLine($"Mean: {mean} | STDev: {stdev} | Max: {max} | Min: {min}");
            }

        }

        /// <summary>
        /// Custom Standard Deviation Algorithm
        /// </summary>
        /// <param name="values"></param>
        /// <param name="mean"></param>
        /// <returns></returns>
        public static double StdDev(double[] values, double mean)
        {
            double[] diff = new double[values.Length];

            for (int i = 0; i < values.Length; i++) 
            {
                double temp = (values[i] - mean);
                diff[i] = temp * temp;
            }

            double newMean = diff.Average();
            double stdev = Math.Sqrt(newMean);

            return stdev;
        }

        /// <summary>
        /// Creates a plan that transforms real data to complex data with no IFFT
        /// </summary>
        [TestMethod]
        public void CreateR2CPlan()
        {
            int size = 8192;
            int complexSize = (size / 2) + 1; //for some reason is half the size of the input data... plus 1
            //alligned arrays are far more efficient... although the .net implementation may not be as performant that it could be
            const int alignmentByteSize = 16; //I believe this is the amount of bytes that make up a double, which is then used to make a fully contiguous array.
            
            using (var timeDomain = new AlignedArrayDouble(alignmentByteSize, size))
            using (var frequencyDomain = new AlignedArrayComplex(alignmentByteSize, complexSize))
            using (var fft = FftwPlanRC.Create(timeDomain, frequencyDomain, DftDirection.Forwards))
            {
                // Set the input after the plan was created as the input may be overwritten
                // during planning
                FillData(timeDomain, size);

                // timeDomain -> frequencyDomain
                fft.Execute();

                for(int i = 0; i< frequencyDomain.Length; i++)
                {
                    Console.WriteLine(frequencyDomain[i]);                   
                }

            }
        }

        [TestMethod]
        public void CreateC2CPlan()
        {
            using (var timeDomain = new FftwArrayComplex(253))
            using (var frequencyDomain = new FftwArrayComplex(timeDomain.GetSize()))
            using (var fft = FftwPlanC2C.Create(timeDomain, frequencyDomain, DftDirection.Forwards))
            using (var ifft = FftwPlanC2C.Create(frequencyDomain, timeDomain, DftDirection.Backwards))
            {
                // Set the input after the plan was created as the input may be overwritten
                // during planning
                for (int i = 0; i < timeDomain.Length; i++)
                    timeDomain[i] = i % 10;

                // timeDomain -> frequencyDomain
                fft.Execute();

                for (int i = frequencyDomain.Length / 2; i < frequencyDomain.Length; i++)
                    frequencyDomain[i] = 0;

                // frequencyDomain -> timeDomain
                ifft.Execute();
            }
        }
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
            
            FillData(input, input.Length);
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
        private int FillData(Complex[] data, int arrayLength, int startingIndex = 0)
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

        private int FillData(AlignedArrayDouble data, int arrayLength, int startingIndex = 0)
        {
            int lineNumber = 0;
            using (FileStream stream = File.OpenRead("accelerometer.csv"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    reader.ReadLine(); //skip the header

                    while (lineNumber++ < startingIndex)
                    {
                        reader.ReadLine();
                    }

                    for (int i = 0; i < arrayLength; i++)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');
                        data[i] = Convert.ToDouble(values[3]); //imaginary part should always be 0... i think
                        lineNumber++;
                    }
                }
            }

            return lineNumber;
        }
    }
}
