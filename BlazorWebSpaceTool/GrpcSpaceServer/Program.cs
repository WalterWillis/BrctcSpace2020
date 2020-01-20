using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
                     if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
                     {
                         webBuilder.UseKestrel(options =>
                         {
                             options.ListenAnyIP(5443, listenOptions =>
                             {
                                 string certPath = "server.pfx";
                                 listenOptions.UseHttps(certPath, "1234");
                             });
                         });
                     }
                 });
    }
}
