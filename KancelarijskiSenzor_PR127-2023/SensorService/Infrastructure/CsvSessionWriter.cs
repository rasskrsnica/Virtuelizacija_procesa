using SensorCommon.Models;
using System;
using System.Globalization;
using System.IO;

namespace SensorService.Infrastructure
{
    public sealed class CsvSessionWriter : IDisposable
    {
        private readonly StreamWriter measurementsWriter;
        private readonly StreamWriter rejectsWriter;
        private bool disposed;

        public CsvSessionWriter(string sessionDirectory)
        {
            Directory.CreateDirectory(sessionDirectory);

            measurementsWriter = new StreamWriter(
                new FileStream(Path.Combine(sessionDirectory, "measurements_session.csv"), FileMode.Create, FileAccess.Write, FileShare.Read));
            rejectsWriter = new StreamWriter(
                new FileStream(Path.Combine(sessionDirectory, "rejects.csv"), FileMode.Create, FileAccess.Write, FileShare.Read));

            measurementsWriter.WriteLine("Volume,T_DHT,T_BMP,Pressure,DateTime");
            rejectsWriter.WriteLine("Reason,Volume,T_DHT,T_BMP,Pressure,DateTime");
        }

        public void WriteMeasurement(SensorSample sample)
        {
            ThrowIfDisposed();
            measurementsWriter.WriteLine(ToCsv(sample));
            measurementsWriter.Flush();
        }

        public void WriteReject(SensorSample sample, string reason)
        {
            ThrowIfDisposed();
            rejectsWriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "\"{0}\",{1}", Escape(reason), ToCsv(sample)));
            rejectsWriter.Flush();
        }

        private static string ToCsv(SensorSample sample)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4:O}",
                sample.Volume,
                sample.T_DHT,
                sample.T_BMP,
                sample.Pressure,
                sample.DateTime);
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

            measurementsWriter.Dispose();
            rejectsWriter.Dispose();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
