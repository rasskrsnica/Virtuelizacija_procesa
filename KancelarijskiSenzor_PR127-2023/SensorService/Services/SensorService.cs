using SensorCommon.Contracts;
using SensorCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;


namespace SensorService.Services
{
    public class SensorService : ISensorService
    {
        private string file = "Data/session.csv";
        private double? lastVolume = null;

        public string StartSession()
        {
            File.WriteAllText(file, "Volume,T_DHT,T_BMP,Pressure,DateTime\n");
            return "ACK";
        }

        public string PushSample(SensorSample s)
        {
            // VALIDACIJA
            if (s.Pressure <= 0)
                return "NACK";

            // ΔV (buka)
            if (lastVolume.HasValue)
            {
                double delta = Math.Abs(s.Volume - lastVolume.Value);

                if (delta > 10)
                    Console.WriteLine("⚠ Volume spike!");
            }

            lastVolume = s.Volume;

            // UPIS U FAJL
            string line =
                $"{s.Volume},{s.T_DHT},{s.T_BMP},{s.Pressure},{s.DateTime}";

            File.AppendAllText(file, line + "\n");

            return "ACK";
        }

        public string EndSession()
        {
            return "COMPLETED";
        }
    }
}