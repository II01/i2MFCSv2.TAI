using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Telegrams.WcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract(SessionMode=SessionMode.Required)]
    public interface IWCF_SendTelProxy
    {
        [OperationContract]
        void Init(string name, string addr, int SendPort, int timeoutSec, string version);
        [OperationContract]
        Task<Telegram> SendAsync(Telegram t);
    }

    [ServiceContract(SessionMode=SessionMode.Required)]
    public interface IWCF_RcvTelProxy
    {
        [OperationContract]
        void Init(string name, string addr, int SendPort, int timeoutSec, string version);
        [OperationContract]
        Task<Telegram> ReadAsync();
    }


}
