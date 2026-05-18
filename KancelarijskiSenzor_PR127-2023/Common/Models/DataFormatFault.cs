using System.Runtime.Serialization;

namespace SensorCommon.Models
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember] public string FieldName { get; set; }
        [DataMember] public string Message { get; set; }
    }
}
