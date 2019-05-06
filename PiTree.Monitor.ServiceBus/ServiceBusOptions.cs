namespace PiTree.Monitor.ServiceBus
{
    public class ServiceBusOptions
    {
        public string QueueConnectionString { get; set; }

        public string QueueName { get; set; }

        public string Endpoint { get; set; }

        public string PersonalAccessToken { get; set; }
    }
}
