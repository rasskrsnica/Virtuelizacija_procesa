using SensorCommon.Models;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace SensorService
{
    internal class SensorStorage : IDisposable
    {
        private readonly string storagePath;
        private StreamWriter writer;
        private StreamWriter rejectedWriter;
        private StreamWriter eventWriter;
        private string metadataFile;
        private bool disposed;

        internal string CurrentSessionDirectory { get; private set; }
        internal string CurrentSessionId { get; private set; }

        public SensorStorage()
        {
            storagePath = ResolvePath(ConfigurationManager.AppSettings["StorageDirectory"] ?? "App_Data");
            Directory.CreateDirectory(storagePath);
        }

        public string CreateSession(SessionMeta header)
        {
            string sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            string folder = Path.Combine(storagePath, "OfficeSensor", DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folder);
            CurrentSessionDirectory = folder;
            CurrentSessionId = sessionId;

            string sessionFile = Path.Combine(folder, sessionId + "_measurements_session.csv");
            string rejectedFile = Path.Combine(folder, sessionId + "_rejects.csv");
            string eventFile = Path.Combine(folder, sessionId + "_events.log");
            metadataFile = Path.Combine(folder, sessionId + "_metadata.txt");

            writer = new StreamWriter(sessionFile, false);
            rejectedWriter = new StreamWriter(rejectedFile, false);
            eventWriter = new StreamWriter(eventFile, false);
            writer.WriteLine("Volume,T_DHT,T_BMP,Pressure,DateTime");
            rejectedWriter.WriteLine("Reason,Volume,T_DHT,T_BMP,Pressure,DateTime");
            eventWriter.WriteLine("Time,Event,Message");
            writer.Flush();
            rejectedWriter.Flush();
            eventWriter.Flush();

            File.WriteAllLines(metadataFile, new[]
            {
                "Volume=" + header.Volume.ToString(CultureInfo.InvariantCulture),
                "T_DHT=" + header.T_DHT.ToString(CultureInfo.InvariantCulture),
                "T_BMP=" + header.T_BMP.ToString(CultureInfo.InvariantCulture),
                "Pressure=" + header.Pressure.ToString(CultureInfo.InvariantCulture),
                "DateTime=" + header.DateTime.ToString("o", CultureInfo.InvariantCulture),
                "SessionId=" + sessionId
            });

            return sessionId;
        }

        public void SaveMeasurement(SensorSample sample)
        {
            ThrowIfDisposed();
            writer.WriteLine(ToCsv(sample));
            writer.Flush();
        }

        public void SaveRejected(string reason, SensorSample sample)
        {
            ThrowIfDisposed();
            rejectedWriter.WriteLine("\"" + Escape(reason) + "\"," + ToCsv(sample));
            rejectedWriter.Flush();
        }

        public void SaveEvent(EventInfo eventInfo)
        {
            ThrowIfDisposed();
            eventWriter.WriteLine(string.Join(",",
                DateTime.Now.ToString("o", CultureInfo.InvariantCulture),
                eventInfo.Name,
                Escape(eventInfo.Message).Replace(",", ";")));
            eventWriter.Flush();
        }

        public void SaveSummary(SessionState state)
        {
            File.AppendAllLines(metadataFile, new[]
            {
                "ReceivedRows=" + state.SampleCount,
                "RejectedRows=" + state.RejectedCount
            });
        }

        private static string ToCsv(SensorSample sample)
        {
            return string.Join(",",
                sample.Volume.ToString(CultureInfo.InvariantCulture),
                sample.T_DHT.ToString(CultureInfo.InvariantCulture),
                sample.T_BMP.ToString(CultureInfo.InvariantCulture),
                sample.Pressure.ToString(CultureInfo.InvariantCulture),
                sample.DateTime.ToString("o", CultureInfo.InvariantCulture));
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\"", "\"\"");
        }

        private static string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (writer != null) writer.Dispose();
                    if (rejectedWriter != null) rejectedWriter.Dispose();
                    if (eventWriter != null) eventWriter.Dispose();
                }

                disposed = true;
            }
        }
    }
}
