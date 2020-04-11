using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO.Ports;

namespace BlazorWebSpaceTool.InputModels
{
    public class SerialPortInputModel
    {
        public string[] AvailablePorts { get => SerialPort.GetPortNames() ?? new string[] { "None" }; }

        [Required(ErrorMessage = "Select a port", AllowEmptyStrings = false)]
        public string SelectedPort { get; set; }

        [Required(ErrorMessage = "There needs to be a message to send", AllowEmptyStrings = false)]
        public string Text { get; set; }

        public void SendText()
        {
            if (!string.IsNullOrEmpty(SelectedPort) && !string.IsNullOrEmpty(Text))
            {
                try
                {
                    using (SerialPort serialPort = new SerialPort(SelectedPort))
                    {
                        serialPort.Open();
                        serialPort.Write(Text);
                        serialPort.Close();
                    }
                }
                catch(Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
            }
        }

        public string ErrorMessage { get; set; }
    }
}
