using SensorCommon.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensorCommon;
using System.IO;
using System.ServiceModel;
using SensorCommon.Contracts;

namespace SensorClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ChannelFactory<ISensorService>(
            new NetTcpBinding(),
            new EndpointAddress("net.tcp://localhost:9000/SensorService")
        );

            var service = factory.CreateChannel();

            service.StartSession();

            var lines = File.ReadLines("AirPi Data - AirPi.csv")
                            .Skip(1)
                            .Take(100);

            foreach (var line in lines)
            {
                var p = line.Split(',');

                SensorSample s = new SensorSample
                {
                    DateTime = DateTime.Parse(p[0]),
                    Volume = double.Parse(p[1], CultureInfo.InvariantCulture),
                    T_DHT = double.Parse(p[3], CultureInfo.InvariantCulture),
                    Pressure = double.Parse(p[4], CultureInfo.InvariantCulture),
                    T_BMP = double.Parse(p[5], CultureInfo.InvariantCulture)
                };

                Console.WriteLine(service.PushSample(s));
            }

            service.EndSession();
        }
    }
}
