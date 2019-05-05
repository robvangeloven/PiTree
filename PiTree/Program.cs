using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PiTree.Monitor.AzureDevopsAPI;
using PiTree.Monitor.ServiceBus;
using PiTree.OutputServices.GPIO;

namespace PiTree
{
    internal class Program
    {
        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        private static async Task Main(string[] args)
        {
            // Setup Config
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings_dev.json", optional: true, reloadOnChange: true);

            var config = builder.Build();

            // Setup DI
            var services = new ServiceCollection()
                .AddLogging(x =>
                {
                    x.AddConsole();
                    x.AddDebug();
                })
                .AddSingleton<AzureDevopsApiService>()
                .AddSingleton<ServiceBusService>()
                .AddSingleton<GPIOService>()
                .Configure<AzureDevopsApiOptions>(config.GetSection("AzureDevopsApi"))
                .Configure<ServiceBusOptions>(config.GetSection("ServiceBusOptions"))
                .BuildServiceProvider();

            var logger = services
                .GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            logger.LogDebug("Starting PiTree");

            var outputService = new GPIOService();
            var monitorService = new ServiceBusService(outputService, null);
            await monitorService.Start();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _quitEvent.Set();
                eventArgs.Cancel = true;
            };

            _quitEvent.WaitOne();

            logger.LogDebug("PiTree stopping");

            await monitorService.Stop();
        }
    }
}
