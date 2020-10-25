using BrctcSpaceLibrary.DataModels;
using BrctcSpaceLibrary.Device;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static BrctcSpaceLibrary.Device.Accelerometer;

namespace BrctcSpaceLibrary.Vibe2020Programs
{
    //future idea. split out file into partial files for each major component. organize under a directory of the class name 
    //followed by the main partial file. then add another directory called "Partials" with the remaining files. 
    //file names can be something like FullSystemSharedRTC_Telemetry.
    //either that or just create some utility functions, so long as they don't take away performance.
    public class FullSystemSharedRTC
    {
        private IMcp3208 _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private RTC _rtcDevice;
        private CpuTemperature _cpuDevice;
        private GpioController _gpio;

        FileStream _gyroStream;

        private const int DR_PIN = 13; //physical pin scheme;
        private const int RST_PIN = 15;

        private int _accelSegmentLength;
        private int _gyroSegmentLength;

        private const int _accelBytes = 12;
        private const int _gyroBytes = 20;
        private const int _rtcBytes = 8;
        private const int _cpuBytes = 8;

        private string _accelFileName;
        private string _gyroFileName;

        private long _accelDatasetCounter = 0;
        private long _gyroDatasetCounter = 0;

        private CancellationTokenSource _source = new CancellationTokenSource();
        private ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();

        #region Test-Only Properties
        public static string TestAccelFile { get => Path.Combine(Directory.GetCurrentDirectory(), "FullSystemSharedRTC", "Accelerometer.binary0"); }
        public static string TestGyroFile { get => Path.Combine(Directory.GetCurrentDirectory(), "FullSystemSharedRTC", "Gyroscope.binary"); }
        #endregion

        public int AccelSegmentLength { get => _accelSegmentLength; }
        public int GyroSegmentLength { get => _gyroSegmentLength; }

        public long AccelDataSetCounter { get => _accelDatasetCounter; }
        public long GyroDataSetCounter { get => _gyroDatasetCounter; }

        public FullSystemSharedRTC(bool useCustomAdcCode = true, bool isTest = true)
        {
            var settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

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

            _gyroscopeDevice = new Gyroscope(new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 900000 });

            _cpuDevice = new CpuTemperature();
            _rtcDevice = new RTC();
            _gpio = new GpioController(PinNumberingScheme.Board);
            _gpio.OpenPin(DR_PIN, PinMode.Input);
            _gpio.OpenPin(RST_PIN, PinMode.Output);
            _gpio.Write(RST_PIN, PinValue.High);  //RST pin should always be High unless we want to reset the gyro

            string subDir;
            if (!isTest)
                subDir = $"FullSystemSharedRTC_{_rtcDevice.GetCurrentDate().ToString("yyyy-MM-dd-HH-mm-ss")}"; //should be used in final program
            else
                subDir = "FullSystemSharedRTC"; //easier to keep track of

            Directory.CreateDirectory(subDir);
            _accelFileName = Path.Combine(Directory.GetCurrentDirectory(), subDir, "Accelerometer.binary");
            _gyroFileName = Path.Combine(Directory.GetCurrentDirectory(), subDir, "Gyroscope.binary");

            _accelSegmentLength = _accelBytes + _rtcBytes + _cpuBytes;
            _gyroSegmentLength = _gyroBytes + _rtcBytes + _cpuBytes;
        }

        public void Run(double timeLimit, CancellationToken token)
        {
            _gyroStream = new FileStream(_gyroFileName, FileMode.Create, FileAccess.Write);

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            Task accelThread = Task.Run(() => { RunAccelerometer(); });

            Task telemetryThread = Task.Run(() => Telemetry());

            _gpio.RegisterCallbackForPinValueChangedEvent(DR_PIN, PinEventTypes.Rising, DataAquisitionCallback);     

            bool loopBreakerProgramMaker = false;

            while (!loopBreakerProgramMaker)
            {
                if (stopwatch.Elapsed.TotalMinutes >= timeLimit)
                {
                    Console.WriteLine("Time limit reached! Ending program...");
                    _source.Cancel();                   
                    loopBreakerProgramMaker = true;
                    stopwatch.Stop();
                }
                else if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Program cancelled! Ending program...");
                    _source.Cancel();
                    loopBreakerProgramMaker = true;
                    stopwatch.Stop();
                }
                else
                    Thread.SpinWait(50);
            }

            accelThread.Wait();
            telemetryThread.Wait();

            _gpio.UnregisterCallbackForPinValueChangedEvent(DR_PIN, DataAquisitionCallback);
            _gyroStream.Close();

            Console.WriteLine($"FullSystemSharedRTC program ran for {stopwatch.Elapsed.TotalSeconds} seconds" +
                $" creating {_accelDatasetCounter} accelerometer datasets at {_accelDatasetCounter / stopwatch.Elapsed.TotalSeconds} datasets per second and" +
                $" {_gyroDatasetCounter} gyroscope datasets at {_gyroDatasetCounter / stopwatch.Elapsed.TotalSeconds} datasets per second");
        }

        private void RunAccelerometer()
        {
            CancellationToken token = _source.Token;
            Span<byte> data = new Span<byte>(new byte[_accelSegmentLength]);

            Span<byte> accelSegment = data.Slice(0, _accelBytes);
            Span<byte> rtcSegment = data.Slice(_accelBytes, _rtcBytes);
            Span<byte> cpuSegment = data.Slice(_accelBytes + _rtcBytes, _cpuBytes);

            const int secondaryDataTrigger = 7999; //subtract one from expected/wanted samples per second since counter starts at 0
            int secondaryDataCounter = 0;

            int fileCounter = 0;

            //Initialize to approximately 256 KB (or as close it as possible given the segment size)
            int chunkSize = (4096 / _accelSegmentLength) * 256;

            int maxLines = chunkSize * 25; //arbitrary amount of iterations per buffer cycle

            int iterations = maxLines / chunkSize;

            while (!token.IsCancellationRequested)
            {
                string fileName = _accelFileName + fileCounter.ToString();

                using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        //initialize
                        GetRTCTime(rtcSegment);
                        GetCPUTemp(cpuSegment);
                      
                        for (int iteration = 0; iteration < iterations; iteration++)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            for (int i = 0; i < chunkSize; i++)
                            {
                                _accelerometerDevice.Read(accelSegment);
                                if (secondaryDataCounter++ >= secondaryDataTrigger)
                                {
                                    GetRTCTime(rtcSegment);
                                    GetCPUTemp(cpuSegment);
                                    secondaryDataCounter = 0;
                                }

                                stream.Write(data);

                                _accelDatasetCounter++;

                                accelSegment.Clear(); // only clear the accelerometer values as they must be ensured to be precise
                            }

                            stream.WriteTo(fs);
                            fs.Flush();
                            stream.Position = 0;
                        }
                    }
                }
                fileCounter++;
                fileQueue.Enqueue(fileName);
            }
        }

        private void DataAquisitionCallback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Span<byte> data = new Span<byte>(new byte[_gyroSegmentLength]);

            Span<byte> gyroSegment = data.Slice(0, _gyroBytes);
            Span<byte> rtcSegment = data.Slice(_gyroBytes, _rtcBytes);
            Span<byte> cpuSegment = data.Slice(_gyroBytes + _rtcBytes, _cpuBytes);

            _gyroscopeDevice.BurstRead(gyroSegment);
            GetRTCTime(rtcSegment);
            GetCPUTemp(cpuSegment);

            _gyroDatasetCounter++;

            _gyroStream.Write(data);
            _gyroStream.Flush();
        }

        private void Telemetry()
        {
            CancellationToken token = _source.Token;
            long indexTracker = 0; //tracks the current index of each line over multiple files
            int fileSent = 1;
            UART telemetry = null;
            while (!token.IsCancellationRequested)
            {              
                try
                {
                    telemetry = new UART();

                    if (!fileQueue.IsEmpty)
                    {
                        Console.WriteLine($"Sending file # {fileSent}");
                        fileQueue.TryDequeue(out string fileName);
                        //logic to handle failed dequeue needs to be here
                        Vibe2020TelemetryModel model = new Vibe2020TelemetryModel(); // let's just reuse the same model for efficiency 
                        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                        {
                            byte[] bytes = new byte[AccelSegmentLength];
                            while (fs.Read(bytes) != 0) //while data is in the stream, continue 
                            {
                                if (token.IsCancellationRequested)
                                {
                                    Console.WriteLine("Time's Up! Cancelling telemetry.");
                                    break;
                                }
                                Span<byte> data = bytes;
                                Span<byte> accelSegment = data.Slice(0, _accelBytes);
                                Span<byte> rtcSegment = data.Slice(_accelBytes, _rtcBytes);
                                Span<byte> cpuSegment = data.Slice(_accelBytes + _rtcBytes, _cpuBytes);

                                model.Index = indexTracker++;
                                model.AccelData_Raw = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>(accelSegment).ToArray();
                                model.TransactionTime = BitConverter.ToInt64(rtcSegment);
                                model.CpuTemp = BitConverter.ToDouble(cpuSegment);

                                try
                                {
                                    telemetry.SerialSend(model.ToString());
                                }
                                catch
                                {
                                    Console.WriteLine("Failed to send!");
                                    telemetry.Dispose();
                                    telemetry = new UART(); // let's refresh the interface
                                }

                                data.Clear(); // since we are reusing this array, clear the values for integrity                                  
                                }
                            }
                        Console.WriteLine($"Finished sending file #{fileSent++} at {indexTracker} total lines transmitted!");
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);                   
                }
                finally
                {
                    telemetry.Dispose();
                }
            }
        }

        private void GetRTCTime(Span<byte> buffer)
        {
            Monitor.Enter(_rtcDevice);
            _rtcDevice.GetCurrentDate(buffer);
            Monitor.Exit(_rtcDevice);
        }

        private void GetCPUTemp(Span<byte> buffer)
        {
            Monitor.Enter(_cpuDevice);
            if (_cpuDevice.IsAvailable)
            {
                var temp = BitConverter.GetBytes(_cpuDevice.Temperature.Fahrenheit);

                buffer[0] = temp[0];
                buffer[1] = temp[1];
                buffer[2] = temp[2];
                buffer[3] = temp[3];
                buffer[4] = temp[4];
                buffer[5] = temp[5];
                buffer[6] = temp[6];
                buffer[7] = temp[7];
            }
            Monitor.Exit(_cpuDevice);
        }

        ~FullSystemSharedRTC()
        {
            _accelerometerDevice.Dispose();
            _gyroscopeDevice.Dispose();
            try { _gpio.UnregisterCallbackForPinValueChangedEvent(DR_PIN, DataAquisitionCallback); } catch { } // ensure this is removed
            _gpio.ClosePin(DR_PIN);
            _gpio.ClosePin(RST_PIN);
            _gpio.Dispose();
            _gyroStream.Dispose();
        }
    }
}
