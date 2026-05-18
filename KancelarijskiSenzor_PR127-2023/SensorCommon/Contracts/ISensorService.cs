using SensorCommon.Models;
using System.ServiceModel;

namespace SensorCommon.Contracts
{
    [ServiceContract]
    public interface ISensorService
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        SensorResponse StartSession(SessionMeta meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        SensorResponse PushSample(SensorSample sample);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        SensorResponse EndSession();
    }
}
