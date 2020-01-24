using System;
using System.Device.Spi;
using System.Linq;
using System.Runtime.InteropServices;

namespace GrpcSpaceServer.Device
{
    public class Gyroscope
    {
        private SpiConnectionSettings _settings;

        /// <summary>
        /// ADIS16460 Gyroscope
        /// </summary>
        public Gyroscope()
        {
            _settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 1000000 };
        }

        /// <summary>
        /// ADIS16460 Gyroscope
        /// </summary>
        /// <param name="settings">Define customized settings or set null to allow default</param>
        public Gyroscope(SpiConnectionSettings settings)
        {
            if (settings == null)
            {
                settings = new SpiConnectionSettings(0, 0) { Mode = SpiMode.Mode3, ClockFrequency = 1000000 };
            }

            _settings = settings;
        }

        /// <summary>
        /// returns an array of burst data
        /// </summary>
        /// <returns></returns>
        public Span<int> BurstRead()
        {
            Span<byte> burstdata = new byte[22]; //+2 bytes for the address selection           

            Span<byte> burstTrigger = new byte[] { 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

            using (SpiDevice Gyro = SpiDevice.Create(_settings))
            {
                Gyro.TransferFullDuplex(burstTrigger, burstdata); 
            }
 
            //Convert the byte array to an int array -- Efficient, but will require using the exact opposite to retrieve correct values
            return MemoryMarshal.Cast<byte, int>(burstdata.Slice(2)); //remove the leading empty bytes
        }

        /// <summary>
        /// Gets non-scaled results for efficient gRPC transmissions.
        /// </summary>
        /// <returns></returns>
        public BrctcSpace.BurstResults GetBurstResults()
        {
            byte[] burstdata = new byte[22]; //+2 bytes for the address selection
            short[] burstwords = new short[10];

            byte[] burstTrigger = { 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

            BrctcSpace.BurstResults results = new BrctcSpace.BurstResults();

            using (SpiDevice Gyro = SpiDevice.Create(_settings))
            {

                Gyro.TransferFullDuplex(burstTrigger, burstdata);

                results.Diagnostic = BitConverter.ToInt16(new byte[] { burstdata[3], burstdata[2] }, 0);
                results.GyroX = BitConverter.ToInt16(new byte[] { burstdata[5], burstdata[4] }, 0);
                results.GyroY = BitConverter.ToInt16(new byte[] { burstdata[7], burstdata[6] }, 0);
                results.GyroZ = BitConverter.ToInt16(new byte[] { burstdata[9], burstdata[8] }, 0);
                results.AccelX = BitConverter.ToInt16(new byte[] { burstdata[11], burstdata[10] }, 0);
                results.AccelY = BitConverter.ToInt16(new byte[] { burstdata[13], burstdata[12] }, 0);
                results.AccelZ = BitConverter.ToInt16(new byte[] { burstdata[15], burstdata[14] }, 0);
                results.Temperature = BitConverter.ToInt16(new byte[] { burstdata[17], burstdata[16] }, 0);
                results.SampleCount = BitConverter.ToInt16(new byte[] { burstdata[19], burstdata[18] }, 0);
                results.Checksum = BitConverter.ToInt16(new byte[] { burstdata[21], burstdata[20] }, 0);
            }

            return results;
        }
    }
}
