using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfService
{
    [ServiceContract(CallbackContract = typeof(INotifyUIEventCallback))]
    public interface INotifyUIEvent
    {

        [OperationContract]
        void UIRegisterEventLog();

        [OperationContract]
        void UIUnRegisterEventLog();
    }


    public interface INotifyUIEventCallback
    {
        [OperationContract]
        void UIAddEvent(DateTime time, Database.EnumEventSeverity s, Database.EnumEventType t, string text);
    }

}
