using BrctcSpaceLibrary;
using BrctcSpaceLibrary.DataModels;
using BrctcSpaceLibrary.Processes;
using BrctcSpaceLibrary.Systems;
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
            ConvertAccelDataSeperateRowsDuplicateIDs();
            ConvertAccelDataNoAnalysis();
            ConvertGyroData();
        }

        public static void ConvertAccelData()
        {
            string filename = Path.Combine("Data", "Accelerometer.binary");
            string fileDir = Path.Combine("Converted", "AccelerometerFreqAnalysis");

            Directory.CreateDirectory("Converted");

            DateTime initialTime = DateTime.Now;
            DateTime currentTime = DateTime.Now;
            bool isInitialDateTime = true;
            bool writeHeader = true;

            int fileCounter = 0; //current files found and navigated
            int newFileCounter = 0; //current number of files converted based on fileLineSize
            int prevSecond = 0;
            int sampleIndex = 0; // initialize to 0 but will increment to 1 right away. Compare using <=, rathter than <
            int sps = 7999;
            int fileSent = 1;

            long index = 1;
            long fileLineIndex = 0; //tracks amount of lines in current file
            long count = 0; //keep track of records
            long indexTracker = 0; //tracks the current index of each line over multiple files


            const int fileLineSize = 1000000;

            string searchFile = $"{filename}{fileCounter++}";
            string resultFile = $"{fileDir}{newFileCounter++}.csv";

            StreamWriter sw = File.CreateText(resultFile);
            AccelerometerDataAnalysis processor = new AccelerometerDataAnalysis();
            TemperatureModel temperature = new TemperatureModel();

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

                    if (writeHeader)
                    {
                        string header = $"ID,Timestamp,Second,Temp (F),SPS{processor.GenerateCsvHeaders()}";

                        sw.WriteLine(header);
                        writeHeader = false;
                    }

                    while (fs.Read(bytes) != 0)
                    {
                        Span<byte> data = bytes;
                        Span<byte> accelSegment = data.Slice(0, accelBytes);
                        Span<byte> rtcSegment = data.Slice(accelBytes, rtcBytes);
                        Span<byte> cpuSegment = data.Slice(accelBytes + rtcBytes, cpuBytes);

                        currentTime = new DateTime(BitConverter.ToInt64(rtcSegment));

                        if (isInitialDateTime)
                        {
                            prevSecond = currentTime.Second;
                            initialTime = currentTime;
                            isInitialDateTime = false;
                        }

                        //as long as the seconds match, get the data
                        if (prevSecond == currentTime.Second && sampleIndex <= sps)
                        {
                            //add data for each second
                            temperature.GetNextAverage(BitConverter.ToDouble(cpuSegment));
                            processor.ProcessData(accelSegment);
                        }
                        else
                        {
                            //perform analysis and send message on second change
                            processor.PerformFFTAnalysis();

                            //iterate second and append all data. Processor data should already have commas
                            string csvLine = $"{indexTracker++},{currentTime.ToString("HH:mm:ss")},{(currentTime - initialTime).TotalSeconds.ToString("F3")}," +
                                $"{(int)temperature.AverageCPUTemp},{processor.SampleSize}{processor.X_Magnitudes}{processor.Y_Magnitudes}{processor.Z_Magnitudes}";

                            try
                            {
                                if (fileLineIndex >= fileLineSize)
                                {
                                    //generate new csv file
                                    fileLineIndex = 0;
                                    resultFile = $"{fileDir}{newFileCounter++}.csv";

                                    sw.Flush();
                                    sw.Close();
                                    sw.Dispose();
                                    sw = File.CreateText(resultFile);
                                    writeHeader = true;
                                }

                                sw.WriteLine(csvLine);
                                count++;
                                fileLineIndex++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to write!\n{ex.Message}\n{ex.StackTrace}");
                            }

                            //reset processor and temperature averge and begin new data set here
                            processor.Reset();
                            temperature.Reset();
                            temperature.GetNextAverage(BitConverter.ToDouble(cpuSegment));
                            processor.ProcessData(accelSegment);
                            sampleIndex = 1; //start at one since we already have added our next first datapoint
                        }
                        prevSecond = currentTime.Second;

                        data.Clear(); // since we are reusing this array, clear the values for integrity    
                    }
                    Console.WriteLine($"Finished writing file #{fileSent++} at {indexTracker} total lines transmitted!");


                }
                sw.Flush();
                searchFile = $"{filename}{fileCounter++}";
            }
            sw.Dispose();

            using (var writer = LogFile.AppendText())
            {
                writer.WriteLine("-------------------------------------------------------------------------------");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Frequency Analysis Records converted: {count}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Frequency Analysis Files found: {fileCounter}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Frequency Analysis Converted Files created: {newFileCounter}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Frequency Analysis TotalSeconds: {(currentTime - initialTime).TotalSeconds.ToString("F3")}");
                writer.WriteLine();
            }
        }

        public static void ConvertAccelDataSeperateRowsDuplicateIDs()
        {
            string filename = Path.Combine("Data", "Accelerometer.binary");
            string fileDir = Path.Combine("Converted", "AccelerometerFreqAnalysisDuplicateIDs");

            Directory.CreateDirectory("Converted");

            DateTime initialTime = DateTime.Now;
            DateTime currentTime = DateTime.Now;
            bool isInitialDateTime = true;
            bool writeHeader = true;

            int fileCounter = 0; //current files found and navigated
            int newFileCounter = 0; //current number of files converted based on fileLineSize
            int prevSecond = 0;
            int sampleIndex = 0; // initialize to 0 but will increment to 1 right away. Compare using <=, rathter than <
            int sps = 7999;
            int fileSent = 1;

            long index = 1;
            long fileLineIndex = 0; //tracks amount of lines in current file
            long count = 0; //keep track of records
            long indexTracker = 0; //tracks the current index of each line over multiple files


            const int fileLineSize = 1000000;

            string searchFile = $"{filename}{fileCounter++}";
            string resultFile = $"{fileDir}{newFileCounter++}.csv";

            StreamWriter sw = File.CreateText(resultFile);
            AccelerometerDataAnalysis processor = new AccelerometerDataAnalysis();
            TemperatureModel temperature = new TemperatureModel();

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

                    if (writeHeader)
                    {
                        string header = $"ID,Timestamp,Second,Temp (F),SPS,X_Frequency,X_Magnitude,Y_Frequency,Y_Magnitude,Z_Frequency,Z_Magnitude";

                        sw.WriteLine(header);
                        writeHeader = false;
                    }

                    while (fs.Read(bytes) != 0)
                    {
                        Span<byte> data = bytes;
                        Span<byte> accelSegment = data.Slice(0, accelBytes);
                        Span<byte> rtcSegment = data.Slice(accelBytes, rtcBytes);
                        Span<byte> cpuSegment = data.Slice(accelBytes + rtcBytes, cpuBytes);

                        currentTime = new DateTime(BitConverter.ToInt64(rtcSegment));

                        if (isInitialDateTime)
                        {
                            prevSecond = currentTime.Second;
                            initialTime = currentTime;
                            isInitialDateTime = false;
                        }

                        //as long as the seconds match, get the data
                        if (prevSecond == currentTime.Second && sampleIndex <= sps)
                        {
                            //add data for each second
                            temperature.GetNextAverage(BitConverter.ToDouble(cpuSegment));
                            processor.ProcessData(accelSegment);
                        }
                        else
                        {
                            //perform analysis and send message on second change
                            processor.PerformFFTAnalysis();

                            //iterate second and append all data. Processor data should already have commas
                            var xPairs = processor.X_Magnitudes.Split(',');
                            var yPairs = processor.Y_Magnitudes.Split(',');
                            var zPairs = processor.Z_Magnitudes.Split(',');
                            try
                            {
                                for (int i = 0; i < processor.MagnitudeCount * 2; i += 2) //magnitude count times two since the array will have each result's frequency and magnitude
                                {
                                    if (string.IsNullOrEmpty(xPairs[i]))
                                    {
                                        xPairs[i] = "0";
                                    }

                                    if (string.IsNullOrEmpty(yPairs[i]))
                                    {
                                        yPairs[i] = "0";
                                    }

                                    if (string.IsNullOrEmpty(zPairs[i]))
                                    {
                                        zPairs[i] = "0";
                                    }

                                    string csvLine = $"{indexTracker++},{currentTime.ToString("HH:mm:ss")},{(currentTime - initialTime).TotalSeconds.ToString("F3")}," +
                                        $"{(int)temperature.AverageCPUTemp},{processor.SampleSize},{xPairs[i]},{xPairs[i + 1]},{yPairs[i]},{yPairs[i + 1]},{zPairs[i]},{zPairs[i + 1]}";

                                    sw.WriteLine(csvLine);
                                    count++;
                                    fileLineIndex++;

                                }

                                if (fileLineIndex >= fileLineSize)
                                {
                                    //generate new csv file
                                    fileLineIndex = 0;
                                    resultFile = $"{fileDir}{newFileCounter++}.csv";

                                    sw.Flush();
                                    sw.Close();
                                    sw.Dispose();
                                    sw = File.CreateText(resultFile);
                                    writeHeader = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(xPairs.Length);
                                Console.WriteLine($"Failed to write!\n{ex.Message}\n{ex.StackTrace}");
                            }


                            //reset processor and temperature averge and begin new data set here
                            processor.Reset();
                            temperature.Reset();
                            temperature.GetNextAverage(BitConverter.ToDouble(cpuSegment));
                            processor.ProcessData(accelSegment);
                            sampleIndex = 1; //start at one since we already have added our next first datapoint
                        }
                        prevSecond = currentTime.Second;

                        data.Clear(); // since we are reusing this array, clear the values for integrity    
                    }
                    Console.WriteLine($"Finished writing file #{fileSent++} at {indexTracker} total lines transmitted!");


                }
                sw.Flush();
                searchFile = $"{filename}{fileCounter++}";
            }
            sw.Dispose();

            using (var writer = LogFile.AppendText())
            {
                writer.WriteLine("-------------------------------------------------------------------------------");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Frequency Analysis Duplicate ID Records converted: {count}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Frequency Analysis Duplicate ID Files found: {fileCounter}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Frequency Analysis Duplicate ID Converted Files created: {newFileCounter}");
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")} - Accelerometer Frequency Analysis Duplicate ID TotalSeconds: {(currentTime - initialTime).TotalSeconds.ToString("F3")}");
                writer.WriteLine();
            }
        }

        public static void ConvertAccelDataNoAnalysis()
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
