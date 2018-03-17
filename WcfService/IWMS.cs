using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WcfService.DTO;

namespace WcfService
{
    [ServiceContract]
    public interface IWMS
    {
        [OperationContract(IsOneWay =true)]
        void MFCS_Submit(IEnumerable<DTOCommand> cmds);
    }

}
