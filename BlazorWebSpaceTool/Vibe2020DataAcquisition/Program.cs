using BrctcSpaceLibrary.WriteTests;
using System;
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
            const int timeLimit = 1000 * 300; //timelimit in miliseconds

            PerformBinaryTest(fileName, timeLimit);
            ReadFile(fileName, convertedfile, timeLimit);
            PerformBinaryChunkTest(fileName, timeLimit);
            ReadFile(fileName, convertedfile, timeLimit);
            PerformSingleThreadBinaryChunkTest(fileName, timeLimit);
            ReadFile(fileName, convertedfile, timeLimit);
            PerformSimpleBinaryTest(fileName, timeLimit);
            ReadFile(fileName, convertedfile, timeLimit);
        }

        private static void PerformBinaryTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Binary Test");
            SimpleFileStreamTest binary = new SimpleFileStreamTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            DateTime startTime = DateTime.Now;
            DateTime endTime;

            binary.Start(token);

            endTime = DateTime.Now;

            Console.WriteLine($"Test complete! Took {endTime.Subtract(startTime).TotalSeconds} seconds to create {binary.DataSetCounter} datasets.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended
        }

        private static void PerformBinaryChunkTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Binary Test using 4KB chunk");
            BinaryChunkWriterTest binary = new BinaryChunkWriterTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            DateTime startTime = DateTime.Now;
            DateTime endTime;

            binary.Start(token);

            endTime = DateTime.Now;

            Console.WriteLine($"Test complete! Took {endTime.Subtract(startTime).TotalSeconds} seconds to create {binary.DataSetCounter} datasets.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended
        }

        private static void PerformSingleThreadBinaryChunkTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Async Binary Test using 4KB chunk");
            SingleThreadBinaryChunkkTest binary = new SingleThreadBinaryChunkkTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            DateTime startTime = DateTime.Now;
            DateTime endTime;

            binary.Start(token);

            endTime = DateTime.Now;

            Console.WriteLine($"Test complete! Took {endTime.Subtract(startTime).TotalSeconds} seconds to create {binary.DataSetCounter} datasets.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended
        }

        private static void PerformSimpleBinaryTest(string fileName, int timeLimit)
        {
            Console.WriteLine("Simple Binary Test using 4KB chunk");
            SimpleBinaryTest binary = new SimpleBinaryTest(fileName);
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeLimit);
            CancellationToken token = source.Token;
            DateTime startTime = DateTime.Now;
            DateTime endTime;

            binary.Start(token);

            endTime = DateTime.Now;

            Console.WriteLine($"Test complete! Took {endTime.Subtract(startTime).TotalSeconds} seconds to create {binary.DataSetCounter} datasets. Ignore next metric.");
            GC.Collect();
            GC.WaitForPendingFinalizers(); //ensure file handle from aquisition has ended
        }

        private static void ReadFile(string fileName, string convertedFile, int timeLimit)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (FileStream newFile = new FileStream(convertedFile, FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(newFile, System.Text.Encoding.UTF8))
                    {
                        long fileLength = fs.Length;

                        const int segmentSize = 48;
                        const int accelBytes = 12;
                        const int gyroBytes = 20;
                        const int rtcBytes = 8;
                        const int cpuBytes = 8;
                        const char comma = ',';

                        Console.WriteLine($"File size is {fs.Length} bytes and estimated datasets is {fs.Length / segmentSize}.  Write Speed is estimated at {(fs.Length / segmentSize) / (timeLimit /1000)} datasets per second!");

                        for (long i = 0; i < fs.Length; i += segmentSize)
                        {
                            fs.Seek(i, SeekOrigin.Begin);
                            byte[] bytes = new byte[segmentSize];

                            fs.Read(bytes, 0, segmentSize);

                            Span<byte> segment = new Span<byte>(bytes);

                            var accelSlice = segment.Slice(0, accelBytes);
                            var accel = MemoryMarshal.Cast<byte, int>(accelSlice).ToArray();

                            var gyroSlice = segment.Slice(accelBytes, gyroBytes);
                            var gyro = MemoryMarshal.Cast<byte, short>(accelSlice).ToArray();

                            var rtcSlice = segment.Slice((accelBytes + gyroBytes), rtcBytes);
                            var time = BitConverter.ToInt64(rtcSlice);

                            var cpuSlice = segment.Slice((accelBytes + gyroBytes + rtcBytes), cpuBytes);
                            var cpuTemp = BitConverter.ToDouble(cpuSlice);

                            string line = string.Join(comma, accel);
                            line += comma;
                            line += string.Join(comma, gyro);
                            line += comma;
                            line += time;
                            line += comma;
                            line += cpuTemp;

                            sw.WriteLine(line);
                            sw.Flush();
                        }
                    }
                }
            }
        }
    }
}
