using System.Runtime.Serialization;

namespace SensorCommon.Models
{
    [DataContract]
    public enum TransferStatus
    {
        [EnumMember]
        Success = 0,
        [EnumMember]
        Warning = 1,
        [EnumMember]
        Failed = 2,
        [EnumMember]
        InProgress = 3,
        [EnumMember]
        Completed = 4
    }
}
