using System;
using System.IO;

namespace DataAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            FileInfo fileInfo = new FileInfo(@"C:\Users\Walte\Desktop\Jan2021TestResults\Test 2\accel.csv");
            FileInfo newFile = new FileInfo("text.csv");
            if (newFile.Exists)
                newFile.Delete();
            bool foundStart = false;
            DateTime startTime = new DateTime();

            using (var writeStream = newFile.OpenWrite())
            {
                using (var writer = new StreamWriter(writeStream))
                {
                    using (var stream = fileInfo.OpenRead())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string header = reader.ReadLine();
                            writer.WriteLine(header);

                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                var content = line.Split(',');

                                long ticks = Convert.ToInt64(content[26]);

                                DateTime dataTime = new DateTime(ticks);

                                if (!foundStart) {
                                    startTime = dataTime;
                                    foundStart = true;
                                }
                                var difference = dataTime - startTime;
  
                                content[26] = difference.TotalSeconds.ToString();

                                writer.WriteLine(string.Join(",", content));
                            }
                        }
                    }
                }
            }
        }
    }
}
