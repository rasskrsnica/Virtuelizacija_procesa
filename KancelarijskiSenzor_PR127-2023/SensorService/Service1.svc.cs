using SensorCommon;
using SensorCommon.Contracts;
using SensorCommon.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using SensorCommon;
using SensorCommon.Models;

namespace SensorService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : ISensorService
    {
        public string StartSession()
        {
            return "ACK";
        }

        public string PushSample(SensorSample sample)
        {
            return "ACK";
        }

        public string EndSession()
        {
            return "DONE";
        }
    }
}
