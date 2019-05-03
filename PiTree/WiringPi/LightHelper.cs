using System;
using System.Threading;
using static PiTree.WiringPi.GPIO;

namespace PiTree.WiringPi
{
    public static class LightHelper
    {
        private const int STROBE_INTERVAL = 500;

        public static void Initialize()
        {
            Init.WiringPiSetup();
            Init.WiringPiSetupGpio();

            GPIO.pinMode((int)Light.White, (int)GPIOpinmode.Output);
            GPIO.pinMode((int)Light.Green, (int)GPIOpinmode.Output);
            GPIO.pinMode((int)Light.Red, (int)GPIOpinmode.Output);

            Console.WriteLine($"[{DateTimeOffset.Now}] Initialize");
        }

        public static void LightsOn()
        {
            LightOn(Light.White);
            LightOn(Light.Green);
            LightOn(Light.Red);

            Console.WriteLine($"[{DateTimeOffset.Now}] All lights on");
        }

        public static void LightsOff()
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

        private static void Strobe()
        {
            LightsOff();
            Thread.Sleep(2000);

            for (int i = 0; i < 3; i++)
            {
                foreach (Light light in Enum.GetValues(typeof(Light)))
                {
                    LightOn(light);
                    Thread.Sleep(STROBE_INTERVAL);
                    LightOff(light);
                }
            }

            Thread.Sleep(3000);
        }

        public static void ShowBuildStatus(BuildStatus buildStatus)
        {
            Strobe();

            if (buildStatus.HasFlag(BuildStatus.PartiallySucceeded))
            {
                LightOn(Light.White);
            }

            if (buildStatus.HasFlag(BuildStatus.Succeeded))
            {
                LightOn(Light.Green);
            }

            if (buildStatus.HasFlag(BuildStatus.Failed))
            {
                LightOn(Light.Red);
            }

            Console.WriteLine($"[{DateTimeOffset.Now}] BuildStatus: {buildStatus}");
        }
    }
}
