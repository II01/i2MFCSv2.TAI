using Database;
using System;
using System.ServiceModel;
using Warehouse.ConveyorUnits;



namespace WcfService
{
    [ServiceContract(CallbackContract = typeof(INotifyUICallback))]
    public interface INotifyUI
    {
        [OperationContract]
        void UIRegister(string communicatorName);
        [OperationContract]
        void UIUnRegister(string communicatorName);
        [OperationContract(IsOneWay =true)]
        void SetMode(bool remote, bool automatic, bool run);
        [OperationContract(IsOneWay =true)]
        void Reset(string segment);
        [OperationContract(IsOneWay = true)]
        void Info(string segment);
        [OperationContract(IsOneWay =true)]
        void AutoOn(string segment);
        [OperationContract(IsOneWay =true)]
        void AutoOff(string segment);
        [OperationContract(IsOneWay = true)]
        void LongTermBlockOn(string segment);
        [OperationContract(IsOneWay = true)]
        void RebuildRoutes(bool ignoreBlocked);
        [OperationContract(IsOneWay = false)]
        bool RouteExists(string source, string target, bool isSimpleCommand);
        [OperationContract(IsOneWay = true)]
        void LongTermBlockOff(string segment);
        [OperationContract(IsOneWay = true)]
        void SetClock(string segment);

        [OperationContract(IsOneWay = true)]
        void PlaceIDChanged(PlaceID place);
    }


    public interface INotifyUICallback
    {
        [OperationContract(IsOneWay =true)]
        void UIConveyorBasicUINotify(ConveyorBasicInfo c);
        [OperationContract(IsOneWay = true)]
        void UIAddEvent(DateTime time, Event.EnumSeverity s, Event.EnumType t, string text);
        [OperationContract(IsOneWay = true)]
        void SystemMode(bool remote, bool automatic, bool run);
    }

}
