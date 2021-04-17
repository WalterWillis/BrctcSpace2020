using BrctcSpaceLibrary;
using BrctcSpaceLibrary.DataModels;
using System;
using System.IO;

namespace DataConversions
{
    class Program
    {
        static FileInfo LogFile = new FileInfo(Path.Combine("Converted", "Log.txt"));
        static void Main(string[] args)
        {
            ConvertAccelData();
            ConvertGyroData();
        }

        public static void ConvertAccelData()
        {
            string filename = Path.Combine("Data", "Accelerometer.binary");
            string fileDir = Path.Combine("Converted", "Accelerometer");

            Directory.CreateDirectory("Converted");

            DateTime initialTime = DateTime.Now;
            DateTime currentTime = DateTime.Now;
            bool isInitialDateTime = true;

            int fileCounter = 0; //current files found and navigated
            int newFileCounter = 0; //current number of files converted based on fileLineSize

            long index = 1;
            long fileLineIndex = 0; //tracks amount of lines in current file
            long count = 0; //keep track of records

            const int fileLineSize = 1000000;

            string searchFile = $"{filename}{fileCounter++}";
            string resultFile = $"{fileDir}{newFileCounter++}.csv";

            StreamWriter sw = File.CreateText(resultFile);

            //get all accel binary files
            while (File.Exists(searchFile))
            {

                using (FileStream fs = new FileStream(searchFile, FileMode.Open, FileAccess.Read))
                {
                    int accelBytes = 12;
                    int rtcBytes = 8;
                    int cpuBytes = 8;
                    int accelSegmentLength = accelBytes + rtcBytes + cpuBytes;

                    byte[] bytes = new byte[accelBytes + rtcBytes + cpuBytes];

                    const string header = "ID,SECOND,X_RAW,Y_RAW,Z_RAW,X,Y,X,TIMESTAMP,CPU_TEMP";

                    sw.WriteLine(header);

                    while (fs.Read(bytes) != 0)
                    {
                        Span<byte> data = bytes;
                        Span<byte> accelSegment = data.Slice(0, accelBytes);
                        Span<byte> rtcSegment = data.Slice(accelBytes, rtcBytes);
                        Span<byte> cpuSegment = data.Slice(accelBytes + rtcBytes, cpuBytes);

                        currentTime = new DateTime(BitConverter.ToInt64(rtcSegment));

                        if (isInitialDateTime)
                        {
                            initialTime = currentTime;
                            isInitialDateTime = false;
                        }


                        string csvLine = $"{index++},";

                        csvLine += (currentTime - initialTime).TotalSeconds.ToString("F3") + ",";



                        int[] acelValues = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>(accelSegment).ToArray();
                        AccelerometerModel model = new AccelerometerModel(acelValues);

                        csvLine += $"{model.X_Raw},{model.Y_Raw},{model.Z_Raw},{model.X},{model.Y},{model.Z},";

                        csvLine += currentTime.ToString("HH:mm:ss") + ",";
                        csvLine += BitConverter.ToDouble(cpuSegment).ToString();

                        if (fileLineIndex >= fileLineSize)
                        {
                            //generate new csv file
                            fileLineIndex = 0;
                            resultFile = $"{fileDir}{newFileCounter++}.csv";

                            sw.Flush();
                            sw.Close();
                            sw.Dispose();
                            sw = File.CreateText(resultFile);
                        }

                        sw.WriteLine(csvLine);
                        count++;
                        fileLineIndex++;

                    }

                    
                }
                sw.Flush();
                searchFile = $"{filename}{fileCounter++}";
            }
            sw.Dispose();

            using (var writer = LogFile.AppendText())
            {
                writer.WriteLine("-------------------------------------------------------------------------------");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Records converted: {count}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Files found: {fileCounter}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Converted Files created: {newFileCounter}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer TotalSeconds: {(currentTime - initialTime).TotalSeconds.ToString("F3")}");
                writer.WriteLine();
            }
        }

        public static void ConvertGyroData()
        {
            string filename = Path.Combine("Data", "Gyroscope.binary");
            string newFile = Path.Combine("Converted", "Gyroscope");

            Directory.CreateDirectory("Converted");

            DateTime initialTime = DateTime.Now;
            DateTime currentTime = DateTime.Now;
            bool isInitialDateTime = true;

            int newFileCounter = 0; //current number of files converted based on fileLineSize

            long index = 1;
            long fileLineIndex = 0; //tracks amount of lines in current file
            const int fileLineSize = 1000000;
            long count = 0;

            string resultFile = $"{newFile}{newFileCounter++}.csv";
            StreamWriter sw = File.CreateText(resultFile);

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                const int gyroBytes = 20;
                const int rtcBytes = 8;
                const int cpuBytes = 8;

                byte[] bytes = new byte[gyroBytes + rtcBytes + cpuBytes];

                const string header = "ID,Second,DIAG_STAT_RAW,GYRO_X_Raw,GYRO_Y_RAW,GYRO_Z_RAW,ACCEL_X_RAW,ACCEL_Y_RAW,ACCEL_Z_RAW,TEMP_RAW,SPS_RAW,CHECKSUM_RAW," +
             "DIAG_STAT,GYRO_X,GYRO_Y,GYRO_Z,ACCEL_X,ACCEL_Y,ACCEL_Z,TEMP,SPS,CHECKSUM," +
             "TIMESTAMP,CPU_TEMP";

                sw.WriteLine(header);

                while (fs.Read(bytes) != 0)
                {
                    Span<byte> data = bytes;
                    Span<byte> gyroSegment = data.Slice(0, gyroBytes);
                    Span<byte> rtcSegment = data.Slice(gyroBytes, rtcBytes);
                    Span<byte> cpuSegment = data.Slice(gyroBytes + rtcBytes, cpuBytes);

                    currentTime = new DateTime(BitConverter.ToInt64(rtcSegment));

                    if (isInitialDateTime)
                    {
                        initialTime = currentTime;
                        isInitialDateTime = false;
                    }


                    string csvLine = $"{index++},";

                    csvLine += (currentTime - initialTime).TotalSeconds.ToString("F3") + ",";
                    count++;


                    Span<short> burstData = GyroConversionHelper.CombineBytes(gyroSegment);
                    csvLine += string.Join(',', burstData.ToArray()) + ",";

                    Span<double> gyroData = GyroConversionHelper.GetGyroscopeDetails(burstData);

                    csvLine += string.Join(',', gyroData.ToArray()) + ",";
                    csvLine += currentTime.ToString("HH:mm:ss") + ",";
                    csvLine += BitConverter.ToDouble(cpuSegment).ToString();


                    if (fileLineIndex >= fileLineSize)
                    {
                        //generate new csv file
                        fileLineIndex = 0;
                        resultFile = $"{newFile}{newFileCounter++}.csv";

                        sw.Flush();
                        sw.Close();
                        sw.Dispose();
                        sw = File.CreateText(resultFile);
                    }

                    sw.WriteLine(csvLine);

                }

            }

            using (var writer = LogFile.AppendText())
            {                
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Gyroscope Records converted: {count}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Gyroscope Converted Files created: {newFileCounter}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Gyroscope TotalSeconds: {(currentTime - initialTime).TotalSeconds.ToString("F3")}");
                writer.WriteLine("-------------------------------------------------------------------------------");
            }
        }
    }
}
