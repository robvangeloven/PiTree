using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PiTree.WiringPi;

namespace PiTree.Services
{
    internal class ApiService : BaseService
    {
        private string _personalAccessToken;
        private string _endpoint;
        private CancellationTokenSource _cancellationTokenSource;

        public ApiService(IConfiguration config)
        {
            _endpoint = config["Endpoint"];
            _personalAccessToken = config["PersonalAccessToken"];

            int.TryParse(config["NumberOfBuilds"], out var numberOfBuilds);

            if (numberOfBuilds > 0)
            {
                _endpoint += $"&$top={numberOfBuilds}";
            }
        }

        private BuildStatus GetBuildStatus()
        {
            BuildStatus result = BuildStatus.None;

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
                                    result |= BuildStatus.Succeeded;
                                    break;

                                case "partiallySucceeded":
                                    result |= BuildStatus.PartiallySucceeded;
                                    break;

                                default:
                                case "failed":
                                    result |= BuildStatus.Failed;
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

        public override async Task Start()
        {
            await base.Start();

            _cancellationTokenSource = new CancellationTokenSource();

            await Task.Run(async () =>
             {
                 LightHelper.LightsOn();

                 while (true)
                 {
                     LightHelper.ShowBuildStatus(GetBuildStatus());

                     await Task.Delay(new TimeSpan(0, 1, 0), _cancellationTokenSource.Token);
                 }
             }, _cancellationTokenSource.Token);
        }

        public override async Task Stop()
        {
            await base.Stop();

            _cancellationTokenSource.Cancel();
        }
    }
}
