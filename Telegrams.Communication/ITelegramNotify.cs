using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Telegrams;

namespace WcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract(CallbackContract =typeof(ITelegramNotifyCallback))]
    public interface ITelegramNotify
    {
        [OperationContract]
        void RegisterCommunicator(string communicatorName);
        [OperationContract]
        void UnRegisterCommunicator(string communicatorName);
        [OperationContract]
        void TelegramNotifyRcv(Telegram tel, string communicatorName);

        [OperationContract]
        void TelegramNotifySend(Telegram tel, string communicatorName);
    }

    public interface ITelegramNotifyCallback
    {
        [OperationContract]
        void TelegramSend(Telegram tel);
    }

}
