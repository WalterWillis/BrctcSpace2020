﻿using System;
using System.Collections.Generic;
using System.Device.Spi;
using BrctcSpaceLibrary.Helpers;
using Iot.Device.Adc;

//Methods for communicating with our accelerometers. Uses three accelerometers (ADXL-1002Z) through an MCP3208 ADC Pi hat.
namespace BrctcSpaceLibrary.Device
{
    /// <summary>
    ///  MCP3208 - 12-bit A/D Converter with SPI Serial Interface 
    /// </summary>
    public class Accelerometer : IMcp3208, IDisposable
    {
        private bool _isdisposing = false;
        private SpiConnectionSettings _settings;
        private double _resRatio = 5 / 4095; // verified correct - Use: value * _resRatio
        private Mcp3208 _adc;
        private IntUnion _union = new IntUnion();

       /// <summary>
       ///  MCP3208 - 12-bit A/D Converter with SPI Serial Interface
       /// </summary>
        public Accelerometer()
        {
            _settings = new SpiConnectionSettings(1, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1000000 };

            using (SpiDevice spi = SpiDevice.Create(_settings))
            {
                _adc = new Mcp3208(spi);
            }
        }

        /// <summary>
        /// MCP3208 - 12-bit A/D Converter with SPI Serial Interface
        /// </summary>
        /// <param name="settings">Define customized settings or set null to allow default</param>
        /// <param name="voltRef">Defaults to 5 volts</param>

        public Accelerometer(SpiConnectionSettings settings, double voltRef = 5)
        {
            if (settings == null)
            {
                settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode0, ClockFrequency = 1000000 };
            }

            _settings = settings;

            _resRatio = voltRef / 4095;

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

        /// <summary>
        /// Fills a 12 bit buffer with 3 int values
        /// </summary>
        /// <param name="buffer">Span reference that gets filled with byte data</param>
        public void Read(Span<byte> buffer)
        {
            _union.integer = _adc.Read((int)Channel.X);
            buffer[0] = _union.byte0;
            buffer[1] = _union.byte1;
            buffer[2] = _union.byte2;
            buffer[3] = _union.byte3;

            _union.integer = _adc.Read((int)Channel.Y);
            buffer[4] = _union.byte0;
            buffer[5] = _union.byte1;
            buffer[6] = _union.byte2;
            buffer[7] = _union.byte3;

            _union.integer = _adc.Read((int)Channel.Z);
            buffer[8] = _union.byte0;
            buffer[9] = _union.byte1;
            buffer[10] = _union.byte2;
            buffer[11] = _union.byte3;
        }

        /// <summary>
        /// Get the voltage representation from the specified channel
        /// </summary>
        /// <param name="channel">Channel to read from</param>
        /// <returns></returns>
        public double GetScaledValue(Channel channel)
        {
            return _adc.Read((int)channel) / _resRatio;
        }


        /// <summary>
        /// Gets voltage representation of all three axis data in the order of X, Y, Z
        /// </summary>
        /// <returns></returns>
        public Span<double> GetScaledValues()
        {

            return new double[] {
                _adc.Read((int)Channel.X) * _resRatio,
                _adc.Read((int)Channel.Y) * _resRatio,
                _adc.Read((int)Channel.Z) * _resRatio
            };

        }

        /// <summary>
        /// fills a 24 bit buffer with 3 double values
        /// </summary>
        /// <param name="buffer"></param>
        public void GetScaledValues(Span<byte> buffer)
        {
            byte[] bytes = BitConverter.GetBytes((double)(_adc.Read((int)Channel.X) * _resRatio));
            buffer[0] = bytes[0];
            buffer[1] = bytes[1];
            buffer[2] = bytes[2];
            buffer[3] = bytes[3];
            buffer[4] = bytes[4];
            buffer[5] = bytes[5];
            buffer[6] = bytes[6];
            buffer[7] = bytes[7];

            bytes = BitConverter.GetBytes((double)(_adc.Read((int)Channel.Y) * _resRatio));
            buffer[8] = bytes[0];
            buffer[9] = bytes[1];
            buffer[10] = bytes[2];
            buffer[11] = bytes[3];
            buffer[12] = bytes[4];
            buffer[13] = bytes[5];
            buffer[14] = bytes[6];
            buffer[15] = bytes[7];

            bytes = BitConverter.GetBytes((double)(_adc.Read((int)Channel.Z) * _resRatio));
            buffer[16] = bytes[0];
            buffer[17] = bytes[1];
            buffer[18] = bytes[2];
            buffer[19] = bytes[3];
            buffer[20] = bytes[4];
            buffer[21] = bytes[5];
            buffer[22] = bytes[6];
            buffer[23] = bytes[7];
        }

        //We use pins 0, 2 and 4 as X, Y, and Z respectively
        public enum Channel : int { X = 0, Y = 1, Z = 2 }


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
