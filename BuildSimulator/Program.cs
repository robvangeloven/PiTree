using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using PiTree.WiringPi;
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

            IConfigurationRoot configuration = builder.Build();

            string serviceBusConnectionString = configuration["QueueConnectionString"];
            string queueName = configuration["QueueName"];

            var queueClient = new QueueClient(serviceBusConnectionString, queueName);

            while (true)
            {
                Console.WriteLine("Please select build status or 'X' to quit:");

                int index = 1;

                Light[] lights = (Light[])Enum.GetValues(typeof(Light));

                foreach (var light in lights)
                {
                    Console.WriteLine($"{index++}) {light}");
                }

                var userInput = Console.ReadLine();

                if (userInput == "X")
                {
                    break;
                }

                if (int.TryParse(userInput, out int selectedLight))
                {
                    string command;

                    switch (lights[selectedLight - 1])
                    {
                        case Light.Green:
                            command = "succeeded";
                            break;

                        case Light.White:
                            command = "partiallySucceeded";
                            break;

                        default:
                        case Light.Red:
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