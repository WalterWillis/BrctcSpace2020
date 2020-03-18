using BrctcSpace;
using Google.Protobuf;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Vibe2020ClientLibrary
{
    public class GrpcClient
    {
        private Vibe.VibeClient _client;
        private GrpcChannel _channel;

        public GrpcClient(X509Certificate2 clientCert, X509Certificate2 serverCert, string url)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(clientCert);
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                return cert.Equals(serverCert);
            };
            var httpClient = new HttpClient(handler);
            Uri uri = new Uri(url);
            //Create the channel given our parameters
            _channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions { HttpClient = httpClient });
            _client = new Vibe.VibeClient(_channel);

        }

        public Dictionary<byte, short> GetGyroRegisters(List<byte> registers)
        {
            GyroRegisterList list = new GyroRegisterList();
            foreach (var reg in registers)
            {
                list.RegisterList.Add(new GyroRegisterData() { Register = reg });
            }

            var result = _client.GetGyroRegisters(list);

            Dictionary<byte, short> dictionary = new Dictionary<byte, short>();

            foreach (var data in result.RegisterList)
            {
                dictionary.Add((byte)data.Register, (short)data.Value);
            }

            return dictionary;

        }

        public Dictionary<byte, short> SetGyroRegisters(Dictionary<byte, short> registers)
        {
           
            Dictionary<byte, short> dictionary = new Dictionary<byte, short>();
            foreach (var reg in registers)
            {
                GyroRegisterList list = new GyroRegisterList();
                list.RegisterList.Add(new GyroRegisterData() { Register = reg.Key, Value = reg.Value });
                var result = _client.GetGyroRegisters(list);
                dictionary.Add(reg.Key, (short)result.RegisterList[0].Value);
            }

     


            return dictionary;

        }
    }
}
