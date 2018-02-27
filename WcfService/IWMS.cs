using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfService
{
    [ServiceContract]
    public interface IWMS
    {
        [OperationContract]
        string MFCS_Submit(string wmsID, string item, string instruction, string arguments);
    }

}
