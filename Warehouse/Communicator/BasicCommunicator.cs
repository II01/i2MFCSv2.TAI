using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFCS.Communication
{
    using SimpleLog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Xml.Serialization;
    using Telegrams;
    using Warehouse.ConveyorUnits;
    using Warehouse.Model;


    /*    public class DispatchNode
        {
            public Predicate<Telegram> DispatchTerm { get; set; } // lahko FUNC<in T1,in T2,out T3>
            public Action<Telegram> DispatchTo { get; set; }
        }
    */

    public static class CommunicatorExtensions
    {
        public static void RunCommunication(this IEnumerable<BasicCommunicator> list, CancellationToken ct)
        {
            var task = (from a in list select a.SendThreading(ct)).Union
                        (from b in list select b.RcvThreading(ct));
            Task.Run(async () => await Task.WhenAll(task).ConfigureAwait(false));
        }

    }

    public abstract class BasicCommunicator
    {
        [XmlIgnore]
        public BasicWarehouse Warehouse { get; set; }
        protected Dictionary<short, Crane> CraneByID { get; set; }
        protected Dictionary<short, Conveyor> ConveyorByID { get; set; }
        protected Dictionary<short, Segment> SegmentByID { get; set; }


        protected string _version;
        [XmlIgnore]
        public Action<Telegram> OnReceive { get; set; }
        [XmlIgnore]
        public Action<Telegram> OnSend { get; set; }
/*        [XmlIgnore]
        public List<DispatchNode> DispatchRcv { get; private set; }
        [XmlIgnore]
        public List<DispatchNode> DispatchSend { get; private set; }
*/
        [XmlIgnore]
        public Action OnRefresh { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        // TimeSpan can't be Xml Serialized

        abstract public void AddSendTelegram(Telegram t);

        public int RefreshTimeSeconds
        {
            get { return RefreshTime.Seconds; }
            set { RefreshTime = TimeSpan.FromSeconds(value); }
        }

        protected TimeSpan RefreshTime { get; set; }
        public int SendTimeOutSeconds
        {
            get { return SendTimeOut.Seconds; }
            set { SendTimeOut = TimeSpan.FromSeconds(value); }
        }

        // TimeSpan can't be Xml Serialized
        public int RcvTimeOutSeconds
        {
            get { return RcvTimeOut.Seconds; }
            set { RcvTimeOut = TimeSpan.FromSeconds(value); }
        }

        [XmlIgnore]
        protected TimeSpan SendTimeOut { get; set; }
        [XmlIgnore]
        protected TimeSpan RcvTimeOut { get; set; }


        protected DateTime LastSendTime { get; set; }
        protected DateTime LastReceiveTime { get; set; }
        protected DateTime LastNotifyTime { get; set; }

        protected Dictionary<int, Telegram> AllTelegrams { get; set; }


        [XmlAttribute]
        public Int16 PLC_ID { get; set; }
        [XmlAttribute]
        public Int16 MFCS_ID { get; set; }



        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
            }
        }

        protected TimeSpan KeepALifeTime { get; set; }

        // TimeSpan can't be Xml Serialized
        public int KeepALiveTimeSeconds
        {
            get { return KeepALifeTime.Seconds; }
            set { KeepALifeTime = TimeSpan.FromSeconds(value); }
        }



        public BasicCommunicator()
        {
        }

        public virtual void CallRefresh()
        {
            OnRefresh?.Invoke();
        }

        public abstract void OnRecTelegram(Telegram t);


        public virtual void NotifyRcv(Telegram tel)
        {
            try
            {
                OnRecTelegram(tel);
                OnReceive?.Invoke(tel);
            }
            catch (Exception e)
            {
                Log.AddLog(Log.Severity.EXCEPTION, Name, nameof(NotifyRcv), e.Message);
            }
        }

        // notify about new send telegram
        public virtual void NotifySend(Telegram tel)
        {
            try
            {
                OnSend?.Invoke(tel);
            }
            catch (Exception e)
            {
                Log.AddLog(Log.Severity.EXCEPTION, Name, nameof(NotifyRcv), e.Message);
            }
        }

        public virtual void Initialize(BasicWarehouse wh)
        {
            Warehouse = wh;
            CraneByID = Warehouse.CraneList?.Where(prop => prop.CommunicatorName == Name).ToDictionary((p) => p.PLC_ID);
            ConveyorByID = Warehouse.ConveyorList?.Where(prop => prop.CommunicatorName == Name).ToDictionary((p) => p.PLC_ID);
            SegmentByID = Warehouse.SegmentList?.Where(prop => prop.CommunicatorName == Name).ToDictionary((p) => p.PLC_ID);
        }

        public virtual bool Online()
        {
            return (DateTime.Now - LastReceiveTime) < RcvTimeOut;
        }


        public abstract Task SendThreading(CancellationToken ct);
        public abstract Task RcvThreading(CancellationToken ct);
    }

}
