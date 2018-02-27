using System;
using Database;
using Warehouse.ConveyorUnits;
using Warehouse.Model;
using System.ServiceModel;
using System.Xml.Serialization;
using Warehouse.WCF;
using SimpleLog;
using WCFWarehouse.MFCSService;

namespace WCFClients
{

    //[CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class WCFUIClient : WCFBasicClient, INotifyUICallback
    {
        [XmlIgnore]
        public NotifyUIClient NotifyUIClient { get; set; }


        public void UIAddEvent(DateTime time, Event.EnumSeverity s, Event.EnumType t, string text)
        {
            try
            {
                Warehouse?.OnNewEvent.ForEach(prop => prop.Invoke(time, s, t, text));
                // Warehouse?.AddEvent((Event.EnumSeverity) s, (Event.EnumType) t, text);
            }
            catch (Exception ex)
            {
                Log.AddLog(Log.Severity.EXCEPTION, "WCFClient", "UIAddEvent", ex.Message);
            }
        }

        public void UIConveyorBasicUINotify(ConveyorBasicInfo c)
        {
            try
            {
                if (Warehouse.Crane.ContainsKey(c.Name))
                    Warehouse.Crane[c.Name].CallNotifyVM(c);
                else if (Warehouse.Conveyor.ContainsKey(c.Name))
                    Warehouse.Conveyor[c.Name].CallNotifyVM(c);
                else if (Warehouse.Segment.ContainsKey(c.Name))
                    Warehouse.Segment[c.Name].CallNotifyVM(c);
            }
            catch (Exception ex)
            {
                if (c == null)
                    Log.AddLog(Log.Severity.EXCEPTION, "WCFClient", String.Format("UIConveyorBasicUINotify {0}", "c==null"), ex.Message);
                else
                    Log.AddLog(Log.Severity.EXCEPTION, "WCFClient", String.Format("UIConveyorBasicUINotify {0}", c.Name ?? "null"), ex.Message);
            }
        }

        public override void Initialize(BasicWarehouse w)
        {
            try
            {
                base.Initialize(w);
                // set notifications
                InstanceContext ic = new InstanceContext(this);
                NotifyUIClient = new NotifyUIClient(ic);

//                NotifyUIClient.ClientCredentials.Windows.ClientCredential.UserName = "username";
//                NotifyUIClient.ClientCredentials.Windows.ClientCredential.Password = "password";
//                NotifyUIClient.ClientCredentials.Windows.ClientCredential.Domain = "domainname";

                NotifyUIClient.UIRegister(Warehouse.Name);  // change this to App.Settings ClientName 
            }
            catch (Exception ex)
            {
                Warehouse?.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Program, String.Format("WCFClient.Initialize failed. Reason:{0}", ex.Message));
                throw new WCFException(String.Format("WCFClient.Initialite failed. Reason:{0}", ex.Message));
            }
        }

        public override void Dispose()
        {
            try
            {
                base.Dispose();
                NotifyUIClient?.Close();
            }
            catch { }
        }

        public void SystemMode(bool remote, bool automatic, bool run)
        {
            Warehouse.SteeringCommands.RemoteMode = remote;
            Warehouse.SteeringCommands.AutomaticMode = automatic;
            Warehouse.SteeringCommands.Run = run;
            // TODO Uroš - dodaj klic k sebi 
        }
    }
}
