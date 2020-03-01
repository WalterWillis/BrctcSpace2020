using BrctcSpaceLibrary.Device;
using System;
using System.Collections.Generic;
using System.Device.Spi;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using static BrctcSpaceLibrary.Device.Accelerometer;

namespace BrctcSpaceLibrary.Vibe2020Programs
{
    public class AccelerometerOnly : ISingleDevice
    {
        private IMcp3208 _accelerometerDevice;
        private int _segmentLength = 12;
        private string _fileName;

        //Only used by outside methods. Should always be the same as the internal filename
        public static string FileName { get => Path.Combine(Directory.GetCurrentDirectory(), "AccelerometerOnly.binary"); }

        // Keeps track of the amount of data sets for our test
        private long _datasetCounter = 0;
        // Byte chunk size for the MemoryStream -- amount of bytes to hold in memory beforing writing to disk
        private long _chunkSize;

        public AccelerometerOnly(bool useCustomAdcCode = true)
        {
            var settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

            if (useCustomAdcCode)
            {
                using (SpiDevice spi = SpiDevice.Create(settings))
                {
                    _accelerometerDevice = new Mcp3208Custom(spi, (int)Channel.X, (int)Channel.Y, (int)Channel.Z);
                }
            }
            else
            {
                _accelerometerDevice = new Accelerometer(settings);
            }
            _fileName = Path.Combine(Directory.GetCurrentDirectory(), "AccelerometerOnly.binary");

            //Initialize to approximately 1 MB (or as close it as possible given the segment size)
            _chunkSize = (4096 / _segmentLength) * 256;
        }

        public void Run(double timeLimit, System.Threading.CancellationToken token)
        {
            Span<byte> accelSegment = new Span<byte>(new byte[_segmentLength]);

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
                            _accelerometerDevice.Read(accelSegment);
                            stream.Write(accelSegment);

                            _datasetCounter++;

                            accelSegment.Clear();
                        }
                        stream.WriteTo(fs);
                        fs.Flush();
                        stream.Position = 0;
                    }

                    stopwatch.Stop();

                    Console.WriteLine($"AccelerometerOnly program ran for {stopwatch.Elapsed.TotalSeconds} seconds" +
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

        ~AccelerometerOnly()
        {
            _accelerometerDevice.Dispose();
        }
    }
}
