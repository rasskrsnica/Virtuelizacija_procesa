using SensorService;
using System;
using System.ServiceModel;
using System.Threading;

namespace Service
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("simulate-dispose", StringComparison.OrdinalIgnoreCase))
            {
                ResourceDisposeSimulation.RunStorageSimulation();
                return;
            }

            ServiceHost host = new ServiceHost(typeof(Service1));
            host.Open();

            Console.WriteLine("Sensor service is open.");
            Console.WriteLine("Endpoint: net.tcp://localhost:9000/SensorService");
            Console.WriteLine("Press any key to close service.");

            if (Console.IsInputRedirected)
            {
                Thread.Sleep(Timeout.Infinite);
            }
            else
            {
                Console.ReadKey();
            }

            host.Close();
            Console.WriteLine("Sensor service is closed.");
        }
    }
}
