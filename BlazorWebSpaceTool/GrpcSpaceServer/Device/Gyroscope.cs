using System;
using System.Device.Spi;
using System.Linq;

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
        public short[] BurstRead()
        {
            byte[] burstdata = new byte[22]; //+2 bytes for the address selection
            short[] burstwords = new short[10];

            byte[] burstTrigger = { 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

            using (SpiDevice Gyro = SpiDevice.Create(_settings))
            {

                Gyro.TransferFullDuplex(burstTrigger, burstdata);

                byte[] data = (byte[])burstdata.Skip(2).ToArray(); //the first two bytes contain no valid data
                int counter = 0;

                for (int i = 0; i < data.Length; i += 2)
                {
                    byte[] bytes = data.Skip(i).Take(2).Reverse().ToArray();
                    burstwords[counter++] = BitConverter.ToInt16(bytes, 0);
                }
                #region Array Details
                /*
                burstwords[0]; //DIAG_STAT
                burstwords[1];//XGYRO
                burstwords[2]; //YGYRO
                burstwords[3]; //ZGYRO
                burstwords[4]; //XACCEL
                burstwords[5]; //YACCEL
                burstwords[6]; //ZACCEL
                burstwords[7]; //TEMP_OUT
                burstwords[8]; //SMPL_CNTR
                burstwords[9]; //CHECKSUM
                */
                #endregion
            }

            return burstwords;
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
