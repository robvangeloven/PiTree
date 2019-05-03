using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PiTree.Services;

namespace PiTree
{
    internal class Program
    {
        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        private static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings_dev.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            var service = new QueueService(configuration);
            await service.Start();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _quitEvent.Set();
                eventArgs.Cancel = true;
            };

            _quitEvent.WaitOne();

            await service.Stop();
        }
    }
}
