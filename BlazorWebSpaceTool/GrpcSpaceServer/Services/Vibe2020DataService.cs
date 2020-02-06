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

        private const short gyroProductID = 16460;
        private Accelerometer _accelerometerDevice;
        private Gyroscope _gyroscopeDevice;
        private RTC _rtcDevice;
        private CpuTemperature _cpuDevice;

        //holds the latest status
        private ResultStatus _status = ResultStatus.None;

        public Vibe2020DataService(ILogger<Vibe2020DataService> logger)
        {
            _logger = logger;
            _accelerometerDevice = new Accelerometer();
            _gyroscopeDevice = new Gyroscope();
            _rtcDevice = new RTC();
            _cpuDevice = new CpuTemperature();
        }


        public DeviceDataModel GetSingleReading()
        {
            _status = ResultStatus.None;

            DeviceDataModel model = new DeviceDataModel();

            model.AccelData.AddRange(GetAccelerometerResults() ?? new int[0]);
            model.GyroData.AddRange(GetGyroscopeResults() ?? new int[0]);
            model.TransactionTime = GetRTCResults();
            model.CpuTemp = GetCPUTemperatureResults();

            //Remove None flag
            _status &= ResultStatus.None;

            model.ResultStatus = (int)_status;

            return model;
        }

        public DeviceDataModel[] GetReadings(int numReadings = 1000)
        {
            Span<DeviceDataModel> dataModels = new DeviceDataModel[numReadings];
            DateTime startTime = DateTime.Now;
            for(int i = 0; i < numReadings; i++)
            {
                _status = ResultStatus.None;

                DeviceDataModel model = new DeviceDataModel();

                model.AccelData.AddRange(GetAccelerometerResults() ?? new int[0]);
                model.GyroData.AddRange(GetGyroscopeResults() ?? new int[0]);
                model.TransactionTime = GetRTCResults();
                model.CpuTemp = GetCPUTemperatureResults();
                model.ResultStatus = (int)_status;

                dataModels[i] = model;
            }
            _logger.LogInformation($"Finished {numReadings} items in {(DateTime.Now - startTime).TotalSeconds} seconds!");

            return dataModels.ToArray();
        }

        public bool isGyroValid()
        {
            short regValue = _gyroscopeDevice.RegisterRead(Gyroscope.Register.PROD_ID);
            _logger.LogInformation($"ADIS16460 Prod ID Register reads {regValue}.");
            return regValue.Equals(gyroProductID);
        }


        private int[] GetAccelerometerResults()
        {
            int[] results = null;

            try
            {
                results = _accelerometerDevice.GetRaws();
                _status |= ResultStatus.AccelerometerSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error with Accelerometer");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
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
            catch (Exception ex)
            {
                _logger.LogError("Error with Gyro");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
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
            catch (Exception ex)
            {
                _logger.LogError("Error with RTC");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
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
            catch (Exception ex)
            {
                _logger.LogError("Error with CPU Temp");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                _status = ResultStatus.CpuTempReadFailure;
            }

            return results;
        }

        ~Vibe2020DataService()
        {
            _accelerometerDevice.Dispose();
            _gyroscopeDevice.Dispose();
            _rtcDevice.Dispose();
        }
    }
}
