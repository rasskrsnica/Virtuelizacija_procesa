using SensorCommon.Models;

namespace SensorService
{
    internal class SessionState
    {
        public SessionMeta Header { get; set; }
        public SensorSample PreviousSample { get; set; }
        public double VolumeSum { get; set; }
        public int SampleCount { get; set; }
        public int RejectedCount { get; set; }
    }
}
