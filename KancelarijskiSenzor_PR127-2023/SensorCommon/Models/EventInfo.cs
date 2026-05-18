using System.Runtime.Serialization;

namespace SensorCommon.Models
{
    [DataContract]
    public class EventInfo
    {
        public EventInfo()
        {
        }

        public EventInfo(string name, string message)
        {
            Name = name;
            Message = message;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}
