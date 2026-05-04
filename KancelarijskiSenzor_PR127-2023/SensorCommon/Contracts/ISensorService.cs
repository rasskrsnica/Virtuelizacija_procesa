using SensorCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SensorCommon.Contracts
{
    [ServiceContract]
    public interface ISensorService
    {
        [OperationContract]
        string StartSession();

        [OperationContract]
        string PushSample(SensorSample sample);

        [OperationContract]
        string EndSession();
    }
}
