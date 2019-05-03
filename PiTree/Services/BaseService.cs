using PiTree.WiringPi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PiTree.Services
{
    public abstract class BaseService : IService
    {
        public virtual Task Start()
        {
            LightHelper.Initialize();

            Console.WriteLine("Running diagnostics, activating all relays");
            LightHelper.LightsOn();
            Thread.Sleep(new TimeSpan(0, 0, 2));

            Console.WriteLine("End of diagnostics, deactivating all relays");
            LightHelper.LightsOff();
            Thread.Sleep(new TimeSpan(0, 0, 2));

            return Task.CompletedTask;
        }

        public virtual Task Stop()
        {
            LightHelper.LightsOff();

            return Task.CompletedTask;
        }
    }
}