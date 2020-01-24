using BrctcSpace;
using System.Collections.Generic;

namespace GrpcSpaceServer.Services.Interfaces
{
    public interface IVibe2020DataService
    { 
        /// <summary>
        /// Starts the asynchronous gathering of data
        /// </summary>
        public void Initialize(bool useAccel = true, bool useGyro = true, bool useRtc = true, bool useCpu = true);

        /// <summary>
        /// Retrieves the currently buffered data, clearing the buffer in the process
        /// </summary>
        /// <returns></returns>
        public List<DeviceDataModel> GetData();
    }
}
