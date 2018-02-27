using System;
using Communication.TelegramsService;
using Telegrams;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace Communication
{

    [Serializable]
    public class ServiceCommunicatorException : Exception
    {
        public ServiceCommunicatorException(string s) : base(s) { }

        public ServiceCommunicatorException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }


    public class ServiceCommunicator : Communicator, ITelegramNotifyCallback
    {
        private TelegramNotifyClient TelegramNotify { get; set; }
        private object _lock { get; set; }



        public ServiceCommunicator() : base()
        {
            _lock = new object();
        }

        private void CreateNewProxy()
        {
            try
            {
                if (TelegramNotify != null)
                {
                    if (TelegramNotify.State == CommunicationState.Faulted)
                        TelegramNotify.Abort();
                    else
                        TelegramNotify.Close();
                    TelegramNotify = null;
                }
                var ic = new InstanceContext(this);
                TelegramNotify = new TelegramNotifyClient(ic);
                TelegramNotify.RegisterCommunicator(Name);
                Log?.AddLog(Log.Severity.EVENT, Name, String.Format("ServiceCommunicator.CreateNewProxy ({0})", Name), "");
            }
            catch (Exception ex)
            {
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.CreateNewProxy Exception", ex.Message);
                throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.NotifyRcv failed. Reason:{1}", Name, ex.Message));
            }
        }

        public override void NotifyRcv(Telegram tel)
        {
            try
            {
                TelegramNotify.TelegramNotifyRcv(tel, Name);
                Log?.AddLog(Log.Severity.EVENT, Name, "ServiceCommunicator.NotifyRcv", tel.ToString());
            }
            catch (CommunicationObjectAbortedException ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.NotifyRcv CommunicationObjectAbortException", ex.Message);
                throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.NotifyRcv failed. Reason:{1}", Name, ex.Message));
            }
            catch (CommunicationObjectFaultedException ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.NotifyRcv CommunicationObjectAbortException", ex.Message);
                throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.NotifyRcv failed. Reason:{1}", Name, ex.Message));
            }
            catch (Exception ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.NotifyRcv", ex.Message);
                throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.NotifyRcv failed. Reason :{1}", Name, ex.Message));
            }
        }

        public override void NotifySend(Telegram tel)
        {
            try
            {
                if (!(tel is TelegramLife))
                    TelegramNotify.TelegramNotifySend(tel,Name);
                Log?.AddLog(Log.Severity.EVENT, Name, "ServiceCommunicator.NotifySend", tel.ToString());
            }
            catch (CommunicationObjectAbortedException ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.NotifySend CommunicationObjectAbortException", ex.Message);
                throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.NotifySend failed. Reason:{1}", Name, ex.Message));
            }
            catch (CommunicationObjectFaultedException ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.NotifySend CommunicationObjectFaultException", ex.Message);
                throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.NotifySend failed. Reason:{1}", Name, ex.Message));
            }
            catch (Exception ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.NotifySend", ex.Message);
                throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.NotifySend failed. Reason:{1}", Name, ex.Message));
            }
        }

        public bool LongTelegramSendWait(Telegram t)
        {
            try
            {
                if (CurrentSendTelegram == null)
                    return false;
                t.Sequence = CurrentSendTelegram.Sequence;
                t.CommSendStatus = CurrentSendTelegram.CommSendStatus;
                t.Build();
                CurrentSendTelegram.SetCRC();
                return t.CRC == CurrentSendTelegram.CRC;
            }
            catch (Exception ex)
            {
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.LongTelegramSendWait failed", ex.Message);
                return false;
            }
        }

        public void TelegramSend(Telegram tel)
        {
            // send telegram
            try
            {
                AddSendTelegram(tel);
                Log?.AddLog(Log.Severity.EVENT, Name, "ServiceCommunicator.TelegramSend", tel.ToString());
            }
            catch (CommunicationObjectAbortedException ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.NotifySend CommunicationObjectAbortException", ex.Message);
                throw;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.NotifySend CommunicationObjectFaultException", ex.Message);
                throw;
            } 
            catch (Exception ex)
            {
                CreateNewProxy();
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.Send", ex.Message);
                throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.Send failed. Reason:{1}", Name, ex.Message));
            }
        }


        public override void Initialize(Log log)
        {
            try
            {
                base.Initialize(log);
                CreateNewProxy();
            }
            catch (Exception ex)
            {
                Log?.AddLog(Log.Severity.EXCEPTION, Name, "ServiceCommunicator.Initialize", ex.Message);
                // throw new ServiceCommunicatorException(String.Format("{0} ServiceCommunicator.Send failed. Reason:{1}", Name, ex.Message));
            }
        }

        public override void Dispose()
        {
            try
            {
                base.Dispose();
                TelegramNotify?.Close();
            }
            catch {}
        }

    }
}
