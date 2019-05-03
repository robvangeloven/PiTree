using System;
using System.Threading;
using WiringPi;
using GPIOpinmode = WiringPi.GPIO.GPIOpinmode;

namespace RaspberryPi
{
    public static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Init.WiringPiSetup();
                Init.WiringPiSetupGpio();

                GPIO.pinMode(8, (int)GPIOpinmode.Output);
                GPIO.pinMode(9, (int)GPIOpinmode.Output);

                while (true)
                {
                    Console.WriteLine("On");
                    GPIO.digitalWrite(7, 0);
                    GPIO.digitalWrite(8, 1);
                    GPIO.digitalWrite(9, 0);
                    GPIO.digitalWrite(15, 1);

                    Thread.Sleep(1000);

                    Console.WriteLine("Off");
                    GPIO.digitalWrite(7, 1);
                    GPIO.digitalWrite(8, 0);
                    GPIO.digitalWrite(9, 1);
                    GPIO.digitalWrite(15, 0);

                    Thread.Sleep(1000);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
        }
    }
}
