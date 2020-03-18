using BrctcSpaceLibrary;
using BrctcSpace;
using BrctcSpaceLibrary.Device;
using GrpcSpaceServer.Services.Interfaces;
using Iot.Device.CpuTemperature;
using Microsoft.Extensions.Logging;
using System;
using System.Device.Spi;

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
            try
            {
                var settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1900000 };

                _accelerometerDevice = new Accelerometer(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Initializing Accelerometer.", ex.Message, ex.StackTrace);
            }
            try
            {
                var settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 900000 };

                _gyroscopeDevice = new Gyroscope(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Initializing Gyroscope.", ex.Message, ex.StackTrace);
            }
            try
            {
                _rtcDevice = new RTC();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Initializing RTC.", ex.Message, ex.StackTrace);
            }
            try
            {
                _cpuDevice = new CpuTemperature();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Initializing CPU Temp Device.", ex.Message, ex.StackTrace);
            }
        }


        public DeviceDataModel GetSingleReading()
        {
            _status = ResultStatus.None;

            DeviceDataModel model = new DeviceDataModel();

            model.AccelData.Add(GetAccelerometerResults() ?? new int[0]);
            model.GyroData.Add(GetGyroscopeResults() ?? new int[0]);
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
            for (int i = 0; i < numReadings; i++)
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
            bool valid = false;
            short regValue = 0;
            try
            {
                int attempt = 0;
                for ( attempt = 0; attempt < 200; attempt++)
                {
                    //The ADIS values seem to fluctuate at times, so try a few times to get the right value. 
                    //If it still cannot be validated, something may be wrong
                    regValue = (short)_gyroscopeDevice.RegisterRead((byte)Register.PROD_ID);
                    valid = regValue.Equals(gyroProductID);

                    if (valid)
                        break;
                }

                if (valid)
                {
                    _logger.LogInformation($"ADIS16460 Prod ID Register reads {regValue} after {attempt} attempts.");
                }
                else
                {
                    _logger.LogInformation($"ADIS16460 Prod ID Register could not be validated.");
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError("Error validating Gyro");
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
            }
            return valid;
        }

        public void SetGyroRegister(byte register, short value)
        {
            _gyroscopeDevice.RegisterWrite(register, value);
        }

        public int GetGyroRegister(byte register)
        {
            return _gyroscopeDevice.RegisterRead(register);
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
