using SensorCommon.Models;
using System;
using System.IO;

namespace SensorService
{
    internal static class ResourceDisposeSimulation
    {
        public static void RunStorageSimulation()
        {
            string sessionFolder = null;
            string sessionId = null;

            try
            {
                using (SensorStorage storage = new SensorStorage())
                {
                    storage.CreateSession(new SessionMeta
                    {
                        DateTime = DateTime.Now,
                        Volume = 100,
                        T_DHT = 24,
                        T_BMP = 24,
                        Pressure = 1010
                    });
                    sessionFolder = storage.CurrentSessionDirectory;
                    sessionId = storage.CurrentSessionId;
                    storage.SaveMeasurement(new SensorSample
                    {
                        DateTime = DateTime.Now,
                        Volume = 101,
                        T_DHT = 24.1,
                        T_BMP = 24.2,
                        Pressure = 1011
                    });

                    throw new InvalidOperationException("Simuliran prekid prenosa tokom upisa na serveru.");
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("[SIMULACIJA] Uhvaćen izuzetak: " + ex.Message);
            }

            if (!string.IsNullOrWhiteSpace(sessionFolder) && !string.IsNullOrWhiteSpace(sessionId) && Directory.Exists(sessionFolder))
            {
                foreach (string file in Directory.GetFiles(sessionFolder, sessionId + "_*"))
                {
                    File.Delete(file);
                }

                Console.WriteLine("[SIMULACIJA] Server fajlovi su obrisani, writer-i nisu ostali zakljucani.");
            }
        }
    }
}
