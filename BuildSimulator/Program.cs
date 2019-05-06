using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PiTree.Monitor.ServiceBus;
using PiTree.Shared;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BuildSimulator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
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
                .Configure<ServiceBusOptions>(config.GetSection("ServiceBusOptions"))
                .BuildServiceProvider();

            var serviceBusOptions = services.GetService<IOptionsMonitor<ServiceBusOptions>>();

            var queueClient = new QueueClient(serviceBusOptions.CurrentValue.QueueConnectionString, serviceBusOptions.CurrentValue.QueueName);

            while (true)
            {
                Console.WriteLine("Please select build status or 'X' to quit:");

                int index = 1;

                var monitorStates = (MonitorStatus[])Enum.GetValues(typeof(MonitorStatus));

                foreach (var state in monitorStates)
                {
                    Console.WriteLine($"{index++}) {state}");
                }

                var userInput = Console.ReadLine();

                if (userInput == "X")
                {
                    break;
                }

                if (int.TryParse(userInput, out int selectedState))
                {
                    string command;

                    switch (monitorStates[selectedState - 1])
                    {
                        case MonitorStatus.Succeeded:
                            command = "succeeded";
                            break;

                        case MonitorStatus.PartiallySucceeded:
                            command = "partiallySucceeded";
                            break;

                        default:
                        case MonitorStatus.Failed:
                            command = "failed";
                            break;
                    }

                    dynamic message = new JObject();
                    message.resource = new JObject();
                    message.resource.status = command;

                    await queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes(message.ToString())));
                }
                else
                {
                    Console.WriteLine($"Unknown input '{userInput}'");
                }

                Console.WriteLine();
            }
        }
    }
}