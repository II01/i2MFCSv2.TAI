using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Xml.Serialization;
using System.Configuration;

namespace Telegrams
{
    
    [Serializable]
    public class CommunicationException : Exception
    {
        public CommunicationException(string msg) : base(msg)  
        { 
        }
    }

    public class DispatchNode
    {
        public Predicate<Telegram> DispatchTerm {get;set;} // lahko FUNC<in T1,in T2,out T3>
        public Action<Telegram> DispatchTo { get; set; }
    }


    public class Communication
    {

        [XmlIgnore] 
        public List<DispatchNode> DispatchRcv { get; private set; }
        [XmlIgnore]
        public List<DispatchNode> DispatchSend { get; private set; }
        [XmlIgnore]
        public List<Action> OnRefresh { get; private set; }

        [XmlAttribute]
        public string Name { get; set; }

        // TimeSpan can't be Xml Serialized
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
        private TimeSpan SendTimeOut { get; set; }
        [XmlIgnore]
        private TimeSpan RcvTimeOut { get; set; }
        private object _lockSendTelegram { get; set; }
 

        // IPEndPoint could not be serialized
        public string StringSendIPEndPoint
        {
            get { return String.Format("{0}:{1}", SendIPEndPoint.Address.ToString(), SendIPEndPoint.Port); }
            set {
                string[] str = value.Split(':');
                SendIPEndPoint = new IPEndPoint(IPAddress.Parse(str[0]), int.Parse(str[1]));
            }
        }

        // IPEndPoint could not be xml serialized
        public string StringRcvIPEndPoint
        {
            get { return String.Format("{0}:{1}", RcvIPEndPoint.Address.ToString(), RcvIPEndPoint.Port); }
            set
            {
                string[] str = value.Split(':');
                RcvIPEndPoint = new IPEndPoint(IPAddress.Parse(str[0]), int.Parse(str[1]));
            }
        }

        [XmlIgnore]
        public IPEndPoint SendIPEndPoint { get; set; }

        [XmlIgnore]
        public IPEndPoint RcvIPEndPoint { get; set; }

        public bool SimulateMFCS { get; set; }

        [XmlIgnore]
        public Telegram KeepALifeTelegram { get; set; }

        // TimeSpan can't be Xml Serialized
        public int KeepALiveTimeSeconds
        {
            get { return KeepALifeTime.Seconds; }
            set { KeepALifeTime = TimeSpan.FromSeconds(value); }
        }

        // TimeSpan can't be Xml Serialized
        public int RefreshTimeSeconds
        {
            get { return RefreshTime.Seconds; }
            set { RefreshTime = TimeSpan.FromSeconds(value); }
        }

        private TimeSpan RefreshTime { get; set; }
        private TimeSpan KeepALifeTime { get; set; }

        [XmlAttribute]
        public Int16 PLC_ID { get; set; }
        [XmlAttribute]
        public Int16 MFCS_ID { get; set; }

        private Thread SendThread { get; set; }
        private Thread RcvThread { get; set; }
        private short Sequence { get; set; }
        private int Retry { get; set; }
        private DateTime LastSendTime { get; set; }
        private DateTime LastReceiveTime { get; set; }
        private DateTime LastNotifyTime { get; set; }

        private string _version;


        // it should be thread safe
        public void AddSendTelegram(Telegram t)
        {
            t.Build();
            lock (_lockSendTelegram)
                SendTelegrams.Add(t);
        }

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

        private Dictionary<int,Telegram> AllTelegrams { get; set; }

        [XmlIgnore]
        protected List<Telegram> SendTelegrams { get; private set; }
        [XmlIgnore]
        protected List<Telegram> RcvTelegrams { get; private set; }

        private Socket RcvSocket { get; set; }
        private Socket SendSocket { get; set; }

        private TcpListener RcvListener { get; set; }
        private TcpListener SendListener { get; set; }

        private DateTime SendTime { get; set; }


 
        public Communication()
        {
            OnRefresh = new List<Action>();
            SendTelegrams = new List<Telegram>();
            RcvTelegrams = new List<Telegram>();
            SendThread = new Thread(SendThreading);
            SendThread.IsBackground = true;
            RcvThread = new Thread(RcvThreading);
            RcvThread.IsBackground = true;
            InitSendSocket();
            InitRcvSocket();
            DispatchRcv = new List<DispatchNode>();
            DispatchSend = new List<DispatchNode>();
            KeepALifeTelegram = new TelegramLife();
            _lockSendTelegram = new Object();
        }


        // notify about new received telegram
        public virtual void NotifyRcv(Telegram tel)
        {
            try
            { 
                foreach (DispatchNode prop in DispatchRcv)
                    if (prop.DispatchTerm(tel))
                        prop.DispatchTo(tel);
                OnRefresh.ForEach(prop => prop?.Invoke());
            }
            catch ( Exception e)
            {
                Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.NotifyRvc", e.Message);
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
                    Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.NotifyRvc", e.Message);
                }
        }


        // Main thread for receiving telegrams
        private void RcvThreading()
        {
            try
            {
                if (SimulateMFCS)
                {
                    RcvListener = new TcpListener(RcvIPEndPoint);
                    Log.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", RcvIPEndPoint.ToString());
                    RcvListener.Start();
                }


                LastReceiveTime = DateTime.Now;

                while (Thread.CurrentThread.ThreadState == ThreadState.Background)
                {
                    try
                    {
                        RcvTelegrams.RemoveAll(p => p.CommRcvStatus >= Telegram.CommRcvStatusEnum.NotifyDone);
                        if (DateTime.Now - LastReceiveTime > RcvTimeOut)
                        {
                            InitRcvSocket();
                            LastReceiveTime = DateTime.Now;
                            Log.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", "Timeout receiving");
                        }
                        else if (DateTime.Now - LastNotifyTime > RefreshTime)
                        {
                            LastNotifyTime = DateTime.Now;
                            Log.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", "Refresh() is called");
                            OnRefresh.ForEach(prop => prop?.Invoke());
                        }
                        else if (!RcvSocket.Connected)
                        {
                            InitRcvSocket();
                            ConnectRcvPartner();
                        }
                        else
                        {
                            if (RcvSocket.Available == 0)
                                Thread.Sleep(1);
                            else
                            {
                                Telegram tel = new TelegramOnlyHeader();
                                int numRead = 0;
                                do
                                {
                                    numRead += RcvSocket.Receive(tel.ByteBuffer, numRead, tel.ByteBuffer.Length - numRead, SocketFlags.None);
                                } while (numRead < tel.ByteBuffer.Length) ;
                                // Log.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", String.Format("Received {0} bytes", numRead));
                                tel.ReadBuffer();
                                tel.Validate(false);
                                Telegram tel1 = Activator.CreateInstance(AllTelegrams[tel.TelType].GetType()) as Telegram;
                                tel.ByteBuffer.CopyTo(tel1.ByteBuffer, 0);
                                if (tel1.DesignLength() - tel.DesignLength() > 0 && tel.TelCode == 0)
                                {
                                    numRead = 0;
                                    do
                                    {
                                        numRead += RcvSocket.Receive(tel1.ByteBuffer, tel.DesignLength()+numRead, tel1.DesignLength() - tel.DesignLength()-numRead, SocketFlags.None);
                                    } while (numRead < tel1.DesignLength() - tel.DesignLength());
                                    // Log.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", String.Format("Received {0} bytes", numRead));
                                }
                                tel1.ReadBuffer();
                                tel1.Validate();
                                NotifyRcv(tel1);
                                TelegramACK telACK = new TelegramACK();
                                telACK.Sequence = tel.Sequence;
                                telACK.TelCode = (System.UInt16)0xFFFF;
                                telACK.TelType = tel.TelType;
                                telACK.Sender = tel.Receiver;
                                telACK.Receiver = tel.Sender;
                                telACK.Build();
                                RcvSocket.Send(telACK.ByteBuffer);
                                Log.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", String.Format("ACK sended {0}", telACK.ToString()));
                                tel1.CommRcvStatus = Telegram.CommRcvStatusEnum.Ack;
                                RcvTelegrams.Add(tel1);
                                LastReceiveTime = DateTime.Now;
                                LastNotifyTime = DateTime.Now;
                                Log.AddLog(Log.Severity.EVENT, Name, "Communication.RcvThreading", String.Format("Received finished : {0}", tel1.ToString()));
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.RcvThreading::Socket", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (TelegramException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.RcvThreading::Telegram", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.RcvThreading::KeyNotFound", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (CommunicationException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.RcvThreading::Communication", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (ThreadAbortException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.RcvThreading::Communication", ex.Message);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.SendThread::unknown", ex.Message);
                        Thread.Sleep(1000);
                    }
                }
            }
            finally
            {
                RcvSocket.Close();
                RcvSocket.Dispose();
                SendSocket.Close();
                SendSocket.Dispose();
            }
        }


        // Main thread for sending telegrams
        private void SendThreading()
        {
            TelegramACK telAck = null;

            try
            {
                if (SimulateMFCS)
                {
                    SendListener = new TcpListener(SendIPEndPoint);
                    Log.AddLog(Log.Severity.EVENT, Name, "Communication.SendThreading", SendIPEndPoint.ToString());
                    SendListener.Start();
                }

                lock (_lockSendTelegram)
                {
                    SendTelegrams.RemoveAll(p => p.CommSendStatus >= Telegram.CommSendStatusEnum.Ack);
                }

                LastSendTime = DateTime.Now;

                while (Thread.CurrentThread.ThreadState == ThreadState.Background)
                    try
                    {
                        Telegram tel = null;
                        lock (_lockSendTelegram)
                            tel = SendTelegrams.FirstOrDefault(prop => prop.CommSendStatus < Telegram.CommSendStatusEnum.Ack);
                        if (DateTime.Now - LastSendTime > SendTimeOut)
                        {
                            InitSendSocket();
                            if (tel != null)
                                tel.CommSendStatus = Telegram.CommSendStatusEnum.None;
                            LastSendTime = DateTime.Now;
                            Retry = 0;
                            Log.AddLog(Log.Severity.EVENT, Name, "Communication.SendThread", "Send timeout, SendSocket reinitialized!");
                        }
                        else if (DateTime.Now - LastSendTime > KeepALifeTime && tel == null)
                        {
                            Telegram tRes = null;
                            lock (_lockSendTelegram)
                                tRes = SendTelegrams.FirstOrDefault(prop=>prop.CommSendStatus < Telegram.CommSendStatusEnum.Ack);
                            if ( tRes == null)
                                if (KeepALifeTelegram != null)
                                {
                                    Telegram t = Activator.CreateInstance(KeepALifeTelegram.GetType()) as Telegram;
                                    t.Sender = MFCS_ID;
                                    t.Receiver = PLC_ID;
                                    t.Build();
                                    AddSendTelegram(t);
                                    Log.AddLog(Log.Severity.EVENT, Name, "Communication.SendThread", String.Format("Adding KeepALife telegram"));
                                }
                        }
                        else if (!SendSocket.Connected)
                        {
                            InitSendSocket();
                            ConnectSendPartner();
                        }
                        else if (tel != null)
                        {
                            switch (tel.CommSendStatus)
                            {
                                case (Telegram.CommSendStatusEnum.None):
                                    tel.Sequence = Sequence;
                                    tel.Build();
                                    // Log.AddLog(Log.Severity.EVENT, Name, "Communication.SendThread", String.Format("Start sending {0}", tel.ToString()));
                                    SendSocket.Send(tel.ByteBuffer, tel.Length, SocketFlags.None);
                                    Log.AddLog(Log.Severity.EVENT, Name, "Communication.SendThread", String.Format("Sended {0}", tel.ToString()));
                                    tel.CommSendStatus = Telegram.CommSendStatusEnum.WaitACK;
                                    telAck = new TelegramACK();
                                    SendTime = DateTime.Now;
                                    break;
                                case (Telegram.CommSendStatusEnum.WaitACK):
                                    int numRead = 0;
                                    do
                                    {
                                        numRead += SendSocket.Receive(telAck.ByteBuffer,numRead,telAck.ByteBuffer.Length-numRead,SocketFlags.None);
                                    } while (numRead < telAck.ByteBuffer.Length);
                                    telAck.ReadBuffer();
                                    Log.AddLog(Log.Severity.EVENT, Name, "Communication.SendThread", String.Format("Received ACK {0}", telAck.ToString()));
                                    if (telAck.Validate() && telAck.Sequence == tel.Sequence)
                                    {
                                        tel.CommSendStatus = Telegram.CommSendStatusEnum.Ack;
                                        LastSendTime = DateTime.Now;
                                        Retry = 0;
                                        if (Sequence < 99)
                                            Sequence++;
                                        else
                                            Sequence = 0;
                                        Log.AddLog(Log.Severity.EVENT, Name, "Communication.SendThreading", String.Format("Send Finished : {0}", tel.ToString()));
                                        NotifySend(tel);
                                    }
                                    else
                                    {
                                        //                                      tel.CommSendStatus = Telegram.CommSendStatusEnum.None;
                                        Retry++;
                                        Log.AddLog(Log.Severity.EVENT, Name, "Communication.SendThreading", String.Format("Retry increased - {0}", Retry));
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                            Thread.Sleep(100);
                    }
                    catch (SocketException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.SendThread::SocketException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (TelegramException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.SendThread::TelegramException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    catch (ThreadAbortException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.RcvThreading::Communication", ex.Message);
                        return; 
                    }
                    catch (CommunicationException ex)
                    {
                        Log.AddLog(Log.Severity.EXCEPTION, Name, "Communication.SendThread::CommunicationException", ex.Message);
                        Thread.Sleep(1000);
                    }
                    


            }
            finally
            {
                SendSocket.Close();
                SendSocket.Dispose();
                RcvSocket.Close();
                RcvSocket.Dispose();
            }
        }

        private void ConnectSendPartner()
        {
            if (SimulateMFCS)
                if (SendListener.Pending())
                {
                    SendSocket = SendListener.AcceptSocket();
                    Log.AddLog(Log.Severity.EVENT, Name, "Communication.ConnectSendPartner", "SendSocket connected");
                }
                else
                {
                    Thread.Sleep(1000);
                    // throw new CommunicationException("CommunicationException - no clientSocket on send port.");
                }
            else
            {
                SendSocket.Connect(SendIPEndPoint);
                Log.AddLog(Log.Severity.EVENT, Name, "Communication.ConnectSendPartner", "SendSocket connected");
            }
        }


        private void ConnectRcvPartner()
        {
            if (SimulateMFCS)
                if (RcvListener.Pending())
                {
                    RcvSocket = RcvListener.AcceptSocket();
                    Log.AddLog(Log.Severity.EVENT, Name, "Communication.ConnectRcvPartner", "RcvSocket connected");
                }
                else
                {
                    Thread.Sleep(100);
                    // throw new CommunicationException("CommunicationException - no clientSocket on rcv port.");
                }
            else
            {
                RcvSocket.Connect(RcvIPEndPoint);
                Log.AddLog(Log.Severity.EVENT, Name, "Communication.ConnectRcvPartner", "RcvSocket connected");
            }
        }

        // initialze RcvSocket
        private void InitRcvSocket()
        {
            if (RcvSocket != null)
            {
                RcvSocket.Close();
                Thread.Sleep(1000);
                //                RcvSocket.Dispose();
            }
            RcvSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            RcvSocket.ReceiveTimeout = 10000;
            RcvSocket.SendTimeout = 10000;
            RcvSocket.Blocking = true;
            RcvSocket.LingerState.Enabled = true;
            RcvSocket.LingerState.LingerTime = 0;
        }

        private void InitSendSocket()
        {
            if (SendSocket != null)
            {
                SendSocket.Close();
                Thread.Sleep(1000);
                //                SendSocket.Dispose();
            }
            SendSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            SendSocket.ReceiveTimeout = 10000;
            SendSocket.SendTimeout = 10000;
            SendSocket.Blocking = true;
            SendSocket.LingerState.Enabled = true;
            SendSocket.LingerState.LingerTime = 0;
        }

        private void AssignAllTelegrams()
        {
            AllTelegrams = (from typ in Assembly.GetExecutingAssembly().GetExportedTypes()
                            where (typ.GetCustomAttribute<TelegramSettingsAttr>() != null)
                            where (typ.GetCustomAttribute<TelegramSettingsAttr>().ValidFor.Split(';').Contains(Version))
                            select new
                            {
                                key = (int)typ.GetCustomAttribute<TelegramSettingsAttr>().Type,
                                value = Activator.CreateInstance(typ) as Telegram
                            }).ToDictionary(p => p.key, p => p.value);
        }


        // is system online
        public bool Online()
        {
            return (DateTime.Now - LastReceiveTime) < TimeSpan.FromSeconds(KeepALiveTimeSeconds * 3);
        }

        public void StartCommunication()
        {
            RcvThread.Start();
            SendThread.Start();
        }

        public void StopCommunication()
        {
            try
            {
                RcvThread.Abort();
            }
            catch
            { }
            try
            {
                SendThread.Abort();
            }
            catch
            { }

        }

    }
}
