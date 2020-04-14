using BlazorWebSpaceTool.Pages;
using BrctcSpaceLibrary.Device;
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

        /// <summary>
        /// Send text via UART
        /// </summary>
        public void SendText()
        {
            if (!string.IsNullOrEmpty(SelectedPort) && !string.IsNullOrEmpty(Text))
            {
                ErrorMessage = ""; // Make sure there is no previous error message
                try
                {
                    using (UART telemetry = new UART(SelectedPort))
                    {
                        telemetry.SerialSend(Text);
                    }
                }
                catch(Exception ex)
                {
                    ErrorMessage = $"Send error: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Read text from UART
        /// </summary>
        /// <returns></returns>
        public string ReadText()
        {
            string message = "";
            if (!string.IsNullOrEmpty(SelectedPort) && !string.IsNullOrEmpty(Text))
            {
                ErrorMessage = "";
                try
                {                 
                    using (UART telemetry = new UART(SelectedPort))
                    {
                        message = telemetry.SerialRead();
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Read error: {ex.Message}";
                }              
            }
            else
            {
                ErrorMessage = "No text to send or invalid port selected";
            }

            return message;
        }

        /// <summary>
        /// Stores a volatile error message generated from the SendText and ReadText functions.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Used to indicate a successful test
        /// </summary>
        public bool TestSuccess { get; set; }
    }
}
