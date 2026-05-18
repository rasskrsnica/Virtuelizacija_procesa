using System.Runtime.Serialization;

namespace SensorCommon.Models
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember] public string FieldName { get; set; }
        [DataMember] public string Message { get; set; }
    }
}
