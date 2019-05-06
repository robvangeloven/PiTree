using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PiTree.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace PiTree.Output.GPIO
{
    public class GPIOService : IOutputService
    {
        private const int STROBE_INTERVAL = 500;
        private const int STROBE_START_DELAY = 2000;
        private const int STROBE_END_DELAY = 3000;
        private const int STROBE_REPEAT = 3;

        private IOptionsMonitor<GPIOServiceOptions> _options;
        private ILogger _logger;

        private IDictionary<Light, IGpioPin> _pins = new Dictionary<Light, IGpioPin>();

        public GPIOService(
            IOptionsMonitor<GPIOServiceOptions> options,
            ILogger<GPIOService> logger)
        {
            _options = options;
            _logger = logger;
        }

        private bool TryParse(int value, out P1 pin)
        {
            return Enum.TryParse($"Pin{value}", out pin);
        }

        private void Initialize()
        {
            if (!TryParse(_options.CurrentValue.HardwarePinGreen, out P1 p1Pin))
            {
                p1Pin = P1.Pin03;
            }

            _pins[Light.Green] = Pi.Gpio[p1Pin];

            if (!TryParse(_options.CurrentValue.HardwarePinYellow, out p1Pin))
            {
                p1Pin = P1.Pin05;
            }

            _pins[Light.Yellow] = Pi.Gpio[p1Pin];

            if (!TryParse(_options.CurrentValue.HardwarePinRed, out p1Pin))
            {
                p1Pin = P1.Pin07;
            }

            _pins[Light.Red] = Pi.Gpio[p1Pin];

            _pins[Light.Green].PinMode = GpioPinDriveMode.Output;
            _pins[Light.Yellow].PinMode = GpioPinDriveMode.Output;
            _pins[Light.Red].PinMode = GpioPinDriveMode.Output;

            _logger.LogDebug($"[{DateTimeOffset.Now}] Initialize");
        }

        private void LightsOn()
        {
            LightOn(Light.Green);
            LightOn(Light.Yellow);
            LightOn(Light.Red);

            _logger.LogDebug($"[{DateTimeOffset.Now}] All lights on");
        }

        private void LightsOff()
        {
            LightOff(Light.Green);
            LightOff(Light.Yellow);
            LightOff(Light.Red);

            _logger.LogDebug($"[{DateTimeOffset.Now}] All lights off");
        }

        private void LightOn(Light light)
        {
            _pins[light].Write(GpioPinValue.High);

            _logger.LogDebug($"[{DateTimeOffset.Now}] Light on: {light}");
        }

        private void LightOff(Light light)
        {
            _pins[light].Write(GpioPinValue.Low);

            _logger.LogDebug($"[{DateTimeOffset.Now}] Light off: {light}");
        }

        private async Task Strobe()
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
                LightOn(Light.Yellow);
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
