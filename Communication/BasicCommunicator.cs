using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using Telegrams;

namespace Communication
{
    public abstract class BasicCommunicator : IDisposable
    {
        protected AutoResetEvent _newSendTelegram;
        protected AutoResetEvent _newRcvTelegram;
        protected AutoResetEvent _newSendNotifyTelegram; // send finished
        protected string _version;


        [XmlIgnore]
        public Log Log { get; set; }
        [XmlIgnore]
        public List<DispatchNode> DispatchRcv { get; private set; }
        [XmlIgnore]
        public List<DispatchNode> DispatchSend { get; private set; }
        [XmlIgnore]
        public List<Action> OnRefresh { get; private set; }

        [XmlAttribute]
        public string Name { get; set; }

        // TimeSpan can't be Xml Serialized

        protected object _lockSendTelegram { get; set; }
        protected object _lockRcvTelegram { get; set; }
        protected object _lockSendNotifyTelegram { get; set; }

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


        protected Thread SendThread { get; set; }
        protected Thread RcvThread { get; set; }

        protected DateTime LastSendTime { get; set; }
        protected DateTime LastReceiveTime { get; set; }
        protected DateTime LastNotifyTime { get; set; }

        protected Dictionary<int, Telegram> AllTelegrams { get; set; }

        [XmlIgnore]
        protected List<Telegram> SendTelegrams { get; private set; }
        [XmlIgnore]
        protected List<Telegram> RcvTelegrams { get; private set; }
        [XmlIgnore]
        protected List<Telegram> SendNotifyTelegrams { get; private set; }

        [XmlAttribute]
        public Int16 PLC_ID { get; set; }
        [XmlAttribute]
        public Int16 MFCS_ID { get; set; }



        // it should be thread safe


        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
                AssignAllTelegrams(); // based on current version 
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
            OnRefresh = new List<Action>();
            SendThread = new Thread(SendThreading);
            SendThread.IsBackground = true;
            RcvThread = new Thread(RcvThreading);
            RcvThread.IsBackground = true;
            SendTelegrams = new List<Telegram>();
            RcvTelegrams = new List<Telegram>();
            SendNotifyTelegrams = new List<Telegram>();
            DispatchRcv = new List<DispatchNode>();
            DispatchSend = new List<DispatchNode>();
            _lockSendTelegram = new object();
            _lockRcvTelegram = new object();
            _lockSendNotifyTelegram = new object();
            _newSendTelegram = new AutoResetEvent(false);
            _newRcvTelegram = new AutoResetEvent(false);
            _newSendNotifyTelegram = new AutoResetEvent(false);
        }

        public virtual void CallRefresh()
        {
            OnRefresh.ForEach(prop => prop?.Invoke());
        }

        public virtual void NotifyRcv(Telegram tel)
        {
            try
            {
                foreach (DispatchNode prop in DispatchRcv)
                    if (prop.DispatchTerm(tel))
                        prop.DispatchTo(tel);
//                CallRefresh();
            }
            catch (Exception e)
            {
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "BasicCommunicator.NotifyRvc failed", e.Message);
            }
        }

        // notify about new send telegram
        public virtual void NotifySend(Telegram tel)
        {
            foreach (DispatchNode prop in DispatchSend)
                try
                {
                    if (prop.DispatchTerm(tel))
                        prop.DispatchTo(tel);
                }
                catch (Exception e)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "BasicCommunicator.NotifySend failed", e.Message);
                }
        }

        public void AddSendNotifyTelegram(Telegram t)
        {
            lock (_lockSendNotifyTelegram)
                SendNotifyTelegrams.Add(t);
            _newSendNotifyTelegram.Set(); 
        }

        public void AddRcvTelegram(Telegram t)
        {
            lock (_lockRcvTelegram)
                RcvTelegrams.Add(t);
            _newRcvTelegram.Set();
        }

        public void AddSendTelegram(Telegram t)
        {
            t.Build();
            lock (_lockSendTelegram)
                SendTelegrams.Add(t);
            _newSendTelegram.Set();
        }

        protected void AssignAllTelegrams()
        {
            AllTelegrams = (from typ in Assembly.GetAssembly(typeof(Telegram)).GetExportedTypes()
                            where (typ.GetCustomAttribute<TelegramSettingsAttr>() != null)
                            where (typ.GetCustomAttribute<TelegramSettingsAttr>().ValidFor.Split(';').Contains(Version))
                            select new
                            {
                                key = (int)typ.GetCustomAttribute<TelegramSettingsAttr>().Type,
                                value = Activator.CreateInstance(typ) as Telegram
                            }).ToDictionary(p => p.key, p => p.value);
        }

        public virtual void StartCommunication()
        {
            RcvThread.Start();
            SendThread.Start();
        }


        public virtual void Dispose()
        {
            try { RcvThread.Abort();}
            catch { }

            try { SendThread.Abort();}
            catch { }

            _newRcvTelegram.Dispose();
            _newSendTelegram.Dispose();
            _newSendNotifyTelegram.Dispose();
        }


        public virtual void Initialize(Log log)
        {
            Log = log;
            AssignAllTelegrams();
        }

        public virtual bool Online()
        {
            return (DateTime.Now - LastReceiveTime) < RcvTimeOut;
        }


        public abstract void SendThreading();
        public abstract void RcvThreading();
    }

 }
