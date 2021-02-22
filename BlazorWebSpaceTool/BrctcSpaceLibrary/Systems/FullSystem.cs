using BrctcSpaceLibrary.DataModels;
using BrctcSpaceLibrary.Device;
using BrctcSpaceLibrary.Processes;
using Iot.Device.CpuTemperature;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrctcSpaceLibrary.Systems
{    
    public class FullSystem
    {
        private AccelerometerSystem AccelerometerSystem;
        private GyroscopeSystem GyroscopeSystem;

        private IRTC _rtcDevice;
        private IUART _uartDevice;
        private CpuTemperature _cpuDevice;
        private IGPIO _gpio;

        private FileStream _gyroStream;

        public string AccelFileName { get => AccelerometerSystem.FileName; }

        

        public FullSystem(IMcp3208 mcp, IGyroscope gyro, IRTC rtc, IUART uartDevice, IGPIO gpio)
        {
            _rtcDevice = rtc;
            _cpuDevice = new CpuTemperature();
            _uartDevice = uartDevice;

            string subDir = $"FullSystemSharedRTC_{_rtcDevice.GetCurrentDate().ToString("yyyy-MM-dd-HH-mm-ss")}"; //should be used in final program


            Directory.CreateDirectory(subDir);
            string accelFileName = Path.Combine(Directory.GetCurrentDirectory(), subDir, "Accelerometer.binary");
            string gyroFileName = Path.Combine(Directory.GetCurrentDirectory(), subDir, "Gyroscope.binary");

            AccelerometerSystem = new AccelerometerSystem(mcp, accelFileName, _cpuDevice, _rtcDevice);
            GyroscopeSystem = new GyroscopeSystem(gyro, gyroFileName, _cpuDevice, _rtcDevice);

            _gpio = gpio;
        }

        /// <summary>
        /// Sets the amount of chunks to be written per file
        /// </summary>
        /// <param name="size"></param>
        public void SetChunkAmount(int size)
        {
            AccelerometerSystem.MaxChunksPerFile = size;
        }

        public void Run(CancellationToken token)
        {
            _gyroStream = new FileStream(GyroscopeSystem.FileName, FileMode.Create, FileAccess.Write);

            GyroscopeSystem.GyroStream = _gyroStream;

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            Task accelThread = Task.Run(() => { AccelerometerSystem.RunAccelerometer(token); });

            Task telemetryThread = Task.Run(() => Telemetry(token));

            _gpio.RegisterCallbackForPinValueChangedEvent(PinEventTypes.Rising, GyroscopeSystem.DataAquisitionCallback);

            bool loopBreakerProgramMaker = false;

            while (!loopBreakerProgramMaker)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Time limit reached! Ending program...");
                    loopBreakerProgramMaker = true;
                    stopwatch.Stop();
                }
                else
                    Thread.SpinWait(50);
            }

            accelThread.Wait();
            telemetryThread.Wait();

            _gpio.UnregisterCallbackForPinValueChangedEvent(GyroscopeSystem.DataAquisitionCallback);
            _gyroStream.Dispose();

            Console.WriteLine($"FullSystemSharedRTC program ran for {stopwatch.Elapsed.TotalSeconds} seconds" +
                $" creating {AccelerometerSystem.AccelDatasetCounter} accelerometer datasets at " +
                $"{AccelerometerSystem.AccelDatasetCounter / stopwatch.Elapsed.TotalSeconds} datasets per second and" +
                $" {GyroscopeSystem.GyroDataSetCounter} gyroscope datasets at " +
                $"{GyroscopeSystem.GyroDataSetCounter / stopwatch.Elapsed.TotalSeconds} datasets per second");
        }

        private void Telemetry(CancellationToken token)
        {
            long indexTracker = 0; //tracks the current index of each line over multiple files
            int fileSent = 1;
            long currentSecond = 1;
            TemperatureModel temperature = new TemperatureModel();
            int prevSecond = 0;
            bool isInitialDateTime = true;
            AccelerometerDataAnalysis processor = new AccelerometerDataAnalysis();

            int accelBytes = AccelerometerSystem.AccelBytes;
            int rtcBytes = AccelerometerSystem.Rtcbytes;
            int cpuBytes = AccelerometerSystem.CpuBytes;
            int accelSegmentLength = accelBytes + rtcBytes + cpuBytes;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    _uartDevice = _uartDevice.GetUART();

                    if (!AccelerometerSystem.FileQueue.IsEmpty)
                    {
                        Console.WriteLine($"Sending file # {fileSent}");
                        AccelerometerSystem.FileQueue.TryDequeue(out string fileName);
                        //logic to handle failed dequeue needs to be here
                        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                        {
                            byte[] bytes = new byte[accelSegmentLength];

                            while (fs.Read(bytes) != 0) //while data is in the stream, continue 
                            {
                                if (token.IsCancellationRequested)
                                {
                                    Console.WriteLine("Time's Up! Cancelling telemetry.");
                                    break;
                                }
                                Span<byte> data = bytes;                                
                                Span<byte> accelSegment = data.Slice(0, accelBytes);                               
                                Span<byte> rtcSegment = data.Slice(accelBytes, rtcBytes);                               
                                Span<byte> cpuSegment = data.Slice(accelBytes + rtcBytes, cpuBytes);

                                DateTime currentTime = new DateTime(BitConverter.ToInt64(rtcSegment));
                               
                                if (isInitialDateTime)
                                {
                                    prevSecond = currentTime.Second;
                                    isInitialDateTime = false;
                                }

                                //as long as the seconds match, get the data
                                if (prevSecond == currentTime.Second)
                                {
                                    //add data for each second
                                    temperature.GetNextAverage(BitConverter.ToDouble(cpuSegment));
                                    processor.ProcessData(accelSegment);
                                }
                                else
                                {
                                    //perform analysis and send message on second change
                                    processor.PerformFFTAnalysis();

                                    //iterate second and append all data. Processor data should already have commas
                                    string message = $"{currentSecond++},{temperature.AverageCPUTemp}{processor.X_Magnitudes}{processor.Y_Magnitudes}{processor.Z_Magnitudes}";

                                    try
                                    {
                                        _uartDevice.SerialSend(message);
                                        indexTracker++;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to send!\n{ex.Message}\n{ex.StackTrace}");
                                        _uartDevice.Dispose();
                                        _uartDevice = _uartDevice.GetUART(); // let's refresh the interface
                                    }

                                    //reset processor and temperature averge and begin new data set here
                                    processor.Reset();
                                    temperature.Reset();
                                    temperature.GetNextAverage(BitConverter.ToDouble(cpuSegment));
                                    processor.ProcessData(accelSegment);
                                }

                                prevSecond = currentTime.Second;

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
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    _uartDevice.Dispose();
                }
            }
        }

    }
}
