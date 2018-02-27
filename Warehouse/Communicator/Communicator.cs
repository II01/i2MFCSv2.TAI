using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Telegrams;
using SimpleLog;
using System.Threading;
using Warehouse.WCF_RcvTelProxy;
using Warehouse.WCF_SendTelProxy;
using Warehouse.ConveyorUnits;

namespace MFCS.Communication
{
    public class Communicator : BasicCommunicator
    {
        private TaskCompletionSource<Telegram> _newSendTelegram ;
        [XmlIgnore]

        protected Queue<Telegram> SendTelegrams { get; private set; }
        private object _lockSendTelegrams;

        public Communicator() : base()
        {
            SendTelegrams = new Queue<Telegram>();
            KeepALifeTelegram = new TelegramLife();
            _newSendTelegram = new TaskCompletionSource<Telegram>();
            _lockSendTelegrams = new object();
        }


        override public void AddSendTelegram(Telegram t)
        {
            lock(_lockSendTelegrams)
            {
                t.Build();
                SendTelegrams.Enqueue(t);
                if (!_newSendTelegram.Task.IsCompleted)
                    _newSendTelegram.SetResult(SendTelegrams.First());
                Log.AddLog(Log.Severity.EVENT, Name, $"SendTelegrams.Enqueue({t.ToString()})");
            }
        }


        [XmlIgnore]
        public Telegram CurrentSendTelegram { get; set; }
        // TimeSpan can't be Xml Serialized


        // IPEndPoint could not be serialized
        public string StringSendIPEndPoint
        {
            get { return String.Format("{0}:{1}", SendIPEndPoint.Address.ToString(), SendIPEndPoint.Port); }
            set
            {
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


        [XmlIgnore]
        public Telegram KeepALifeTelegram { get; set; }


        // TimeSpan can't be Xml Serialized



        private DateTime SendTime { get; set; }


        public override void OnRecTelegram(Telegram t)
        {
            if (t is TelegramPalletRemoved)
            {
                // not connected to Conveyor
                // TODO 1113
            }
            else
            {
                if (SegmentByID.ContainsKey(t.ConveyorID()))
                    (SegmentByID[t.ConveyorID()]).OnRecTelegram(t);
                if (ConveyorByID.ContainsKey(t.ConveyorID()))
                    (ConveyorByID[t.ConveyorID()]).OnReceiveTelegram(t);
                if (CraneByID.ContainsKey(t.ConveyorID()))
                    (CraneByID[t.ConveyorID()]).OnReceiveTelegram(t);
            }
        }



    // Main thread for receiving telegrams
    public override async Task RcvThreading(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using (WCF_RcvTelProxyClient rcvTel = new WCF_RcvTelProxyClient())
                    {
                        await rcvTel.InitAsync(Name, RcvIPEndPoint.Address.ToString(), RcvIPEndPoint.Port, RcvTimeOutSeconds, Version);
                        Task delay = Task.Delay(RefreshTime);
                        Task<Telegram> read = rcvTel.ReadAsync();
                        while (!ct.IsCancellationRequested)
                        {

                            Task task = await Task.WhenAny(read, delay).ConfigureAwait(false);
                            await task.ConfigureAwait(false);
                            if (task == read)
                            {
                                var tel = await read.ConfigureAwait(false);
                                LastReceiveTime = DateTime.Now;
                                NotifyRcv(tel);
                                Log.AddLog(Log.Severity.EVENT, Name, $"NotifyRcv({tel.ToString()}");
                                LastNotifyTime = DateTime.Now;
                                read = rcvTel.ReadAsync();
                            }
                            if (task == delay)
                            {
                                OnRefresh?.Invoke();
                                delay = Task.Delay(RefreshTime);
                                Log.AddLog(Log.Severity.EVENT, Name, $"OnRefresh called");
                            }
                        }
                    }
                }
                catch( Exception ex)
                {
                    Log.AddLog(Log.Severity.EXCEPTION, Name, ex.Message);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
        }


        // Main thread for sending telegrams
        public override async Task SendThreading(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using (WCF_SendTelProxyClient sendTel = new WCF_SendTelProxyClient())
                    {
                        await sendTel.InitAsync(Name, SendIPEndPoint.Address.ToString(), SendIPEndPoint.Port, SendTimeOutSeconds, Version);
                        while (!ct.IsCancellationRequested)
                        {
                            Task longSilence = Task.Delay(KeepALifeTime);
                            lock (_lockSendTelegrams)
                                if (SendTelegrams.Count > 0 && !_newSendTelegram.Task.IsCompleted)
                                    _newSendTelegram.SetResult(SendTelegrams.First());
                            Task task = await Task.WhenAny(_newSendTelegram.Task, longSilence).ConfigureAwait(false);
                            await task.ConfigureAwait(false);
                            if (_newSendTelegram.Task.IsCompleted)
                            {
                                Telegram t = _newSendTelegram.Task.Result;
                                await sendTel.SendAsync(t).ConfigureAwait(false);
                                SendTime = DateTime.Now;
                                NotifySend(t);
                                Log.AddLog(Log.Severity.EVENT, Name, $"NotifySend({t.ToString()})");
                                lock (_lockSendTelegrams)
                                {
                                    _newSendTelegram = new TaskCompletionSource<Telegram>();
                                    SendTelegrams.Dequeue();
                                }
                            }
                            else if (task == longSilence)
                            {
                                KeepALifeTelegram.Sender = MFCS_ID;
                                KeepALifeTelegram.Receiver = PLC_ID;
                                AddSendTelegram(KeepALifeTelegram);
                            }
                        }
                    }
                }
                catch( Exception ex)
                {
                    Log.AddLog(Log.Severity.EXCEPTION, Name, ex.Message);
                    await Task.Delay(1000).ConfigureAwait(false) ;
                }
            }
        }

    }
}
