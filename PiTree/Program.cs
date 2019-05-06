using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PiTree.Monitor.AzureDevopsAPI;
using PiTree.Monitor.ServiceBus;
using PiTree.Output.GPIO;
using PiTree.Shared;

namespace PiTree
{
    internal class Program
    {
        private const string AZURE_DEVOPS_API_SECTION = "Monitor:AzureDevopsApi";
        private const string SERVICE_BUS_SECTION = "Monitor:ServiceBus";
        private const string GPIO_SECTION = "Output:GPIO";

        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        private static async Task Main(string[] args)
        {
            var userConfigDirectory = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}config";

            Directory.CreateDirectory(userConfigDirectory);

            if (!File.Exists($"{userConfigDirectory}{Path.DirectorySeparatorChar}appsettings.json"))
            {
                File.Copy($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}appsettings.json", $"{userConfigDirectory}{Path.DirectorySeparatorChar}appsettings.json");
            }

            // Setup Config
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"config{Path.DirectorySeparatorChar}appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings_dev.json", optional: true, reloadOnChange: true);

            var config = builder.Build();

            // Setup DI
            var services = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.AddConsole();
                    loggingBuilder.AddDebug();
                })
                .AddSingleton<IMonitorService>(serviceProvider =>
                {
                    if (config.GetSection(AZURE_DEVOPS_API_SECTION).Exists())
                    {
                        return serviceProvider.GetService<AzureDevopsApiService>();
                    }

                    if (config.GetSection(SERVICE_BUS_SECTION).Exists())
                    {
                        return serviceProvider.GetService<ServiceBusService>();
                    }

                    return null;
                })
                .AddSingleton<AzureDevopsApiService>()
                .AddSingleton<ServiceBusService>()
                .AddSingleton<IOutputService, GPIOService>()
                .Configure<AzureDevopsApiOptions>(config.GetSection(AZURE_DEVOPS_API_SECTION))
                .Configure<ServiceBusOptions>(config.GetSection(SERVICE_BUS_SECTION))
                .Configure<GPIOServiceOptions>(config.GetSection(GPIO_SECTION))
                .BuildServiceProvider();

            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting PiTree");

            var outputService = services.GetService<IOutputService>();
            var monitorService = services.GetService<IMonitorService>();
            await monitorService.Start();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _quitEvent.Set();
                eventArgs.Cancel = true;
            };

            _quitEvent.WaitOne();

            logger.LogInformation("PiTree stopping");

            await monitorService.Stop();
        }
    }
}
