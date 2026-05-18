using System;
using System.Runtime.Serialization;

namespace SensorCommon.Models
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember] public double Volume { get; set; }
        [DataMember] public double T_DHT { get; set; }
        [DataMember] public double T_BMP { get; set; }
        [DataMember] public double Pressure { get; set; }
        [DataMember] public DateTime DateTime { get; set; }
    }
}
