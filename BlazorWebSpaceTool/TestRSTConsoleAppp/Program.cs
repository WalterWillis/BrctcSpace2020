using System;
using System.Device.Gpio;
using System.Diagnostics;

namespace TestRSTConsoleApp
{
    class Program
    {
        /// <summary>
        /// Test and see if a pin set to output on high resets after a few seconds or remains set.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            const int RST_PIN = 15;
            const int RANDO = 31;

            using (var gpio = new GpioController(PinNumberingScheme.Board))
            {
                gpio.OpenPin(RST_PIN, PinMode.Output);
                gpio.Write(RST_PIN, PinValue.High);
                gpio.OpenPin(RANDO, PinMode.Input);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                while (stopwatch.Elapsed.TotalSeconds < 30)
                    if (gpio.Read(RANDO) == PinValue.Low)
                        Console.WriteLine("Pin does not stay high!");

                //NOTE: Test indicates that the pin stays set to high. 
                gpio.ClosePin(RST_PIN);
                gpio.ClosePin(RANDO);

                stopwatch.Stop();
            }
        }
    }
}
