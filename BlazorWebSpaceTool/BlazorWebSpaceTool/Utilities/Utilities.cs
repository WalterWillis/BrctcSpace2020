using BrctcSpace;
using BrctcSpaceLibrary;
using BrctcSpaceLibrary.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BlazorWebSpaceTool.Utilities
{
    public static class Utilities
    {
        public static Vibe2020DataModel ConvertToDataModel(ResultSet set)
        {
            Vibe2020DataModel model = new Vibe2020DataModel();

            if (set?.AccelerometerResults != null) {
                model.AccelData = new int[] { set.AccelerometerResults.X, set.AccelerometerResults.Y, set.AccelerometerResults.Z };
            }

            if(set?.GyroscopeResults?.BurstResults != null)
            {
                model.GyroData = new double[10]
                {
                    set.GyroscopeResults.BurstResults.Diagnostic,
                    set.GyroscopeResults.BurstResults.GyroX,
                    set.GyroscopeResults.BurstResults.GyroY,
                    set.GyroscopeResults.BurstResults.GyroZ,
                    set.GyroscopeResults.BurstResults.AccelX,
                    set.GyroscopeResults.BurstResults.AccelY,
                    set.GyroscopeResults.BurstResults.AccelZ,
                    set.GyroscopeResults.BurstResults.Temperature,
                    set.GyroscopeResults.BurstResults.SampleCount,
                    set.GyroscopeResults.BurstResults.Checksum
                };
            }

            model.TransactionTime = set.CurrentTime.ToDateTime().ToLocalTime();

            model.CpuTemp = set.CpuTemperature;

            return model;
        }

        public static List<Vibe2020DataModel> ConvertToDataModel(List<ResultSet> resultSet)
        {
            List<Vibe2020DataModel> dataModels = new List<Vibe2020DataModel>();
            foreach (var set in resultSet)
                dataModels.Add(ConvertToDataModel(set));

            return dataModels;
        }

        public static Vibe2020DataModel ConvertToDataModel(DeviceDataModel deviceData)
        {
            Vibe2020DataModel model = new Vibe2020DataModel();

            if (deviceData?.AccelData != null && deviceData.AccelData.Count > 0)
            {
                model.AccelData = new int[] { deviceData.AccelData[0], deviceData.AccelData[1], deviceData.AccelData[2] };
            }

            if (deviceData?.GyroData != null && deviceData.GyroData.Count > 0)
            {               
                Span<int> data = new int[10]
                {
                    deviceData.GyroData[0],
                    deviceData.GyroData[1],
                    deviceData.GyroData[2],
                    deviceData.GyroData[3],
                    deviceData.GyroData[4],
                    deviceData.GyroData[5],
                    deviceData.GyroData[6],
                    deviceData.GyroData[7],
                    deviceData.GyroData[8],
                    deviceData.GyroData[9]
                };

                Span<byte> bytes = MemoryMarshal.Cast<int, byte>(data);
                model.GyroData_Raw = GyroConversionHelper.CombineBytes(bytes).ToArray();

                model.GyroData = GyroConversionHelper.GetGyroscopeDetails(model.GyroData_Raw).ToArray();
            }

            model.TransactionTime = new DateTime(deviceData.TransactionTime).ToLocalTime();

            model.CpuTemp = deviceData.CpuTemp;

            return model;
        }

        public static List<Vibe2020DataModel> ConvertToDataModel(List<DeviceDataModel> resultSet)
        {
            List<Vibe2020DataModel> dataModels = new List<Vibe2020DataModel>();
            foreach (var set in resultSet)
                dataModels.Add(ConvertToDataModel(set));

            return dataModels;
        }
    }
}
