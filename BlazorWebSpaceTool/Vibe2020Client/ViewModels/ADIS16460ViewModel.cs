using BrctcSpace;
using BrctcSpaceLibrary;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Vibe2020ClientLibrary;

namespace Vibe2020Client.ViewModels
{
    public class ADIS16460ViewModel : BaseViewModel
    {
        GrpcClient _client;
        private IConfiguration _configuration;
        private bool _isConnected = false;

        private string _status;
        private short _settings; //MSC_CTRL
        private short _productID;

        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public short Settings { get => _settings; set => SetProperty(ref _settings, value); }
        public short ProductID { get => _productID; set => SetProperty(ref _productID, value); }

        public ADIS16460ViewModel(IConfiguration configuration)
        {
            _configuration = configuration;
            Setup(); 
        }

        public void GetSettings()
        {
            try
            {
                List<byte> registerList = new List<byte>();
                byte register = (byte)Register.MSC_CTRL;
                registerList.Add(register);
                var dictionary = _client.GetGyroRegisters(registerList);
                Settings = dictionary[register];
            }
            catch
            {
                Status = "Error retrieving settings.";
            }
        }

        private void Setup()
        {
            X509Certificate2 clientCert = Utilities.GetClientCertificate(_configuration.GetSection("ClientCert").Value, _configuration.GetSection("ClientCertPass").Value);
            X509Certificate2 serverCert = Utilities.GetServerCertificate(_configuration.GetSection("ServerCert").Value, _configuration.GetSection("ServerCertPass").Value);
            try
            {
                clientCert = Utilities.GetClientCertificate(_configuration.GetSection("ClientCert").Value, _configuration.GetSection("ClientCertPass").Value);
                serverCert = Utilities.GetServerCertificate(_configuration.GetSection("ServerCert").Value, _configuration.GetSection("ServerCertPass").Value);
            }
            catch (Exception)
            {
                MessageBox.Show($"Error configuring connection to server. Cannot find certificate(s). " +
                    $"{Environment.NewLine} Program requires a server.pfx and client.pfx file. ");

                Application.Current.Shutdown();
            }

            try
            {
                _client = new GrpcClient(clientCert, serverCert, _configuration.GetSection("TargetURL").Value);
                Status = "Idle";
                _isConnected = true;
            }
            catch (Exception)
            {
                MessageBox.Show($"Error configuring connection to server. Server is offline. " +
                   $"{Environment.NewLine} Start the service and then click 'Connect'. ");

                Status = "Disconnected";
            }
        }
    }
}
