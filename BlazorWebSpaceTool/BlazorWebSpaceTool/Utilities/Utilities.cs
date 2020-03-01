using BrctcSpace;
using BrctcSpaceLibrary;
using BrctcSpaceLibrary.DataModels;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace BlazorWebSpaceTool.Utilities
{
    public static class Utilities
    {
        public static Vibe2020DataModel ConvertToDataModel(DeviceDataModel deviceData)
        {
            Vibe2020DataModel model = new Vibe2020DataModel();

            if (deviceData?.AccelData != null && deviceData.AccelData.Count > 0)
            {
                model.AccelData_Raw = new int[] { deviceData.AccelData[0], deviceData.AccelData[1], deviceData.AccelData[2] };

                model.AccelData = new double[] {
                    ScaleAccelerometer(deviceData.AccelData[0]), 
                    ScaleAccelerometer(deviceData.AccelData[1]),
                    ScaleAccelerometer(deviceData.AccelData[2])
                };
            }

            if (deviceData?.GyroData != null && deviceData.GyroData.Count > 0)
            {
                Span<int> data = new int[4]
                {
                    deviceData.GyroData[0],
                    deviceData.GyroData[1],
                    deviceData.GyroData[2],
                    deviceData.GyroData[3]
                };

                Span<byte> bytes = MemoryMarshal.Cast<int, byte>(data);
                model.GyroData_Raw = GyroConversionHelper.CombineBytes(bytes).ToArray();

                model.GyroData = GyroConversionHelper.GetGyroscopeDetails(model.GyroData_Raw).ToArray();
            }

            model.ResultStatus = (ResultStatus)deviceData.ResultStatus;

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

        /// <summary>
        /// Save the file using javascript
        /// </summary>
        /// <param name="js"></param>
        /// <param name="filename"></param>
        /// <param name="data"></param>
        /// <remarks>Thanks to a tutorial located Here: https://www.syncfusion.com/kb/10358/how-to-create-a-pdf-file-in-blazor-using-c</remarks>
        /// <returns></returns>
        public static ValueTask<object> SaveAs(this IJSRuntime js, string filename, byte[] data)
            => js.InvokeAsync<object>(
                "saveAsFile",
                filename,
                Convert.ToBase64String(data));

        /// <summary>
        /// Using javascript, save the data and download to the client. 
        /// </summary>
        /// <param name="data">Data to save to json</param>
        /// <param name="JS">Page calling this requires passing an injected JSRuntime</param>
        /// <param name="identifier">Unique part of name used to identify the file</param>
        public async static void SaveToJson<T>(T data, IJSRuntime JS, string identifier = "")
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            //string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            //Directory.CreateDirectory(tempPath);

            string fileName = $"{identifier}-{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.json";

            options.Converters.Add(new HandleSpecialDoublesAsStrings());

            using (MemoryStream stream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(stream, data, options);

                await JS.SaveAs(fileName, stream.ToArray());
            }
        }

        /// <summary>
        /// Saves a list of models to csv.
        /// </summary>
        /// <param name="data">Data to save to csv</param>
        /// <param name="JS">Page calling this requires passing an injected JSRuntime</param>
        /// <param name="identifier">Unique part of name used to identify the file</param>
        public async static void SaveToCsv(List<Vibe2020DataModel> data, IJSRuntime JS, string identifier = "") // should create a DataModel base class if more data models are created and then generalize this
        {
            string fileName = $"{identifier}-{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.csv";

            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    await writer.WriteLineAsync(Vibe2020DataModel.GetHeader());

                    foreach (var item in data)
                    {
                        await writer.WriteLineAsync(item.ToCsvLine());
                    }
                }
                await JS.SaveAs(fileName, stream.ToArray());
            }
        }

        private static double ScaleAccelerometer(int value)
        {
            double resRatio = 5D / 4095;
            return value * resRatio;
        }
    }


    ///for handling NaN and Infinity doubles - should be moved elsewhere
    public class HandleSpecialDoublesAsStrings : System.Text.Json.Serialization.JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return double.Parse(reader.GetString());
            }
            return reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsFinite(value))
            {
                writer.WriteNumberValue(value);
            }
            else
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
