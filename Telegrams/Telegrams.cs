using System;
using System.Reflection;
using System.Runtime.Serialization;


namespace Telegrams
{
    public class TelegramSettingsAttr : Attribute
    {
        public bool ACK { get; set; }
        public Int16 Type { get; set; }
        public Int16 Length { get; set; }
        public string ValidFor { get; set; }
    }

    [Serializable]
    [KnownType(typeof(byte[]))]
    [KnownType(typeof(TelegramCraneStatus))]
    [KnownType(typeof(TelegramCraneTO))]
    [KnownType(typeof(TelegramTransportTO))]
    [KnownType(typeof(TelegramTransportStatus))]
    [KnownType(typeof(TelegramTransportSetTime))]
    [KnownType(typeof(TelegramPalletRemoved))]
    [KnownType(typeof(TelegramLife))]
    [KnownType(typeof(TelegramACK))]

    public class Telegram : TelegramHeader, ISerializable
    {
        public enum CommSendStatusEnum { None = 0, Resend, Send, WaitACK, Ack }
        public enum CommRcvStatusEnum { None = 0, SendACK, Ack, NotifyDone }

        public CommSendStatusEnum CommSendStatus { get; set; }
        public CommRcvStatusEnum CommRcvStatus { get; set; }



        public Telegram()
        {
            CommRcvStatus = CommRcvStatusEnum.None;
            CommSendStatus = CommSendStatusEnum.None;
            ByteBuffer = new byte[DesignLength()];
        }

        protected Telegram(SerializationInfo info, StreamingContext context) : base()
        {
            ByteBuffer = (byte[]) info.GetValue("Buffer", typeof(byte[]));
            ReadBuffer();
        }

        public virtual short ConveyorID()
        {
            return -1;
        }


        public int DesignType()
        {
            return GetType().GetCustomAttribute<TelegramSettingsAttr>().Type;
        }

        public int DesignLength()
        {
            return GetType().GetCustomAttribute<TelegramSettingsAttr>().Length;
        }

        public void Build()
        {
            Type type = this.GetType();

            foreach (var pi in type.GetProperties())
                foreach (var ca in pi.GetCustomAttributes<ValidateAttr>())
                    pi.SetValue(this, ca.Value);

            Length = type.GetCustomAttribute<TelegramSettingsAttr>().Length;
            if (TelCode != 0xFFFF)
                TelType = type.GetCustomAttribute<TelegramSettingsAttr>().Type;
            SetCRC();
        }


        public bool Validate(bool checkCRC = true)
        {
            Type type = this.GetType();

            if (checkCRC)
                if (!CheckCRC())
                    throw new TelegramException(String.Format("TelegramException:{0} Validation CRC failed.", this.ToString()));

            foreach (var pi in type.GetProperties())
                foreach (var ca in pi.GetCustomAttributes<ValidateAttr>())
                    if (!ca.Validate(pi.GetValue(this)))
                        throw new TelegramException(String.Format("TelegramException:Property {0} validation with value {1} failed.", pi.Name, pi.GetValue(this)));
            return true;
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            WriteBuffer();
            info.AddValue("Buffer", this.ByteBuffer, this.ByteBuffer.GetType());
        }


    }

    [Serializable]
    [TelegramSettingsAttr(ACK = false, Type = 99, Length = 16, ValidFor = "")]
    public class TelegramOnlyHeader : Telegram
    {
        public TelegramOnlyHeader() : base() { }
        protected TelegramOnlyHeader(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    [TelegramSettingsAttr(ACK = false, Type = 100, Length = 16, ValidFor = "CRANE;TRANSPORT")]
    public class TelegramACK : Telegram
    {
        public TelegramACK() : base() { }
        protected TelegramACK(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }


    [Serializable]
    [TelegramSettingsAttr(ACK = true, Type = 1, Length = 16, ValidFor = "CRANE;TRANSPORT")]
    public class TelegramLife : Telegram
    {
        public TelegramLife() : base() { }
        protected TelegramLife(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }


}