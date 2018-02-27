using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Telegrams.WcfService;

namespace Telegrams.Console
{
    class Program
    {
 
        static void Main(string[] args)
        {
            using (var rcvHost = new ServiceHost(typeof(WCF_RcvTelProxy)))
            using (var sendHost = new ServiceHost(typeof(WCF_SendTelProxy)))
            {
                rcvHost.Open();
                sendHost.Open();
                System.Console.WriteLine($"WCF Service started...\n Press ENTER to stop.");
                System.Console.ReadLine();
            }
        }
    }
}
