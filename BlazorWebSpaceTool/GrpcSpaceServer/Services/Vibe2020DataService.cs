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

        private List<double[]> _buffer = new List<double[]>();
        private static object _locker = new Object();
        private Accelerometer _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private RTC _rtcDevice;
        private CpuTemperature _cpuDevice;

        //holds the latest status
        private ResultStatus _status = ResultStatus.None;

        public Vibe2020DataService(ILogger<Vibe2020DataService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Starts the asynchronous gathering of data
        /// </summary>
        public void Initialize()
        {
            //Occurs per GRPC request, so only start once or restart if completed
            if (_dataTask == null || _dataTask.IsCompleted)
            {
                Configure();
                _dataTask = Task.Run(GatherData);
            }
        }

        private void Configure()
        {
            lock (_locker)
            {
                try
                {
                    _accelerometerDevice = new Accelerometer();
                }
                catch(Exception ex)
                {
                    _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
                    //Remove the none status if it exists
                    _status |= ResultStatus.AccelerometerFailure & ~ResultStatus.None;
                }
                try
                {
                    _gyroscopeDevice = new Gyroscope();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
                    _status |= ResultStatus.GyroscopeFailure & ~ResultStatus.None;
                }
                try
                {
                    _rtcDevice = new RTC();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
                    _status |= ResultStatus.RTCFailure & ~ResultStatus.None;
                }
                try
                {
                    _cpuDevice = new CpuTemperature();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
                    _status |= ResultStatus.CpuTempReadFailure & ~ResultStatus.None;
                }
            }
        }

        private void GatherData()
        {
            while (true)
            {
                bool isFull = false;
                lock (_locker)
                {
                    if (_buffer.Count >= 100000)
                    {
                        isFull = true;
                    }
                    else
                    {
                        _buffer.Add(new double[] { 1, 2, 3, 4 });
                    }
                }

                if (isFull)
                {
                    _logger.LogInformation("Buffer full, waiting");
                    Thread.Sleep(5000);
                }
            }
        }

        /// <summary>
        /// Retrieves the currently buffered data, clearing the buffer in the process
        /// </summary>
        /// <returns></returns>
        public List<double[]> GetData()
        {
            lock (_locker)
            {
                //clone the list to prevent reference clearing
                List<double[]> data = new List<double[]>(_buffer);
                _buffer.Clear();
                return data;
            }
        }
    }
}
