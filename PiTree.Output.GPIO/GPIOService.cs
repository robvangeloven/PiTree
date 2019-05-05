using PiTree.Shared;
using System;
using System.Threading.Tasks;
using static PiTree.OutputServices.GPIO.GPIO;

namespace PiTree.OutputServices.GPIO
{
    public class GPIOService : IOutputService
    {
        private const int STROBE_INTERVAL = 500;
        private const int STROBE_START_DELAY = 2000;
        private const int STROBE_END_DELAY = 3000;
        private const int STROBE_REPEAT = 3;

        private static void Initialize()
        {
            Init.WiringPiSetup();
            Init.WiringPiSetupGpio();

            GPIO.pinMode((int)Light.White, (int)GPIOpinmode.Output);
            GPIO.pinMode((int)Light.Green, (int)GPIOpinmode.Output);
            GPIO.pinMode((int)Light.Red, (int)GPIOpinmode.Output);

            Console.WriteLine($"[{DateTimeOffset.Now}] Initialize");
        }

        private static void LightsOn()
        {
            LightOn(Light.White);
            LightOn(Light.Green);
            LightOn(Light.Red);

            Console.WriteLine($"[{DateTimeOffset.Now}] All lights on");
        }

        private static void LightsOff()
        {
            LightOff(Light.White);
            LightOff(Light.Green);
            LightOff(Light.Red);

            Console.WriteLine($"[{DateTimeOffset.Now}] All lights off");
        }

        private static void LightOn(Light light)
        {
            GPIO.digitalWrite((int)light, 0);

            Console.WriteLine($"[{DateTimeOffset.Now}] Light on: {light}");
        }

        private static void LightOff(Light light)
        {
            GPIO.digitalWrite((int)light, 1);

            Console.WriteLine($"[{DateTimeOffset.Now}] Light off: {light}");
        }

        private static async Task Strobe()
        {
            LightsOff();

            await Task.Delay(STROBE_START_DELAY);

            for (int i = 0; i < STROBE_REPEAT; i++)
            {
                foreach (Light light in Enum.GetValues(typeof(Light)))
                {
                    LightOn(light);
                    await Task.Delay(STROBE_INTERVAL);
                    LightOff(light);
                }
            }

            await Task.Delay(STROBE_END_DELAY);
        }

        public async Task SignalNewStatus(MonitorStatus monitorStatus)
        {
            await Strobe();

            if (monitorStatus.HasFlag(MonitorStatus.Succeeded))
            {
                LightOn(Light.Green);
            }

            if (monitorStatus.HasFlag(MonitorStatus.PartiallySucceeded))
            {
                LightOn(Light.White);
            }

            if (monitorStatus.HasFlag(MonitorStatus.Failed))
            {
                LightOn(Light.Red);
            }

            Console.WriteLine($"[{DateTimeOffset.Now}] BuildStatus: {monitorStatus}");
        }

        public virtual async Task Start()
        {
            Initialize();

            Console.WriteLine("Running diagnostics, activating all relays");
            LightsOn();
            await Task.Delay(STROBE_START_DELAY);

            Console.WriteLine("End of diagnostics, deactivating all relays");
            LightsOff();
            await Task.Delay(STROBE_END_DELAY);
        }

        public virtual Task Stop()
        {
            LightsOff();

            return Task.CompletedTask;
        }
    }
}
