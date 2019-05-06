using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PiTree.Shared;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace PiTree.Monitor.AzureDevopsAPI
{
    public class AzureDevopsApiService : IMonitorService
    {
        private CancellationTokenSource _cancellationTokenSource;

        private IOptionsMonitor<AzureDevopsApiOptions> _options;
        private IOutputService _outputService;
        private ILogger<AzureDevopsApiService> _logger;

        public AzureDevopsApiService(
            IOutputService outputService,
            IOptionsMonitor<AzureDevopsApiOptions> options,
            ILogger<AzureDevopsApiService> logger)
        {
            _outputService = outputService;
            _options = options;
            _logger = logger;
        }

        private async Task<MonitorStatus> GetBuildStatus()
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
                        dynamic responseBody = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

                        foreach (dynamic item in responseBody.value)
                        {
                            switch ((string)item.result)
                            {
                                case "succeeded":
                                    result |= MonitorStatus.Succeeded;
                                    break;

                                case "partiallySucceeded":
                                    result |= MonitorStatus.PartiallySucceeded;
                                    break;

                                default:
                                case "failed":
                                    result |= MonitorStatus.Failed;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{DateTimeOffset.Now}] {ex.ToString()}");
            }

            return result;
        }

        public async Task Start()
        {
            await _outputService.Start();

            _cancellationTokenSource = new CancellationTokenSource();

            await Task.Run(async () =>
             {
                 while (true)
                 {
                     await _outputService.SignalNewStatus(await GetBuildStatus());

                     await Task.Delay(new TimeSpan(0, 1, 0), _cancellationTokenSource.Token);
                 }
             }, _cancellationTokenSource.Token);
        }

        public async Task Stop()
        {
            await _outputService.Stop();

            _cancellationTokenSource.Cancel();
        }
    }
}
