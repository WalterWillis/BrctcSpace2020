using BlazorWebSpaceTool.Pages;
using BrctcSpaceLibrary.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Ports;

namespace BlazorWebSpaceTool.InputModels
{
    public class SerialPortInputModel
    {
        private UART _telemetry;

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
                catch (Exception ex)
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

        public void PrepareForTestRecieveEvent()
        {
            ErrorMessage = ""; //Reset Errors on next call

            if (!string.IsNullOrEmpty(SelectedPort) && !string.IsNullOrEmpty(Text))
            {
                try
                {
                    _telemetry = new UART(SelectedPort);

                    _telemetry.Subscribe((sender, e) =>
                        {
                            try
                            {
                                string result = _telemetry.SerialRead();

                                if (result == Text)
                                    TestSuccess = true;
                                else
                                {
                                    ErrorMessage = $"Recieved {result} and expected {Text}";
                                    TestSuccess = false;
                                }
                            }
                            catch (Exception ex) { ErrorMessage = $"Critical Failure during UART call: {ex.Message}"; }
                            finally { _telemetry.Unsubscribe(); _telemetry.Dispose(); } //Remove all handles and such
                    }
                    );
                }
                catch (Exception ex) { ErrorMessage = $"Critical Failure during UART call: {ex.Message}"; }
            }
            else
            {
                ErrorMessage = "Either the selected port is incorrect or no text has been prepared to send.";
            }
        }

        /// <summary>
        /// Used for continuous telemetry where data is written to disk one line at a time based on event triggers
        /// </summary>
        public void PrepareForContinuousRecieveEvent()
        {
            ErrorMessage = ""; //Reset Errors on next call

            if (!string.IsNullOrEmpty(SelectedPort))
            {
                try
                {
                    _telemetry = new UART(SelectedPort);

                    _telemetry.Subscribe((sender, e) =>
                    {
                        try
                        {
                            string result = _telemetry.SerialRead();
                            //Prevent too large of files being created by saving to files based on time down to the minute, rather than seconds
                            string fileName = Path.Combine(Directory.GetCurrentDirectory(), "Temp", $"Telemetry_{DateTime.Now.ToString("yyyy-dd-MM--HH-mm")}.csv"); 
                            FileInfo file = new FileInfo(fileName);

                            using (StreamWriter sw = file.AppendText())
                            {
                                sw.WriteLine(result);
                            }
                        }
                        catch (Exception ex) { ErrorMessage = $"Critical Failure during UART call: {ex.Message}"; }
                        finally { _telemetry.Unsubscribe(); _telemetry.Dispose(); } //Remove all handles and such
                    }
                    );
                }
                catch (Exception ex) { ErrorMessage = $"Critical Failure during UART call: {ex.Message}"; _telemetry.Unsubscribe(); _telemetry.Dispose(); }
            }
            else
            {
                ErrorMessage = "No port selected";
            }
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
