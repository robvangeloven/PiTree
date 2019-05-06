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
                //.AddSingleton<AzureDevopsApiService>()
                .AddSingleton<IMonitorService, ServiceBusService>()
                .AddSingleton<IOutputService, GPIOService>()
                .Configure<AzureDevopsApiOptions>(config.GetSection("AzureDevopsApi"))
                .Configure<ServiceBusOptions>(config.GetSection("ServiceBusOptions"))
                .Configure<GPIOServiceOptions>(config.GetSection("GPIOServiceOptions"))
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
