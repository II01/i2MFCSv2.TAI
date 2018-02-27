using Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Warehouse.WCF;
using Warehouse.WMS;

namespace WcfService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class WMS : IWMS
    {
        public string MFCS_Submit(string wmsID, string item, string instruction, string arguments)
        {
            try
            {
                ServiceHostBase sh = OperationContext.Current.Host;
                if (!(sh is WarehouseServiceHost))
                    throw new WCFServiceException("Host is wrong type.");
                var warehouse = (sh as WarehouseServiceHost).Warehouse;
                try
                {
                    warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                       string.Format("MFCS_Submit called ({0}|{1}|{2}|{3})", wmsID, item, instruction, arguments));

                    string retVal = warehouse.WMS.OnRequest(wmsID, item, instruction, arguments);

                    warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.WMS,
                                       string.Format("MFCS_Submit returned ({0}|{1}|{2}|{3}): {4}.", wmsID, item, instruction, arguments, retVal));

                    return retVal;
                }
                catch (Exception e)
                {
                    warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception,
                                       string.Format("{0}.{1}: {2}, MFCS_Submit exception ({3}|{4}|{5}|{6})",
                                       this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message, wmsID, item, instruction, arguments));
                    return "FALSE; Exception: MFCS_Submit.";
                }

            }
            catch
            {
                return "FALSE; Exception: Warehouse host not correctly initialized.";
            }
        }
    }



}
