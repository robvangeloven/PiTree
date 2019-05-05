using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PiTree.Shared;

namespace PiTree.Services
{
    internal class AzureDevopsApiService : IMonitorService
    {
        private string _personalAccessToken;
        private string _endpoint;
        private CancellationTokenSource _cancellationTokenSource;

        private IOutputService _outputService;

        public AzureDevopsApiService(IOutputService outputService)
        {
            _outputService = outputService;

            //_endpoint = config["Endpoint"];
            //_personalAccessToken = config["PersonalAccessToken"];

            //int.TryParse(config["NumberOfBuilds"], out var numberOfBuilds);

            //if (numberOfBuilds > 0)
            //{
            //    _endpoint += $"&$top={numberOfBuilds}";
            //}
        }

        private MonitorStatus GetBuildStatus()
        {
            var result = MonitorStatus.Failed;

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
                Console.WriteLine($"[{DateTimeOffset.Now}] {ex.ToString()}");
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
                     await _outputService.SignalNewStatus(GetBuildStatus());

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
