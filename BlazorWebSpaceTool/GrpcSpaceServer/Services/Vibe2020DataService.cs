using BrctcSpaceLibrary;
using BrctcSpace;
using GrpcSpaceServer.Device;
using GrpcSpaceServer.Services.Interfaces;
using Iot.Device.CpuTemperature;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcSpaceServer.Services
{
    public class Vibe2020DataService : IVibe2020DataService
    {
        private readonly ILogger _logger;
        private Task _dataTask;

        private List<DeviceDataModel> _buffer = new List<DeviceDataModel>();
        private static object _locker = new Object();
        private Accelerometer _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private RTC _rtcDevice;
        private CpuTemperature _cpuDevice;

        private bool _useAccel, _useGyro, _useRtc, _useCpu;

        //holds the latest status
        private ResultStatus _status = ResultStatus.None;

        public Vibe2020DataService(ILogger<Vibe2020DataService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Starts the asynchronous gathering of data
        /// </summary>
        public void Initialize(bool useAccel = true, bool useGyro = true, bool useRtc = true, bool useCpu = true)
        {
            lock (_locker)
            {
                _useAccel = useAccel;
                _useGyro = useGyro;
                _useRtc = useRtc;
                _useCpu = useCpu;

                Configure();

                //Occurs per GRPC request, so only start once or restart if completed
                if (_dataTask == null || _dataTask.IsCompleted)
                {
                    _dataTask = Task.Run(GatherData);
                }
            }
        }

        private void Configure()
        {

            try
            {
                _accelerometerDevice = new Accelerometer();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
                //Remove the none status if it exists
                _status |= ResultStatus.AccelerometerFailure;
            }
            try
            {
                _gyroscopeDevice = new Gyroscope();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
                _status |= ResultStatus.GyroscopeFailure;
            }
            try
            {
                _rtcDevice = new RTC();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
                _status |= ResultStatus.RTCFailure;
            }
            try
            {
                _cpuDevice = new CpuTemperature();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
                _status |= ResultStatus.CpuTempReadFailure;
            }

        }

        private void GatherData()
        {
            while (true)
            {
                bool isFull = false;
                DateTime startTime = DateTime.Now;
                lock (_locker)
                {
                    if (_buffer.Count >= 100000)
                    {
                        isFull = true;
                    }
                    else
                    {
                        //Can't guarentee order of assignment, so simply initialize as none
                        _status = ResultStatus.None;
                        DeviceDataModel model = new DeviceDataModel();

                        if (_useAccel)
                            model.AccelData.AddRange(GetAccelerometerResults() ?? new int[0]);
                        if (_useGyro)
                            model.GyroData.AddRange(GetGyroscopeResults() ?? new int[0]);
                        if (_useRtc)
                            model.TransactionTime = GetRTCResults();
                        if (_useCpu)
                            model.CpuTemp = GetCPUTemperatureResults();

                        //Remove None flag
                        _status &= ResultStatus.None;

                        _buffer.Add(model);
                    }
                }

                if (isFull)
                {
                    _logger.LogInformation($"Buffer full. Created {_buffer.Count} items in {(DateTime.Now - startTime).TotalSeconds} seconds!");
                    Thread.Sleep(5000);
                }
            }
        }

        /// <summary>
        /// Retrieves the currently buffered data, clearing the buffer in the process
        /// </summary>
        /// <returns></returns>
        public List<DeviceDataModel> GetData()
        {
            lock (_locker)
            {
                //clone the list to prevent reference clearing
                List<DeviceDataModel> data = new List<DeviceDataModel>(_buffer);
                _buffer.Clear();
                return data;
            }
        }

        private int[] GetAccelerometerResults()
        {
            int[] results = null;

            try
            {
                results = _accelerometerDevice.GetRaws().ToArray();
                _status |= ResultStatus.AccelerometerSuccess;
            }
            catch
            {
                _status |= ResultStatus.AccelerometerFailure;
            }

            return results;
        }

        private int[] GetGyroscopeResults()
        {
            Span<int> results = null;
            try
            {
                results = _gyroscopeDevice.BurstRead();
                _status |= ResultStatus.GyroscopeSuccess;
            }
            catch
            {
                _status |= ResultStatus.GyroscopeFailure;
            }

            return results.ToArray();
        }

        private long GetRTCResults()
        {
            DateTime results;

            try
            {
                results = _rtcDevice.GetCurrentDate();
                _status |= ResultStatus.RTCSuccess;
            }
            catch
            {
                //Timestamp must be in UTC
                results = DateTime.UtcNow;
                _status |= ResultStatus.RTCFailure;
            }

            return results.Ticks;
        }

        private double GetCPUTemperatureResults()
        {
            double results = double.NaN;

            try
            {
                results = _cpuDevice.Temperature.Fahrenheit;
            }
            catch
            {

                _status = ResultStatus.CpuTempReadFailure;
            }

            return results;
        }
    }
}
