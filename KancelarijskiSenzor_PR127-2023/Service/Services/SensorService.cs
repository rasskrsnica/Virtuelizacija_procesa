using SensorCommon.Contracts;
using SensorCommon.Models;
using System;
using System.Globalization;
using System.ServiceModel;

namespace SensorService.Services
{
    public delegate void TransferEventHandler(object sender, EventInfo e);
    public delegate void SampleEventHandler(object sender, SensorSample sample);
    public delegate void WarningEventHandler(object sender, EventInfo e);

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    public class SensorService : ISensorService, IDisposable
    {
        public event TransferEventHandler OnTransferStarted;
        public event SampleEventHandler OnSampleReceived;
        public event TransferEventHandler OnTransferCompleted;
        public event WarningEventHandler OnWarningRaised;

        private SessionState state;
        private SensorStorage storage;
        private readonly SensorAnalytics analytics = new SensorAnalytics();
        private bool disposed;

        public SensorService()
        {
            OnTransferStarted += LogTransferEvent;
            OnSampleReceived += LogSampleEvent;
            OnTransferCompleted += LogTransferEvent;
            OnWarningRaised += LogWarningEvent;
        }

        public SensorResponse StartSession(SessionMeta meta)
        {
            ValidateMeta(meta);
            CloseOpenSession();

            storage = new SensorStorage();
            string sessionId = storage.CreateSession(meta);
            state = new SessionState { Header = meta };

            SensorResponse result = SensorResponse.Ok("IN_PROGRESS", sessionId);
            EventInfo started = new EventInfo("OnTransferStarted", "prenos u toku... session=" + sessionId);
            AddEvent(result, started);
            RaiseTransferStarted(started);
            Console.WriteLine("prenos u toku...");
            Console.WriteLine("Start session: " + sessionId + " V=" + meta.Volume.ToString(CultureInfo.InvariantCulture));
            return result;
        }

        public SensorResponse PushSample(SensorSample sample)
        {
            EnsureSession();

            try
            {
                ValidateSample(sample);
                SensorResponse result = SensorResponse.Ok("IN_PROGRESS", "ACK PushSample");

                result.Events.AddRange(analytics.Analyze(state, sample));
                EventInfo received = new EventInfo("OnSampleReceived", "Primljen red " + (state.SampleCount + 1));
                result.Events.Insert(0, received);

                storage.SaveMeasurement(sample);
                state.VolumeSum += sample.Volume;
                state.SampleCount++;
                state.PreviousSample = sample;

                RaiseSampleReceived(sample);
                Console.WriteLine("Primljen red " + state.SampleCount + ", V=" + sample.Volume.ToString(CultureInfo.InvariantCulture) + ", T_DHT=" + sample.T_DHT.ToString(CultureInfo.InvariantCulture) + ", T_BMP=" + sample.T_BMP.ToString(CultureInfo.InvariantCulture));
                foreach (EventInfo eventInfo in result.Events)
                {
                    storage.SaveEvent(eventInfo);
                    if (eventInfo.Name == "OnWarningRaised")
                    {
                        RaiseWarning(eventInfo);
                    }

                    Console.WriteLine("[EVENT] " + eventInfo.Name + " - " + eventInfo.Message);
                }

                return result;
            }
            catch (FaultException<ValidationFault> ex)
            {
                SaveRejected(sample, ex.Detail.Message);
                throw;
            }
            catch (FaultException<DataFormatFault> ex)
            {
                SaveRejected(sample, ex.Detail.Message);
                throw;
            }
            catch (Exception ex)
            {
                CloseOpenSession();
                throw new FaultException<ValidationFault>(
                    new ValidationFault { FieldName = "Transfer", Message = "Prenos je prekinut i resursi su zatvoreni: " + ex.Message },
                    new FaultReason("Transfer interrupted"));
            }
        }

        public SensorResponse EndSession()
        {
            EnsureSession();

            SensorResponse result = SensorResponse.Ok("COMPLETED", "ACK EndSession");
            EventInfo completed = new EventInfo("OnTransferCompleted", "zavrsen prenos");
            AddEvent(result, completed);
            storage.SaveSummary(state);
            RaiseTransferCompleted(completed);
            Console.WriteLine("zavrsen prenos");
            Console.WriteLine("End session. Primljeno=" + state.SampleCount + ", odbaceno=" + state.RejectedCount);

            CloseOpenSession();
            return result;
        }

        private void SaveRejected(SensorSample sample, string reason)
        {
            if (sample != null && storage != null && state != null)
            {
                storage.SaveRejected(reason, sample);
                state.RejectedCount++;
            }
        }

        private void AddEvent(SensorResponse result, EventInfo eventInfo)
        {
            result.Events.Add(eventInfo);
            if (storage != null)
            {
                storage.SaveEvent(eventInfo);
            }
        }

        private static void ValidateMeta(SessionMeta meta)
        {
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault { FieldName = "meta", Message = "Meta-zaglavlje je obavezno." }, new FaultReason("Missing metadata"));
            }

            ValidateNumbers(meta.Volume, meta.T_DHT, meta.T_BMP, meta.Pressure, meta.DateTime);
        }

        private static void ValidateSample(SensorSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault { FieldName = "sample", Message = "Uzorak je obavezan." }, new FaultReason("Missing sample"));
            }

            ValidateNumbers(sample.Volume, sample.T_DHT, sample.T_BMP, sample.Pressure, sample.DateTime);
        }

        private static void ValidateNumbers(double volume, double tDht, double tBmp, double pressure, DateTime dateTime)
        {
            if (double.IsNaN(volume) || double.IsInfinity(volume))
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault { FieldName = "Volume", Message = "Volume mora biti broj." }, new FaultReason("Invalid volume"));
            }

            if (volume < 0 || volume > 5000)
            {
                throw new FaultException<ValidationFault>(new ValidationFault { FieldName = "Volume", Message = "Volume mora biti u opsegu 0-5000 mV." }, new FaultReason("Invalid volume range"));
            }

            if (double.IsNaN(tDht) || double.IsInfinity(tDht))
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault { FieldName = "T_DHT", Message = "T_DHT mora biti broj." }, new FaultReason("Invalid DHT temperature"));
            }

            if (tDht < -50 || tDht > 80)
            {
                throw new FaultException<ValidationFault>(new ValidationFault { FieldName = "T_DHT", Message = "T_DHT mora biti u opsegu -50 do 80 stepeni Celzijusa." }, new FaultReason("Invalid DHT temperature range"));
            }

            if (double.IsNaN(tBmp) || double.IsInfinity(tBmp))
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault { FieldName = "T_BMP", Message = "T_BMP mora biti broj." }, new FaultReason("Invalid BMP temperature"));
            }

            if (tBmp < -50 || tBmp > 80)
            {
                throw new FaultException<ValidationFault>(new ValidationFault { FieldName = "T_BMP", Message = "T_BMP mora biti u opsegu -50 do 80 stepeni Celzijusa." }, new FaultReason("Invalid BMP temperature range"));
            }

            if (double.IsNaN(pressure) || double.IsInfinity(pressure))
            {
                throw new FaultException<DataFormatFault>(new DataFormatFault { FieldName = "Pressure", Message = "Pressure mora biti broj." }, new FaultReason("Invalid pressure format"));
            }

            if (pressure <= 0)
            {
                throw new FaultException<ValidationFault>(new ValidationFault { FieldName = "Pressure", Message = "Pressure mora biti veci od 0." }, new FaultReason("Invalid pressure"));
            }

            if (pressure < 300 || pressure > 1200)
            {
                throw new FaultException<ValidationFault>(new ValidationFault { FieldName = "Pressure", Message = "Pressure mora biti u opsegu 300-1200 hPa." }, new FaultReason("Invalid pressure range"));
            }

            if (dateTime == default(DateTime))
            {
                throw new FaultException<ValidationFault>(new ValidationFault { FieldName = "DateTime", Message = "DateTime je obavezan." }, new FaultReason("Invalid DateTime"));
            }
        }

        private void EnsureSession()
        {
            if (state == null || storage == null)
            {
                throw new FaultException<ValidationFault>(new ValidationFault { FieldName = "Session", Message = "Sesija nije pokrenuta." }, new FaultReason("Session not started"));
            }
        }

        private void CloseOpenSession()
        {
            if (storage != null)
            {
                storage.Dispose();
                storage = null;
            }

            state = null;
        }

        private void RaiseTransferStarted(EventInfo e)
        {
            if (OnTransferStarted != null) OnTransferStarted(this, e);
        }

        private void RaiseSampleReceived(SensorSample sample)
        {
            if (OnSampleReceived != null) OnSampleReceived(this, sample);
        }

        private void RaiseTransferCompleted(EventInfo e)
        {
            if (OnTransferCompleted != null) OnTransferCompleted(this, e);
        }

        private void RaiseWarning(EventInfo e)
        {
            if (OnWarningRaised != null) OnWarningRaised(this, e);
        }

        private static void LogTransferEvent(object sender, EventInfo e)
        {
            Console.WriteLine("[PRETPLATA] " + e.Name + " - " + e.Message);
        }

        private static void LogSampleEvent(object sender, SensorSample sample)
        {
            Console.WriteLine("[PRETPLATA] OnSampleReceived - V=" + sample.Volume.ToString(CultureInfo.InvariantCulture) + ", T_DHT=" + sample.T_DHT.ToString(CultureInfo.InvariantCulture) + ", T_BMP=" + sample.T_BMP.ToString(CultureInfo.InvariantCulture));
        }

        private static void LogWarningEvent(object sender, EventInfo e)
        {
            Console.WriteLine("[PRETPLATA] " + e.Name + " - " + e.Message);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                CloseOpenSession();
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
