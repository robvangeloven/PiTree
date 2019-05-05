using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PiTree.Shared;

namespace PiTree.Monitor.ServiceBus
{
    public class ServiceBusService : IMonitorService
    {
        private static IQueueClient _queueClient;
        private IOutputService _outputService;
        private IOptionsMonitor<ServiceBusOptions> _options;

        public ServiceBusService(
            IOutputService outputService,
            IOptionsMonitor<ServiceBusOptions> options)
        {
            _outputService = outputService;
            _options = options;
        }

        public async Task Start()
        {
            await _outputService.Start();
            _queueClient = new QueueClient(_options.CurrentValue.ServiceBusConnectionString, _options.CurrentValue.QueueName);

            await ShowLastBuildStatus();

            // Register QueueClient's MessageHandler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();
        }

        public async Task Stop()
        {
            await _outputService.Stop();
            await _queueClient.CloseAsync();
        }

        private MonitorStatus ParseBuildStatus(string buildStatus)
        {
            switch (buildStatus)
            {
                case "succeeded": return MonitorStatus.Succeeded;
                case "partiallySucceeded": return MonitorStatus.PartiallySucceeded;
                default:
                case "failed": return MonitorStatus.Failed;
            }
        }

        private async Task ShowLastBuildStatus()
        {
            var result = MonitorStatus.Failed;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", _options.CurrentValue.PersonalAccessToken))));

                    using (var response = await client.GetAsync(_options.CurrentValue.Endpoint))
                    {
                        response.EnsureSuccessStatusCode();
                        dynamic responseBody = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);

                        result = ParseBuildStatus((string)responseBody["value"].First["result"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTimeOffset.Now}] {ex.ToString()}");
            }

            await _outputService.SignalNewStatus(result);
        }

        private async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            try
            {
                Console.WriteLine(JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body)));

                // Process the message
                dynamic body = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body));

                await _outputService.SignalNewStatus(ParseBuildStatus((string)body.resource.status));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTimeOffset.Now}] {ex.ToString()}");
            }

            // Complete the message so that it is not received again.
            // This can be done only if the queueClient is opened in ReceiveMode.PeekLock mode (which is default).

            await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            return Task.CompletedTask;
        }

        private void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False value below indicates the Complete will be handled by the User Callback as seen in `ProcessMessagesAsync`.
                AutoComplete = false
            };

            // Register the function that will process messages
            _queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }
    }
}