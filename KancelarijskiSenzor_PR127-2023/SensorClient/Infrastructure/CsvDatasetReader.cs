using SensorCommon.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SensorClient.Infrastructure
{
    public sealed class CsvDatasetReader : IDisposable
    {
        private readonly StreamReader reader;
        private readonly StreamWriter rejectLog;
        private readonly int maxValidRows;
        private bool disposed;

        public CsvDatasetReader(string datasetPath, string rejectLogPath, int maxValidRows)
        {
            if (!File.Exists(datasetPath))
            {
                throw new FileNotFoundException("Dataset nije pronadjen.", datasetPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(rejectLogPath));
            reader = new StreamReader(new FileStream(datasetPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            rejectLog = new StreamWriter(new FileStream(rejectLogPath, FileMode.Create, FileAccess.Write, FileShare.Read));
            rejectLog.WriteLine("LineNumber,Reason,RawLine");
            this.maxValidRows = maxValidRows;
        }

        public IList<SensorSample> ReadSamples()
        {
            ThrowIfDisposed();
            var samples = new List<SensorSample>();
            string line;
            int lineNumber = 0;

            reader.ReadLine();
            lineNumber++;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                if (samples.Count >= maxValidRows)
                {
                    WriteReject(lineNumber, "Red viska posle prvih " + maxValidRows + " validnih uzoraka.", line);
                    continue;
                }

                SensorSample sample;
                string error;
                if (TryParse(line, out sample, out error))
                {
                    samples.Add(sample);
                }
                else
                {
                    WriteReject(lineNumber, error, line);
                }
            }

            rejectLog.Flush();
            return samples;
        }

        private static bool TryParse(string line, out SensorSample sample, out string error)
        {
            sample = null;
            error = null;
            var parts = line.Split(',');

            if (parts.Length < 6)
            {
                error = "CSV red nema obavezna polja.";
                return false;
            }

            DateTime dateTime;
            double volume;
            double tDht;
            double pressure;
            double tBmp;

            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime))
            {
                error = "Neispravan DateTime.";
                return false;
            }

            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out volume))
            {
                error = "Neispravan Volume.";
                return false;
            }

            if (!double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out tDht))
            {
                error = "Neispravna DHT temperatura.";
                return false;
            }

            if (!double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out pressure))
            {
                error = "Neispravan Pressure.";
                return false;
            }

            if (!double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out tBmp))
            {
                error = "Neispravna BMP temperatura.";
                return false;
            }

            sample = new SensorSample
            {
                DateTime = dateTime,
                Volume = volume,
                T_DHT = tDht,
                Pressure = pressure,
                T_BMP = tBmp
            };

            return true;
        }

        private void WriteReject(int lineNumber, string reason, string rawLine)
        {
            rejectLog.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},\"{1}\",\"{2}\"", lineNumber, Escape(reason), Escape(rawLine)));
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\"", "\"\"");
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            reader.Dispose();
            rejectLog.Dispose();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
