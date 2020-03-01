using BrctcSpaceLibrary;
using BrctcSpaceLibrary.WriteTests;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Vibe2020DataAcquisition
{
    class Program
    {
        static void Main(string[] args)
        {


            string fileName = Path.Combine(Directory.GetCurrentDirectory(), "test.txt");
            string convertedfile = Path.Combine(Directory.GetCurrentDirectory(), "converted.txt");
            const int timeLimit = 1000 * 60 * 2; //timelimit in miliseconds - millisceonds * seconds * minutes

            double timeTaken;
            //timeTaken = PerformBinaryTest(fileName, timeLimit);
            //ReadFile(fileName, convertedfile, timeTaken);
            timeTaken = PerformBinaryChunkTest(fileName, timeLimit);
            ReadFile(fileName, convertedfile, timeTaken);
            //timeTaken = PerformScaledBinaryChunkTest(fileName, timeLimit);
            //ReadFile(fileName, convertedfile, timeLimit, accelBytes:24);
            //timeTaken = PerformSingleThreadBinaryChunkTest(fileName, timeLimit);
            //ReadFile(fileName, convertedfile, timeTaken);
            //timeTaken = PerformSimpleBinaryTest(fileName, timeLimit);
            //ReadFile(fileName, convertedfile, timeTaken);

            //PerformInMemoryTest(timeLimit);
        }

        private static double PerformBinaryTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Binary Test");
            SimpleFileStreamTest binary = new SimpleFileStreamTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            binary.Start(token);

            stopwatch.Stop();
            Console.WriteLine($"Test complete! Took {stopwatch.Elapsed.TotalSeconds} seconds to create {binary.DataSetCounter} datasets.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended

            return stopwatch.Elapsed.TotalSeconds;
        }

        private static double PerformBinaryChunkTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Binary Test using 4KB chunk");
            BinaryChunkWriterTest binary = new BinaryChunkWriterTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            binary.Start(token);

            stopwatch.Stop();

            Console.WriteLine($"Test complete! Took {stopwatch.Elapsed.TotalSeconds} seconds to create {binary.DataSetCounter} datasets.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended

            return stopwatch.Elapsed.TotalSeconds;
        }

        private static double PerformScaledBinaryChunkTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Scaled Binary Test using 4KB chunk");
            BinaryChunkWriterScaledTest binary = new BinaryChunkWriterScaledTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            binary.Start(token);

            stopwatch.Stop();

            Console.WriteLine($"Test complete! Took {stopwatch.Elapsed.TotalSeconds} seconds to create {binary.DataSetCounter} datasets.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended

            return stopwatch.Elapsed.TotalSeconds;
        }

        private static double PerformSingleThreadBinaryChunkTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Async Binary Test using 4KB chunk");
            SingleThreadBinaryChunkkTest binary = new SingleThreadBinaryChunkkTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            binary.Start(token);

            stopwatch.Stop();

            Console.WriteLine($"Test complete! Took {stopwatch.Elapsed.TotalSeconds} seconds to create {binary.DataSetCounter} datasets.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended

            return stopwatch.Elapsed.TotalSeconds;
        }

        private static double PerformSimpleBinaryTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Simple Binary Test using 4KB chunk");
            SimpleBinaryTest binary = new SimpleBinaryTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            binary.Start(token);

            stopwatch.Stop();

            Console.WriteLine($"Test complete! Took {stopwatch.Elapsed.TotalSeconds} seconds to create {binary.DataSetCounter} datasets. Ignore next metric.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended

            return stopwatch.Elapsed.TotalSeconds;
        }

        private static double PerformInMemoryTest(int timeLimit)
        {
            Console.WriteLine("Simple PureMemory (No SaveFile) Test using 4KB chunk");
            PureMemoryTest binary = new PureMemoryTest();
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            binary.Start(token);

            stopwatch.Stop();

            Console.WriteLine($"Test complete! Took {stopwatch.Elapsed.TotalSeconds} seconds to create {binary.DataSetCounter} datasets at {binary.DataSetCounter / (timeLimit / 1000)} datasets/second.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended

            return stopwatch.Elapsed.TotalSeconds;
        }

        //Pretty inefficient, but no worries. This doesn't require efficient speed
        private static void ReadFile(string fileName, string convertedFile, double timeTaken, int accelBytes = 12, int gyroBytes = 20, int rtcBytes = 8, int cpuBytes = 8)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (FileStream newFile = new FileStream(convertedFile, FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(newFile, System.Text.Encoding.UTF8))
                    {
                        long fileLength = fs.Length;

                        int segmentSize = accelBytes + gyroBytes + rtcBytes + cpuBytes;

                        const string header = "ACCEL_X_RAW,ACCEL_Y_RAW,ACCEL_Z_RAW,ACCEL_X,ACCEL_Y,ACCEL_Z," +
                 "DIAG_STAT_RAW,GYRO_X_Raw,GYRO_Y_RAW,GYRO_Z_RAW,ACCEL_X_RAW,ACCEL_Y_RAW,ACCEL_Z_RAW,TEMP_RAW,SPS_RAW,CHECKSUM_RAW," +
                 "DIAG_STAT,GYRO_X,GYRO_Y,GYRO_Z,ACCEL_X,ACCEL_Y,ACCEL_Z,TEMP,SPS,CHECKSUM," +
                 "TRANSACTION_TIME_TICKS,CPU_TEMP";

                        sw.WriteLine(header);

                        const char comma = ',';

                        Console.WriteLine($"File size is {fs.Length} bytes and estimated datasets is {fs.Length / segmentSize}.  Write Speed is estimated at {(fs.Length / segmentSize) / timeTaken} datasets per second!");

                        int bufferCounter = 0;
                        int bufferSize = 200;

                        for (long i = 0; i < fs.Length; i += segmentSize)
                        {
                            byte[] bytes = new byte[segmentSize];

                            fs.Read(bytes, 0, segmentSize);

                            Span<byte> segment = new Span<byte>(bytes);

                            var accelSlice = segment.Slice(0, accelBytes);
                           
                            var accelX = accelBytes == 12 ? BitConverter.ToInt32(accelSlice.ToArray(), 0) : BitConverter.ToDouble(accelSlice.ToArray(), 0);
                            var accelY = accelBytes == 12 ? BitConverter.ToInt32(accelSlice.ToArray(), 4) : BitConverter.ToDouble(accelSlice.ToArray(), 8);
                            var accelZ = accelBytes == 12 ? BitConverter.ToInt32(accelSlice.ToArray(), 8) : BitConverter.ToDouble(accelSlice.ToArray(), 16);

                            var gyroSlice = segment.Slice(accelBytes, gyroBytes);
                            var gyro = MemoryMarshal.Cast<byte, short>(gyroSlice).ToArray();

                            var rtcSlice = segment.Slice((accelBytes + gyroBytes), rtcBytes);
                            var time = BitConverter.ToInt64(rtcSlice);

                            var cpuSlice = segment.Slice((accelBytes + gyroBytes + rtcBytes), cpuBytes);
                            var cpuTemp = BitConverter.ToDouble(cpuSlice);

                            string line = string.Join(comma, accelX);
                            line += comma;
                            line += string.Join(comma, accelY);
                            line += comma;
                            line += string.Join(comma, accelZ);
                            line += comma;
                            line += string.Join(comma, gyro); //Non scaled
                            line += comma;
                            line += string.Join(comma, GyroConversionHelper.GetGyroscopeDetails(gyroSlice).ToArray()); //scaled
                            line += comma;
                            line += time;
                            line += comma;
                            line += cpuTemp;

                            if (bufferCounter++ >= bufferSize)
                            {
                                sw.Flush();
                                bufferCounter = 0;
                            }

                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }
    }
}
