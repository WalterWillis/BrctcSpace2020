using BrctcSpace;

namespace GrpcSpaceServer.Services.Interfaces
{
    public interface IVibe2020DataService
    {
        /// <summary>
        /// Return a single piece of data
        /// </summary>
        /// <returns></returns>
        public DeviceDataModel GetSingleReading();

        /// <summary>
        /// Return a set of data up to the amount specified
        /// </summary>
        /// <param name="numReadings"></param>
        /// <returns></returns>
        public DeviceDataModel[] GetReadings(int numReadings = 1000);

        /// <summary>
        /// Tests the gyroscope to see if it's returning expected data
        /// </summary>
        /// <returns>Returns true if valid</returns>
        public bool IsGyroValid();

        /// <summary>
        /// Writes a value to a particular register
        /// </summary>
        /// <param name="register"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetGyroRegister(byte register, short value);

        /// <summary>
        /// Gets the value from a particular register
        /// </summary>
        /// <param name="register"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetGyroRegister(byte register);
    }
}
