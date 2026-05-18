using SensorCommon.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

namespace SensorService
{
    internal class SensorAnalytics
    {
        public delegate void SensorWarningHandler(object sender, EventInfo e);

        public event SensorWarningHandler VolumeSpikeDetected;
        public event SensorWarningHandler TemperatureDhtSpikeDetected;
        public event SensorWarningHandler TemperatureBmpSpikeDetected;
        public event SensorWarningHandler RunningMeanDeviationDetected;

        private readonly double volumeThreshold;
        private readonly double dhtThreshold;
        private readonly double bmpThreshold;
        private readonly double runningMeanPercent;

        public SensorAnalytics()
        {
            volumeThreshold = ReadDouble("V_threshold", 10);
            dhtThreshold = ReadDouble("T_dht_threshold", 1);
            bmpThreshold = ReadDouble("T_bmp_threshold", 1);
            runningMeanPercent = ReadDouble("MeanDeviationPercent", 25);
        }

        public List<EventInfo> Analyze(SessionState state, SensorSample current)
        {
            List<EventInfo> events = new List<EventInfo>();
            Subscribe(events);

            if (state.PreviousSample != null)
            {
                double deltaVolume = current.Volume - state.PreviousSample.Volume;
                if (Math.Abs(deltaVolume) > volumeThreshold)
                {
                    OnVolumeSpikeDetected(new EventInfo("OnWarningRaised", "VolumeSpike: Delta V = " + Format(deltaVolume) + ", smer: " + Direction(deltaVolume)));
                }

                double deltaDht = current.T_DHT - state.PreviousSample.T_DHT;
                if (Math.Abs(deltaDht) > dhtThreshold)
                {
                    OnTemperatureDhtSpikeDetected(new EventInfo("OnWarningRaised", "TemperatureSpikeDHT: Delta Tdht = " + Format(deltaDht) + ", smer: " + Direction(deltaDht)));
                }

                double deltaBmp = current.T_BMP - state.PreviousSample.T_BMP;
                if (Math.Abs(deltaBmp) > bmpThreshold)
                {
                    OnTemperatureBmpSpikeDetected(new EventInfo("OnWarningRaised", "TemperatureSpikeBMP: Delta Tbmp = " + Format(deltaBmp) + ", smer: " + Direction(deltaBmp)));
                }
            }

            if (state.SampleCount > 0)
            {
                double mean = state.VolumeSum / state.SampleCount;
                double lowerLimit = mean * (1 - runningMeanPercent / 100.0);
                double upperLimit = mean * (1 + runningMeanPercent / 100.0);

                if (current.Volume < lowerLimit || current.Volume > upperLimit)
                {
                    string direction = current.Volume > upperLimit ? "iznad ocekivane vrednosti" : "ispod ocekivane vrednosti";
                    OnRunningMeanDeviationDetected(new EventInfo("OnWarningRaised", "OutOfBandWarning: V = " + Format(current.Volume) + ", srednja vrednost = " + Format(mean) + ", smer: " + direction));
                }
            }

            Unsubscribe();
            return events;
        }

        private void Subscribe(List<EventInfo> events)
        {
            VolumeSpikeDetected += (sender, e) => events.Add(e);
            TemperatureDhtSpikeDetected += (sender, e) => events.Add(e);
            TemperatureBmpSpikeDetected += (sender, e) => events.Add(e);
            RunningMeanDeviationDetected += (sender, e) => events.Add(e);
        }

        private void Unsubscribe()
        {
            VolumeSpikeDetected = null;
            TemperatureDhtSpikeDetected = null;
            TemperatureBmpSpikeDetected = null;
            RunningMeanDeviationDetected = null;
        }

        private static string Direction(double delta)
        {
            return delta < 0 ? "ispod ocekivanog" : "iznad ocekivanog";
        }

        private static string Format(double value)
        {
            return value.ToString("F4", CultureInfo.InvariantCulture);
        }

        private static double ReadDouble(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            double parsed;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        private void OnVolumeSpikeDetected(EventInfo info)
        {
            if (VolumeSpikeDetected != null) VolumeSpikeDetected(this, info);
        }

        private void OnTemperatureDhtSpikeDetected(EventInfo info)
        {
            if (TemperatureDhtSpikeDetected != null) TemperatureDhtSpikeDetected(this, info);
        }

        private void OnTemperatureBmpSpikeDetected(EventInfo info)
        {
            if (TemperatureBmpSpikeDetected != null) TemperatureBmpSpikeDetected(this, info);
        }

        private void OnRunningMeanDeviationDetected(EventInfo info)
        {
            if (RunningMeanDeviationDetected != null) RunningMeanDeviationDetected(this, info);
        }
    }
}
