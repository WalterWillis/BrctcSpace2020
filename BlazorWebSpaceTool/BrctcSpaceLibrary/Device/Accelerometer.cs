using System;
using System.Collections.Generic;
using System.Device.Spi;
using Iot.Device.Adc;

namespace BrctcSpaceLibrary.Device
{
    /// <summary>
    /// Methods for communicating with our accelerometers. Uses three accelerometers through an MCP3208 ADC Pi hat.
    /// </summary>
    public class Accelerometer : IDisposable
    {
        private bool _isdisposing = false;
        private SpiConnectionSettings _settings;
        private double _resolution = 4095 * 3.3;
        private Mcp3208 _adc;

        /// <summary>
        /// Accelerometer values via SPI 
        /// </summary>
        public Accelerometer()
        {
            _settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode1, ClockFrequency = 1000000 };

            using (SpiDevice spi = SpiDevice.Create(_settings))
            {
                _adc = new Mcp3208(spi);
            }
        }

        /// <summary>
        /// Accelerometer values via SPI 
        /// </summary>
        /// <param name="settings">Define customized settings or set null to allow default</param>
        /// <param name="channel_x">Defaults to channel 0</param>
        /// <param name="channel_y">Defaults to channel 1</param>
        /// <param name="channel_z">Defaults to channel 2</param>
        /// <param name="voltRef">Defaults to 3.3 volts</param>
        public Accelerometer(SpiConnectionSettings settings, int channel_x = 0, int channel_y = 1, int channel_z = 2, double voltRef = 3.3)
        {
            if (settings == null)
            {
                settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode1, ClockFrequency = 1000000 };
            }

            _settings = settings;

            _resolution = 4095 * voltRef;

            using (SpiDevice spi = SpiDevice.Create(_settings))
            {
                _adc = new Mcp3208(spi);
            }
        }

        /// <summary>
        /// Get the raw combined byte value from the specified channel
        /// </summary>
        /// <param name="channel">Channel to read from</param>
        /// <returns></returns>
        public int GetRaw(Channel channel)
        {
            return _adc.Read((int)channel);
        }

        /// <summary>
        /// Gets all three raw axis data in the order of X, Y, Z
        /// </summary>
        /// <returns></returns>
        public int[] GetRaws()
        {
            return new int[] {
                _adc.Read((int)Channel.X),
                _adc.Read((int)Channel.Y),
                _adc.Read((int)Channel.Z)
            };
        }

        public void GetRaws(Span<byte> buffer)
        {
            byte[] bytes = BitConverter.GetBytes(_adc.Read((int)Channel.X));
            buffer[0] = bytes[0];
            buffer[1] = bytes[1];
            buffer[2] = bytes[2];
            buffer[3] = bytes[3];

            bytes = BitConverter.GetBytes(_adc.Read((int)Channel.Y));
            buffer[4] = bytes[0];
            buffer[5] = bytes[1];
            buffer[6] = bytes[2];
            buffer[7] = bytes[3];

            bytes = BitConverter.GetBytes(_adc.Read((int)Channel.Z));
            buffer[8] = bytes[0];
            buffer[9] = bytes[1];
            buffer[10] = bytes[2];
            buffer[11] = bytes[3];
        }

        /// <summary>
        /// Get the voltage representation from the specified channel
        /// </summary>
        /// <param name="channel">Channel to read from</param>
        /// <returns></returns>
        public double GetScaledValue(Channel channel)
        {
            return _adc.Read((int)channel) / _resolution;
        }


        /// <summary>
        /// Gets voltage representation of all three axis data in the order of X, Y, Z
        /// </summary>
        /// <returns></returns>
        public Span<double> GetScaledValues()
        {

            return new double[] {
                _adc.Read((int)Channel.X) / _resolution,
                _adc.Read((int)Channel.Y) / _resolution,
                _adc.Read((int)Channel.Z) / _resolution
            };

        }

        //We use pins 0, 2 and 4 as X, Y, and Z respectively
        public enum Channel : int { X = 0, Y = 2, Z = 4 }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isdisposing)
                return;

            if (disposing)
            {
                _adc.Dispose();
            }

            _isdisposing = true;
        }
    }
}
