using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Telegrams;

namespace Communication
{

    public delegate bool LongWaitForTelegram(Telegram tel);

    public class MFCSCommunicator : BasicCommunicator
    {
        [XmlIgnore]
        private List<string> ForceNotifyVM { get; set; }
        [XmlIgnore]
        public Dictionary<string, Action> DirectNotifyVM { get; private set; }
        private object _lockForceNotifyVM;
        [XmlIgnore]
        public AutoResetEvent _newDirectNotifyVM { get; private set; }

        [XmlIgnore]
        public Action<Telegram> INotifyTelegramCallBack { get; set; }
        [XmlIgnore]
        public LongWaitForTelegram INotifyLongWait { get; set; }


        public MFCSCommunicator() : base()
        {
            _lockForceNotifyVM = new object();
            ForceNotifyVM = new List<string>();
            DirectNotifyVM = new Dictionary<string, Action>();
            _newDirectNotifyVM = new AutoResetEvent(false);
        }

        public override void SendThreading()
        {

            LastSendTime = DateTime.Now;
            Telegram tel = null;

            while (Thread.CurrentThread.ThreadState == ThreadState.Background)
                try
                {
                    lock (_lockSendTelegram)
                        tel = SendTelegrams.FirstOrDefault(prop => prop.CommSendStatus < Telegram.CommSendStatusEnum.Ack);
                    if (tel != null)
                    {
                        if (INotifyTelegramCallBack == null)
                            throw new TelegramException(String.Format("Callback for {0} is not registered.", Name));
                        INotifyTelegramCallBack?.Invoke(tel);
                        Log?.AddLog(Log.Severity.EVENT, Name, "MFCSCommunicator.SendThreading Telegram written", tel.ToString());
                        do
                        {
                            WaitHandle.WaitAny(new WaitHandle[] { _newSendNotifyTelegram }, (int)SendTimeOut.TotalMilliseconds, true); // TODO it is a long time
                            Telegram tel1 = null;
                            lock (_lockSendNotifyTelegram)
                                tel1 = SendNotifyTelegrams.FirstOrDefault();
                            if (tel1 == null)
                                if (!INotifyLongWait.Invoke(tel))
                                    throw new TelegramException("Long wait without knowledge of service communicator...");
                            if (tel1 != null)
                            {
                                // check if both telegrams are completely identical
                                tel.Sequence = tel1.Sequence;
                                tel1.CommSendStatus = tel.CommSendStatus;
                                tel.SetCRC();
                                tel1.SetCRC();
                                if (tel.CRC != tel1.CRC)
                                {
                                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.SendThreadind:tel={0}", tel.ToString());
                                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.SendThreadind:tel1={0}", tel1.ToString());
                                    throw new TelegramException(String.Format("SendNotify telegram CRC does not match ({0},{1})", tel.CRC, tel1.CRC));
                                }
                                lock (_lockSendTelegram)
                                    SendTelegrams.Remove(tel);
                                lock (_lockSendNotifyTelegram)
                                    SendNotifyTelegrams.Remove(tel1);
                                Log?.AddLog(Log.Severity.EVENT, Name, "MFCSCommunicator.SendThreading send phase finished", tel.ToString());
                                tel = null;
                            }
                        } while (tel != null);
                    }
                    else
                        WaitHandle.WaitAny(new WaitHandle[] { _newSendTelegram, _newSendNotifyTelegram }, 100, true);
                }
                catch (TelegramException ex)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.SendThread::TelegramException", ex.Message);
                    Thread.Sleep(1000);
                }
                catch (ThreadAbortException ex)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.RcvThreading::ThreadAbortException", ex.Message);
                    return;
                }
                catch (CommunicationException ex)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.SendThread::CommunicationException", ex.Message);
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.SendThread::Exception", ex.Message);
                    Thread.Sleep(1000);
                }

        }

        public override void RcvThreading()
        {
            LastSendTime = DateTime.Now;

            while (Thread.CurrentThread.ThreadState == ThreadState.Background)
                try
                {
                    Telegram tel = null;

                    // because of thread safety it is not written in .Foreach form
                    while (ForceNotifyVM.Count() > 0)
                    {
                        if (DirectNotifyVM.ContainsKey(ForceNotifyVM[0]))
                            DirectNotifyVM[ForceNotifyVM[0]]?.Invoke(); // null, null);
                        lock (_lockForceNotifyVM)
                            ForceNotifyVM.RemoveAt(0);
                    }
                    lock (_lockRcvTelegram)
                        tel = RcvTelegrams.FirstOrDefault();
                    if (DateTime.Now - LastNotifyTime > RefreshTime)
                    {
                        LastNotifyTime = DateTime.Now;
                        Log?.AddLog(Log.Severity.EVENT, Name, "MFCSCommunicator.RcvThreading", "Refresh() is called");
                        CallRefresh();
                    }
                    if (tel != null)
                    {
                        LastReceiveTime = DateTime.Now;
                        Log?.AddLog(Log.Severity.EVENT, Name, "MFCSCommunicator.RcvThreading Received:", tel.ToString());
                        NotifyRcv(tel);
                        lock (_lockRcvTelegram)
                            RcvTelegrams.Remove(tel);
                    }
                    else
                        WaitHandle.WaitAny(new WaitHandle[] { _newRcvTelegram, _newDirectNotifyVM }, 100, true);
                }
                catch (TelegramException ex)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.SendThread::TelegramException", ex.Message);
                    Thread.Sleep(1000);
                }
                catch (ThreadAbortException ex)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.RcvThreading::ThreadAbortException", ex.Message);
                    return;
                }
                catch (CommunicationException ex)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.SendThread::CommunicationException", ex.Message);
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Log?.AddLog(Log.Severity.EXCEPTION, Name, "MFCSCommunicator.SendThread::Exception", ex.Message);
                    Thread.Sleep(1000);
                }
        }

        public void AddForceVM(string name)
        {
            lock (_lockForceNotifyVM)
            { 
                if (!ForceNotifyVM.Contains(name))
                    ForceNotifyVM.Add(name);
                _newDirectNotifyVM.Set();
            }
        }

        public bool CheckIfForceVM(string name)
        {
            lock (_lockForceNotifyVM)
                return ForceNotifyVM.Contains(name);
        }

        public override void Dispose()
        {
            base.Dispose();
            _newDirectNotifyVM.Dispose();
        }

    }
}
