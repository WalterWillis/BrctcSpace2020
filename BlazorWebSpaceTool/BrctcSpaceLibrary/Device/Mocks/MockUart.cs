using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BrctcSpaceLibrary.Device.Mocks
{
    public class MockUart : IUART
    {

        /// <summary>
        /// File to write mock telemetry data to
        /// </summary>
        public static string FileName { get; set; } // needs to be static since UART is often disposed of
        public void Dispose()
        {
            //do nothing
        }

        public IUART GetUART()
        {
            return new MockUart();
        }

        public void SendBytes(Span<byte> buffer)
        {
            Console.WriteLine("Telemetry Sending bytes result: " + string.Join(' ', buffer.ToArray()));
        }

        public string SerialRead()
        {
            return "Mock Telemetry String";
        }

        public void SerialSend(string message)
        {
            FileInfo file = new FileInfo(FileName);

            using (var s = file.AppendText())
            {
                s.WriteLine(message);
                s.Flush();
            }
                Console.WriteLine($"Telemetry SerialSend Executed!");
            //change to file writing mock test
        }

        public Task SerialSendAsync(string message)
        {
            return Task.Run(() => { SerialSend(message); });
        }
    }
}
