using System.ServiceModel;

namespace SensorService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    public class Service1 : Services.SensorService
    {
    }
}
