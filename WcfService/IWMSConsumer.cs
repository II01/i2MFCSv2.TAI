using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfService
{
    [ServiceContract]
    public interface IWMSConsumer
    {
        [OperationContract]
        string WMS_Status(string wmsID, string item, string status, string info);
    }

}
