using Database;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Warehouse.ConveyorUnits;
using Warehouse.Model;
using Warehouse.WCF;

namespace WcfService
{


    public class WCFServiceException : Exception
    {
        public WCFServiceException(string s) : base(s) { }
    }


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class MFCSService : INotifyUI
    {
        public BasicWarehouse Warehouse { get; set; }
        private object _lock { get; set; }
        private static Dictionary<string, List<Tuple<Action<ConveyorBasicInfo>, ConveyorBasic>>> _callDict = new Dictionary<string, List<Tuple<Action<ConveyorBasicInfo>, ConveyorBasic>>>();
        private static Dictionary<string, List<Tuple<Action<ConveyorBasicInfo>, Segment>>> _callDictSegment = new Dictionary<string, List<Tuple<Action<ConveyorBasicInfo>, Segment>>>();
        private static Dictionary<string, Action<DateTime, Event.EnumSeverity, Event.EnumType, string>> _callDictEvent = new Dictionary<string, Action<DateTime, Event.EnumSeverity, Event.EnumType, string>>();
        private static Dictionary<string, Action<bool, bool, bool>> _callDictSystemMode = new Dictionary<string, Action<bool, bool, bool>>();

        public MFCSService()
        {
            Warehouse = null;
            _lock = new object();
        }

        public void UIRegister(string client)
        {
            lock (_lock)
            {
                try
                {
                    ServiceHostBase sh = OperationContext.Current.Host;
                    if (!(sh is WarehouseServiceHost))
                        throw new WCFServiceException("Host is wrong type.");
                    var Warehouse = (sh as WarehouseServiceHost).Warehouse;

                    if (_callDict.ContainsKey(client))
                    {
                        _callDict[client].ForEach(prop => { try { prop.Item2.NotifyVM.Remove(prop.Item1); } catch { } });
                        _callDict[client].Clear();
                    }
                    else
                        _callDict.Add(client, new List<Tuple<Action<ConveyorBasicInfo>, ConveyorBasic>>());

                    if (_callDictSegment.ContainsKey(client))
                    {
                        _callDictSegment[client].ForEach( prop => { try {prop.Item2.NotifyVM.Remove(prop.Item1); } catch { } });
                        _callDictSegment[client].Clear();
                    }
                    else
                        _callDictSegment.Add(client, new List<Tuple<Action<ConveyorBasicInfo>, Segment>>());

                    var d = new Action<ConveyorBasicInfo>(OperationContext.Current.GetCallbackChannel<INotifyUICallback>().UIConveyorBasicUINotify);
                    Warehouse.ConveyorList.ForEach(prop=> { prop.NotifyVM.Add(d); _callDict[client].Add(new Tuple<Action<ConveyorBasicInfo>, ConveyorBasic>(d,prop));});
                    Warehouse.CraneList.ForEach(prop => { prop.NotifyVM.Add(d); _callDict[client].Add(new Tuple<Action<ConveyorBasicInfo>, ConveyorBasic>(d, prop)); });
                    Warehouse.SegmentList.ForEach(prop => { prop.NotifyVM.Add(d); _callDictSegment[client].Add(new Tuple<Action<ConveyorBasicInfo>, Segment>(d, prop)); });

                    if (_callDictEvent.ContainsKey(client))
                    {
                        try
                        {
                            Warehouse.OnNewEvent.Remove(_callDictEvent[client]);
                        }
                        catch { }
                        _callDictEvent.Remove(client);
                    }
                    var d1 = new Action<DateTime, Event.EnumSeverity, Event.EnumType, string>(OperationContext.Current.GetCallbackChannel<INotifyUICallback>().UIAddEvent);
                    _callDictEvent.Add(client, d1);
                    Warehouse.OnNewEvent.Add(d1);

                    if (_callDictSystemMode.ContainsKey(client))
                    {
                        try
                        {
                            Warehouse.SteeringCommands.SteeringNotify.Remove(_callDictSystemMode[client]);
                        }
                        catch { }
                        _callDictSystemMode.Remove(client);
                    }

                    var d2 = new Action<bool, bool, bool>(OperationContext.Current.GetCallbackChannel<INotifyUICallback>().SystemMode);
                    _callDictSystemMode.Add(client, d2);
                    Warehouse.SteeringCommands.SteeringNotify.Add(d2);

                    Warehouse.SegmentList.ForEach(prop => prop.UINotified = false);
                    // this call is not possible because it is not reentrant!
                    // Warehouse?.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("MFCService.UIRegister({0})", client));
                }
                catch (Exception ex)
                {
                    Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.UIRegister({0}) failed. Reason:{1}", client, ex.Message));
//                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
                }
            }
        }

        public void UIUnRegister(string client)
        {
            lock (_lock)
            {
                // nothing to do inside at the moment. 
            }
        }

        public void SetMode(bool remote, bool automatic, bool run)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var Warehouse = (sh as WarehouseServiceHost).Warehouse;
                Warehouse.SteeringCommands.RemoteMode = remote;
                Warehouse.SteeringCommands.AutomaticMode = automatic;
                Warehouse.SteeringCommands.Run = run;
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.SetMode({0},{1},{2}) failed. Reason:{3}", remote, automatic, run, ex.Message));
                //                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
            }

        }

        public void Reset(string segment)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var Warehouse = (sh as WarehouseServiceHost).Warehouse;
                Warehouse.Segment[segment].Reset(0);
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.Reset({0}) failed. Reason:{1}", segment, ex.Message));
                //                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
            }
        }

        public void Info(string segment)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var Warehouse = (sh as WarehouseServiceHost).Warehouse;
                Warehouse.Segment[segment].Info(0);
                // Action<int> action = new Action<int>(Warehouse.Segment[segment].Info)
               // del.BeginInvoke(0, null, null); // blind invoke
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.Info({0}) failed. Reason:{1}", segment, ex.Message));
                //                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
            }
        }

        public void AutoOn(string segment)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var Warehouse = (sh as WarehouseServiceHost).Warehouse;
                Warehouse.Segment[segment].AutomaticOn(0);
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.AutomaticOn({0}) failed. Reason:{1}", segment, ex.Message));
                //                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
            }
        }

        public void AutoOff(string segment)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var Warehouse = (sh as WarehouseServiceHost).Warehouse;
                Warehouse.Segment[segment].AutomaticOff(0);
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.AutomaticOff({0}) failed. Reason:{1}", segment, ex.Message));
                //                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
            }
        }

        public void LongTermBlockOn(string segment)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var Warehouse = (sh as WarehouseServiceHost).Warehouse;
                Warehouse.Segment[segment].LongTermBlockOn(0);
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.LongTermBlockOn({0}) failed. Reason:{1}", segment, ex.Message));
                //                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
            }
        }
        public void LongTermBlockOff(string segment)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var Warehouse = (sh as WarehouseServiceHost).Warehouse;
                Warehouse.Segment[segment].LongTermBlockOff(0);
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.LongTermBlockOff({0}) failed. Reason:{1}", segment, ex.Message));
                //                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
            }
        }

        public void SetClock(string segment)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var Warehouse = (sh as WarehouseServiceHost).Warehouse;
                Warehouse.Segment[segment].SetClock(0);
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.SetClock({0}) failed. Reason:{1}", segment, ex.Message));
                //                    throw new WCFServiceException(String.Format("MFCService.UIRegisterComWamunicator({0}) failed. Reason:{1}", client, ex.Message));
            }
        }

        public void RebuildRoutes(bool ignoreBlocked)
        {
            try
            {
                var Warehouse = (OperationContext.Current.Host as WarehouseServiceHost).Warehouse;
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, "Rebuilding routes...");
                Warehouse?.BuildRoutes(ignoreBlocked);
            }
            catch(Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.RebuildRoutes() failed. Reason:{0}", ex.Message));
            }
        }
        public bool RouteExists(string source, string target, bool isSimpleCommand)
        {
            try
            {
                var Warehouse = (OperationContext.Current.Host as WarehouseServiceHost).Warehouse;
                return Warehouse.RouteExists(source, target, isSimpleCommand);
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.RouteExists() failed. Reason:{0}", ex.Message));
                return false;
            }
        }

        public void PlaceIDChanged(PlaceID place)
        {
            try
            {
                var Warehouse = (OperationContext.Current.Host as WarehouseServiceHost).Warehouse;
                Warehouse.WMS.SendPlaceInfo(place);
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception, String.Format("MFCService.PlaceIDChanged() failed. Reason:{0}", ex.Message));
            }
        }

        public void ClearRamp(string ramp)
        {
        }    
    }
}
