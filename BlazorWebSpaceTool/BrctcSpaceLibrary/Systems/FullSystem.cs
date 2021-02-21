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
        private GpioController _gpio;

        private FileStream _gyroStream;

        private const int DR_PIN = 13; //physical pin scheme;
        private const int RST_PIN = 15;

        public FullSystem(IMcp3208 mcp, IGyroscope gyro, IRTC rtc, IUART uartDevice)
        {
            _rtcDevice = rtc;
            _cpuDevice = new CpuTemperature();
            _uartDevice = uartDevice;

            string subDir = $"FullSystemSharedRTC_{_rtcDevice.GetCurrentDate().ToString("yyyy-MM-dd-HH-mm-ss")}"; //should be used in final program


            Directory.CreateDirectory(subDir);
            string accelFileName = Path.Combine(Directory.GetCurrentDirectory(), subDir, "Accelerometer.binary");
            string gyroFileName = Path.Combine(Directory.GetCurrentDirectory(), subDir, "Gyroscope.binary");

            AccelerometerSystem = new AccelerometerSystem(mcp, accelFileName, _cpuDevice, _rtcDevice);
            GyroscopeSystem = new GyroscopeSystem(gyro, gyroFileName, _gyroStream, _cpuDevice, _rtcDevice);

            _gpio = new GpioController(PinNumberingScheme.Board);
            _gpio.OpenPin(DR_PIN, PinMode.Input);
            _gpio.OpenPin(RST_PIN, PinMode.Output);
            _gpio.Write(RST_PIN, PinValue.High);  //RST pin should always be High unless we want to reset the gyro

        }

        public void Run(double timeLimit, CancellationToken token)
        {
            _gyroStream = new FileStream(GyroscopeSystem.FileName, FileMode.Create, FileAccess.Write);

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            Task accelThread = Task.Run(() => { AccelerometerSystem.RunAccelerometer(token); });

            Task telemetryThread = Task.Run(() => Telemetry(token));

            _gpio.RegisterCallbackForPinValueChangedEvent(DR_PIN, PinEventTypes.Rising, GyroscopeSystem.DataAquisitionCallback);

            bool loopBreakerProgramMaker = false;

            while (!loopBreakerProgramMaker)
            {
                if (stopwatch.Elapsed.TotalMinutes >= timeLimit)
                {
                    Console.WriteLine("Time limit reached! Ending program...");
                    loopBreakerProgramMaker = true;
                    stopwatch.Stop();
                }
                else if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Program cancelled! Ending program...");
                    loopBreakerProgramMaker = true;
                    stopwatch.Stop();
                }
                else
                    Thread.SpinWait(50);
            }

            accelThread.Wait();
            telemetryThread.Wait();

            _gpio.UnregisterCallbackForPinValueChangedEvent(DR_PIN, GyroscopeSystem.DataAquisitionCallback);
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
            DateTime prevTime = new DateTime(0); //set invalid date to start so comparisons can begin
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
                                TimeSpan diff = currentTime - prevTime;

                                //as long as the seconds match, get the data
                                if (diff.TotalSeconds == 0)
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
                                    string message = $"{currentSecond++},{temperature.AverageCPUTemp},{processor.X_Magnitudes}{processor.Y_Magnitudes}{processor.Z_Magnitudes}";

                                    try
                                    {
                                        _uartDevice.SerialSend(message);
                                        indexTracker++;
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Failed to send!");
                                        _uartDevice.Dispose();
                                        _uartDevice = _uartDevice.GetUART(); // let's refresh the interface
                                    }

                                    //reset processor and temperature averge and begin new data set here
                                    processor.Reset();
                                    temperature.Reset();
                                    temperature.GetNextAverage(BitConverter.ToDouble(cpuSegment));
                                    processor.ProcessData(accelSegment);
                                }

                                prevTime = currentTime;

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
