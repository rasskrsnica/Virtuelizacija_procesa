using SensorClient.Infrastructure;
using SensorCommon.Contracts;
using SensorCommon.Models;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace SensorClient
{
    internal class Program
    {
        private const int MaxRowsToSend = 100;

        private static void Main(string[] args)
        {
            ChannelFactory<ISensorService> factory = null;
            IClientChannel channel = null;

            try
            {
                factory = new ChannelFactory<ISensorService>("SensorServiceEndpoint");
                ISensorService proxy = factory.CreateChannel();
                channel = (IClientChannel)proxy;

                int option;
                do
                {
                    option = PrintMenu();
                    switch (option)
                    {
                        case 1:
                            SendTestSession(proxy);
                            break;
                        case 2:
                            SendCsvDataset(proxy);
                            break;
                        case 3:
                            SimulateReaderDisposeAfterException();
                            break;
                        case 4:
                            Console.WriteLine("Kraj programa.");
                            break;
                        default:
                            Console.WriteLine("Nepostojeca opcija.");
                            break;
                    }
                }
                while (option != 4);

                channel.Close();
                factory.Close();
            }
            catch (EndpointNotFoundException ex)
            {
            Console.WriteLine("Service nije dostupan: " + ex.Message);
            Console.WriteLine("Pokreni Service ili ponovo pokreni klijent da proba automatsko pokretanje.");
                TryStartSensorHost();
                Abort(channel, factory);
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine("[DATA FORMAT FAULT] " + ex.Detail.FieldName + " - " + ex.Detail.Message);
                Abort(channel, factory);
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("[VALIDATION FAULT] " + ex.Detail.FieldName + " - " + ex.Detail.Message);
                Abort(channel, factory);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GRESKA: " + ex.Message);
                Abort(channel, factory);
            }
        }

        private static int PrintMenu()
        {
            Console.WriteLine("------------------------------");
            Console.WriteLine("1. Posalji test merenja");
            Console.WriteLine("2. Posalji prvih 100 redova iz CSV dataset-a");
            Console.WriteLine("3. Simuliraj izuzetak i provera Dispose reader-a");
            Console.WriteLine("4. Izlaz");
            Console.Write("Izbor: ");

            int option;
            if (int.TryParse(Console.ReadLine(), out option)) return option;
            return 0;
        }

        private static void SendTestSession(ISensorService proxy)
        {
            SensorSample[] samples =
            {
                new SensorSample { DateTime = DateTime.Now, Volume = 30, T_DHT = 22.0, T_BMP = 22.1, Pressure = 1008 },
                new SensorSample { DateTime = DateTime.Now.AddSeconds(1), Volume = 32, T_DHT = 22.2, T_BMP = 22.2, Pressure = 1008 },
                new SensorSample { DateTime = DateTime.Now.AddSeconds(2), Volume = 48, T_DHT = 24.0, T_BMP = 23.6, Pressure = 1009 },
                new SensorSample { DateTime = DateTime.Now.AddSeconds(3), Volume = 20, T_DHT = 21.7, T_BMP = 21.9, Pressure = 1007 }
            };

            StartAndSend(proxy, samples);
        }

        private static void SendCsvDataset(ISensorService proxy)
        {
            string datasetPath = ResolvePath(ConfigurationManager.AppSettings["DatasetPath"], Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Data.csv"));
            string rejectLogPath = ResolvePath(ConfigurationManager.AppSettings["ClientRejectLogPath"], Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "client_rejects.csv"));

            using (CsvDatasetReader reader = new CsvDatasetReader(datasetPath, rejectLogPath, MaxRowsToSend))
            {
                var samples = reader.ReadSamples();
                if (samples.Count == 0)
                {
                    Console.WriteLine("Nema validnih uzoraka za slanje.");
                    return;
                }

                Console.WriteLine("Ucitano validnih uzoraka: " + samples.Count);
                Console.WriteLine("Reject log: " + rejectLogPath);
                StartAndSend(proxy, samples);
            }
        }

        private static void SimulateReaderDisposeAfterException()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DisposeSimulation");
            string csvPath = Path.Combine(folder, "dispose_test.csv");
            string rejectPath = Path.Combine(folder, "dispose_rejects.csv");

            Directory.CreateDirectory(folder);
            File.WriteAllLines(csvPath, new[]
            {
                "Date time,Volume [mV],Light_Level [Ohms],Temperature-DHT [Celsius],Pressure [Hectopascal],Temperature-BMP [Celsius]",
                "2016-11-27 17:22:45,100,5000,24.05,1024.43,24.13"
            });

            try
            {
                using (CsvDatasetReader reader = new CsvDatasetReader(csvPath, rejectPath, MaxRowsToSend))
                {
                    reader.ReadSamples();
                    throw new InvalidOperationException("Simuliran prekid tokom citanja CSV fajla.");
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("[SIMULACIJA] Uhvacen izuzetak: " + ex.Message);
            }

            File.Delete(csvPath);
            File.Delete(rejectPath);
            Directory.Delete(folder);
            Console.WriteLine("[SIMULACIJA] Privremeni fajlovi su obrisani, reader/reject log nisu ostali zakljucani.");
        }

        private static void StartAndSend(ISensorService proxy, System.Collections.Generic.IList<SensorSample> samples)
        {
            SensorSample first = samples[0];
            SensorResponse startResult = StartSessionWithRetry(proxy, new SessionMeta
            {
                Volume = first.Volume,
                T_DHT = first.T_DHT,
                T_BMP = first.T_BMP,
                Pressure = first.Pressure,
                DateTime = first.DateTime
            });
            PrintResult(startResult);

            foreach (SensorSample sample in samples)
            {
                try
                {
                    PrintResult(proxy.PushSample(sample));
                    Thread.Sleep(25);
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    Console.WriteLine("[FAULT] " + ex.Detail.FieldName + " - " + ex.Detail.Message);
                }
                catch (FaultException<ValidationFault> ex)
                {
                    Console.WriteLine("[FAULT] " + ex.Detail.FieldName + " - " + ex.Detail.Message);
                }
            }

            PrintResult(proxy.EndSession());
        }

        private static SensorResponse StartSessionWithRetry(ISensorService service, SessionMeta meta)
        {
            Exception lastError = null;
            bool hostStarted = false;

            for (int attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    return service.StartSession(meta);
                }
                catch (EndpointNotFoundException ex)
                {
                    lastError = ex;
                    if (!hostStarted)
                    {
                        hostStarted = TryStartSensorHost();
                    }

                    Console.WriteLine("Cekam Service da otvori net.tcp port... pokusaj " + attempt + "/10");
                    Thread.Sleep(1000);
                }
                catch (CommunicationException ex)
                {
                    lastError = ex;
                    if (!hostStarted)
                    {
                        hostStarted = TryStartSensorHost();
                    }

                    Console.WriteLine("Cekam Service da bude spreman... pokusaj " + attempt + "/10");
                    Thread.Sleep(1000);
                }
            }

            throw lastError ?? new EndpointNotFoundException("Service nije dostupan.");
        }

        private static bool TryStartSensorHost()
        {
            string hostPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Service", "bin", "Debug", "Service.exe"));

            if (!File.Exists(hostPath))
            {
                Console.WriteLine("Service.exe nije pronadjen. Uradi Build Solution pa pokreni ponovo.");
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = hostPath,
                    WorkingDirectory = Path.GetDirectoryName(hostPath),
                    UseShellExecute = true
                });
                Console.WriteLine("Pokrecem Service automatski...");
                Thread.Sleep(2000);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ne mogu da pokrenem Service: " + ex.Message);
                return false;
            }
        }

        private static void PrintResult(SensorResponse result)
        {
            if (result == null) return;

            Console.WriteLine("[" + (result.Ack ? "ACK" : "NACK") + "/" + result.Status + "] " + result.Message);
            foreach (EventInfo eventInfo in result.Events)
            {
                Console.WriteLine("[EVENT] " + eventInfo.Name + " - " + eventInfo.Message);
            }
        }

        private static string ResolvePath(string configuredPath, string defaultPath)
        {
            string path = string.IsNullOrWhiteSpace(configuredPath) ? defaultPath : configuredPath;
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
        }

        private static void Abort(IClientChannel channel, ChannelFactory<ISensorService> factory)
        {
            if (channel != null && channel.State != CommunicationState.Closed)
            {
                channel.Abort();
            }

            if (factory != null && factory.State != CommunicationState.Closed)
            {
                factory.Abort();
            }
        }
    }
}
