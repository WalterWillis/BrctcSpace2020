using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GrpcSpaceServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseStartup<Startup>();
                         webBuilder.UseKestrel(options =>
                         {
                             options.ListenAnyIP(5443, listenOptions =>
                             {
                                 var config = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json", optional: false)
                                    .Build();

                                 string certPath = config.GetSection("ServerCert").Value;
                                 string certPass = config.GetSection("ServerCertPass").Value;
                                 listenOptions.UseHttps(certPath, certPass, o =>
                                 {
                                     //o.AllowAnyClientCertificate();

                                     o.ClientCertificateValidation = (cert, chain, errors) =>
                                     {
                                         string certPath = config.GetSection("ClientCert").Value;
                                         string certPass = config.GetSection("ClientCertPass").Value;

                                         var clientCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, certPass);

                                         return cert.Equals(clientCert);
                                     };
                                 });
                             });
                         });
                 });
    }
}
