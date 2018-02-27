using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Telegrams;
using Database;

namespace TransportUnits
{

    public class SimpleTransportException : Exception
    {
        public SimpleTransportException(string s) : base(s)
        { }
    }

    // Place 
    public class SimpleTransport : BasicTransport
    {

        [XmlIgnore]
        public TelegramTransportTO Command_Status { get; set; }

        [XmlIgnore]
        public TelegramTransportStatus PLC_Status { get; set; }

        public SimpleTransport()
        {
        }

        public override bool Automatic()
        {
            if (PLC_Status == null)
                return false;
            else
                return PLC_Status.Status[TelegramTransportStatus.STATUS_AUTOMATIC];
        }


        public override void Initialize()
        {
            Communicator.DispatchRcv.Add(new DispatchNode { DispatchTerm = (t) => t.Sender == PLC_ID && (t is TelegramCraneStatus), DispatchTo = OnTelegramTransportStatus });
            Communicator.DispatchRcv.Add(new DispatchNode { DispatchTerm = (t) => t.Sender == PLC_ID && (t is TelegramCraneTO), DispatchTo = OnTelegramTransportTO });
            InitializePlace();
        }

        public void OnTelegramTransportStatus(Telegram t)
        {
            PLC_Status = t as TelegramTransportStatus;
            // do code here
        }

        public void OnTelegramTransportTO(Telegram t)
        {
            Command_Status = t as TelegramTransportTO;
            // do code here
        }

    }


}
