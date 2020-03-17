using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Vibe2020Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Configuration = builder.Build();

                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                ServiceProvider = serviceCollection.BuildServiceProvider();

                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                using (var sw = File.CreateText(Path.Combine(Directory.GetCurrentDirectory(), "Log_latest.txt")))
                {
                    sw.WriteLine("Error ocurred during startup.");
                    sw.WriteLine("Check and make sure you have your 'appsettings.json' file.");
                    sw.WriteLine("Check and make sure you have your server.pfx and client.pfx files as marked in the json file.");
                    sw.WriteLine("Error message: " + ex.Message);
                    sw.WriteLine("Stacktrace: " + ex.StackTrace);
                }
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient(typeof(MainWindow)).AddSingleton<IConfiguration>(Configuration);
        }
    }
}
