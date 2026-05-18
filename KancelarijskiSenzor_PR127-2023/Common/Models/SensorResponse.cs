using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SensorCommon.Models
{
    [DataContract]
    public class SensorResponse
    {
        public SensorResponse()
        {
            Events = new List<EventInfo>();
        }

        [DataMember] public bool Ack { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public List<EventInfo> Events { get; set; }

        public static SensorResponse Ok(string status, string message)
        {
            return new SensorResponse { Ack = true, Status = status, Message = message };
        }

        public static SensorResponse Fail(string status, string message)
        {
            return new SensorResponse { Ack = false, Status = status, Message = message };
        }
    }
}
