using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PiTree.WiringPi;

namespace PiTree.Services
{
    public class QueueService : BaseService
    {
        private string _serviceBusConnectionString;
        private string _queueName;
        private string _personalAccessToken;
        private string _endpoint;
        private static IQueueClient _queueClient;

        public QueueService(IConfiguration config)
        {
            _serviceBusConnectionString = config["QueueConnectionString"];
            _queueName = config["QueueName"];
            _endpoint = $"{config["Endpoint"]}&$top=1";
            _personalAccessToken = config["PersonalAccessToken"];
        }

        public override async Task Start()
        {
            await base.Start();

            _queueClient = new QueueClient(_serviceBusConnectionString, _queueName);

            ShowLastBuildStatus();

            // Register QueueClient's MessageHandler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();
        }

        public override async Task Stop()
        {
            await base.Stop();

            await _queueClient.CloseAsync();
        }

        private BuildStatus ParseBuildStatus(string buildStatus)
        {
            switch (buildStatus)
            {
                case "succeeded": return BuildStatus.Succeeded;
                case "partiallySucceeded": return BuildStatus.PartiallySucceeded;
                default:
                case "failed": return BuildStatus.Failed;
            }
        }

        private void ShowLastBuildStatus()
        {
            BuildStatus result = BuildStatus.Failed;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", _personalAccessToken))));

                    using (var response = client.GetAsync(_endpoint).Result)
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

            LightHelper.ShowBuildStatus(result);
        }

        private async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            try
            {
                Console.WriteLine(JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body)));

                // Process the message
                dynamic body = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body));

                LightHelper.ShowBuildStatus(ParseBuildStatus((string)body.resource.status));
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