using SimpleLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Warehouse.WCF;


namespace WcfService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class WMSConsumer : IWMSConsumer
    {
        private static object _lock = new object();

        public string WMS_Status(string wmsID, string item, string status, string info)
        {
            try
            {
                lock(_lock)
                {
                    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
//                    Log log = new Log(ConfigurationManager.AppSettings["logFile"], Convert.ToBoolean(ConfigurationManager.AppSettings["logToFile"]));
                    Log.AddLog(Log.Severity.EVENT, "WMS", "Consumer", string.Format("|{0}|{1}|{2}|{3}|", wmsID, item, status, info));
                    return "True";
                }
            }
            catch(Exception e)
            {
                return "False; " + e.Message;
            }
        }
    }



}
