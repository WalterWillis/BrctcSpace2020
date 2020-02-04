using System;
using System.Collections.Generic;
using System.Device.Spi;
using Iot.Device.Adc;

namespace GrpcSpaceServer.Device
{
    /// <summary>
    /// Methods for communicating with our accelerometers. Uses three accelerometers through an MCP3208 ADC Pi hat.
    /// </summary>
    public class Accelerometer
    {
        private SpiConnectionSettings _settings;
        //We use pins 0, 1 and 2 as X, Y, and Z respectively
        private Dictionary<Channel, int> _channels =
            new Dictionary<Channel, int> { { Channel.X, 0 }, { Channel.Y, 1 }, { Channel.Z, 2 } };
        private double _resolution = 4095 * 3.3;

        /// <summary>
        /// Accelerometer values via SPI 
        /// </summary>
        public Accelerometer()
        {
            _settings = new SpiConnectionSettings(0, 1) { Mode = SpiMode.Mode1, ClockFrequency = 1000000 };
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
                settings = new SpiConnectionSettings(0, 1) { Mode = SpiMode.Mode1, ClockFrequency = 1000000 };
            }

            _settings = settings;
            _channels = new Dictionary<Channel, int>
            { { Channel.X, channel_x }, { Channel.Y, channel_y }, { Channel.Z, channel_z } };

            _resolution = 4095 * voltRef;
        }

        /// <summary>
        /// Get the raw combined byte value from the specified channel
        /// </summary>
        /// <param name="channel">Channel to read from</param>
        /// <returns></returns>
        public int GetRaw(Channel channel)
        {
            using (SpiDevice spi = SpiDevice.Create(_settings))
            {
                using (Mcp3208 adc = new Mcp3208(spi))
                {
                    return adc.Read(_channels[channel]);
                }
            }
        }

        /// <summary>
        /// Gets all three raw axis data in the order of X, Y, Z
        /// </summary>
        /// <returns></returns>
        public Span<int> GetRaws()
        {
            using (SpiDevice spi = SpiDevice.Create(_settings))
            {
                using (Mcp3208 adc = new Mcp3208(spi))
                {
                    Span<int> values = new Span<int>();
                    values[0] = adc.Read(_channels[Channel.X]);
                    values[0] = adc.Read(_channels[Channel.Y]);
                    values[0] = adc.Read(_channels[Channel.Z]);
                    return values;
                }
            }
        }

        /// <summary>
        /// Get the voltage representation from the specified channel
        /// </summary>
        /// <param name="channel">Channel to read from</param>
        /// <returns></returns>
        public double GetScaledValue(Channel channel)
        {
            using (SpiDevice spi = SpiDevice.Create(_settings))
            {
                using (Mcp3208 adc = new Mcp3208(spi))
                {
                    return adc.Read(_channels[channel]) / _resolution;
                }
            }
        }


        /// <summary>
        /// Gets voltage representation of all three axis data in the order of X, Y, Z
        /// </summary>
        /// <returns></returns>
        public Span<double> GetScaledValues()
        {
            using (SpiDevice spi = SpiDevice.Create(_settings))
            {
                using (Mcp3208 adc = new Mcp3208(spi))
                {
                    return new double[] {
                        adc.Read(_channels[Channel.X]) / _resolution,
                        adc.Read(_channels[Channel.Y]) / _resolution,
                        adc.Read(_channels[Channel.Z]) / _resolution
                    };
                }
            }
        }

        /// <summary>
        /// Gets formatted results for efficient gRPC transmissions.
        /// </summary>
        /// <returns></returns>
        public BrctcSpace.AccelerometerResults GetAccelerometerResults()
        {
            BrctcSpace.AccelerometerResults results = new BrctcSpace.AccelerometerResults();

            using (SpiDevice spi = SpiDevice.Create(_settings))
            {
                using (Mcp3208 adc = new Mcp3208(spi))
                {
                    results.X = adc.Read(_channels[Channel.X]);
                    results.Y = adc.Read(_channels[Channel.Y]);
                    results.Z = adc.Read(_channels[Channel.Z]);
                }
            }

            return results;
        }


        public enum Channel { X, Y, Z }
    }
}
