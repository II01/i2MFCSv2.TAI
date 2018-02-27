using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Telegrams
{
    public class ServiceCommunication : Communication
    {
        [XmlAttribute]
        public string NotifyEndPointName { get; set; }
//        private WCFServiceTelegramNotify _notifyProxy {get;set;}

        public ServiceCommunication() : base()
        {

        }

        public override void NotifyRcv(Telegram tel)
        {
        }

        public override void NotifySend(Telegram tel)
        {
        }
    }
}
