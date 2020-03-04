using BrctcSpaceLibrary.Device;
using System;
using System.Device.Spi;
using System.Diagnostics;
using System.IO;

namespace BrctcSpaceLibrary.Vibe2020Programs
{
    public class GyroscopeOnly : ISingleDevice
    {
        private Gyroscope _gyroscopeDevice;
        private int _segmentLength = 20;
        private string _fileName;

        //Only used by outside methods. Should always be the same as the internal filename
        public static string FileName { get => Path.Combine(Directory.GetCurrentDirectory(), "GyroscopeOnly.binary"); }

        // Keeps track of the amount of data sets for our test
        private long _datasetCounter = 0;
        // Byte chunk size for the MemoryStream -- amount of bytes to hold in memory beforing writing to disk
        private long _chunkSize;

        public GyroscopeOnly()
        {
            var settings = new SpiConnectionSettings(0, 1) { Mode = SpiMode.Mode3, ClockFrequency = 900000 };

             _gyroscopeDevice = new Gyroscope(settings);

            _fileName = Path.Combine(Directory.GetCurrentDirectory(), "GyroscopeOnly.binary");

            //Initialize to approximately 1 MB (or as close it as possible given the segment size)
            _chunkSize = (4096 / _segmentLength) * 256;
        }

        public void Run(double timeLimit, System.Threading.CancellationToken token)
        {
            Span<byte> gyroSegment = new Span<byte>(new byte[_segmentLength]);

            Stopwatch stopwatch = new Stopwatch();

            using (FileStream fs = new FileStream(_fileName, FileMode.Create, FileAccess.Write))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    stopwatch.Start();

                    while (stopwatch.Elapsed.TotalMinutes < timeLimit && !token.IsCancellationRequested)
                    {

                        for (int i = 0; i < _chunkSize; i++)
                        {
                            _gyroscopeDevice.AcquireData(gyroSegment);
                            stream.Write(gyroSegment);

                            _datasetCounter++;

                            gyroSegment.Clear();
                        }
                        stream.WriteTo(fs);
                        fs.Flush();
                        stream.Position = 0;
                    }

                    stopwatch.Stop();

                    Console.WriteLine($"GyroscopeOnly program ran for {stopwatch.Elapsed.TotalSeconds} seconds" +
                        $" creating {_datasetCounter} datasets at {_datasetCounter / stopwatch.Elapsed.TotalSeconds} datasets per second");
                }
            }

        }

        public long GetDataSetCount()
        {
            return _datasetCounter;
        }

        public string GetFileName()
        {
            return _fileName;
        }

        public int GetSegmentLength()
        {
            return _segmentLength;
        }

        ~GyroscopeOnly()
        {
            _gyroscopeDevice.Dispose();
        }
    }
}
