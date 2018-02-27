using System;
using System.Collections;
using System.Runtime.Serialization;

namespace Telegrams
{
    // Transport TO
    [Serializable]
    [KnownType(typeof(byte[]))]
    [TelegramSettingsAttr(ACK = true, Type = 1101, Length = 52, ValidFor = "TRANSPORT")]
    public class TelegramTransportTO : Telegram
    {
        public const int ORDER_NONE = 0;
        public const int ORDER_MOVE = 1;
        public const int ORDER_INPUTCONTROL = 2;
        public const int ORDER_PALETTEDELETE = 3;
        public const int ORDER_PALETTECREATE = 4;
        public const int ORDER_STACK = 5;
        public const int ORDER_DESTACK = 6;
        public const int ORDER_DESTACK1 = 7;
        public const int ORDER_STACK1 = 8;
        public const int ORDER_DESTACK2 = 9;
        public const int ORDER_STRAP = 10;

        public const int CONFIRMATION_NONE = 0;
        public const int CONFIRMATION_NEWPALETTE = 101;
        public const int CONFIRMATION_NOTIFY = 102;
        public const int CONFIRMATION_COMMANDFINISHED = 103;
        public const int CONFIRMATION_PALETTETAKEN = 104;
        public const int CONFIRMATION_FAULT = 105;
        public const int CONFIRMATION_INITIALNOTIFY = 106;
        public const int CONFIRMATION_PALETTEDELETED = 201;
        public const int CONFIRMATION_PALETTECREATED = 202;

        public const int FAULT_NONE = 0;
        public const int FAULT_OK = 0;
        public const int FAULT_TELEGRAMSYNTAX = 101;
        public const int FAULT_SENDER = 102;
        public const int FAULT_RECEIVER = 103;
        public const int FAULT_TYPE = 104;
        public const int FAULT_ORDER = 201;
        public const int FAULT_SOURCE = 202;
        public const int FAULT_TARGET = 203;
        public const int FAULT_CONFIRMATION = 204;
        public const int FAULT_FAULT = 205;
        public const int FAULT_PALETTE = 206;
        public const int FAULT_NOPALETTEONSOURCE = 207;
        public const int FAULT_WEIGHT = 301;
        public const int FAULT_STACKERFULL = 302;

        [Int32ValueFieldAttr(Offset = 16)]
        public Int32 MFCS_ID { get; set; }
        [Int16ValueFieldAttr(Offset = 20)]
        public Int16 Order { get; set; }
        [Int16ValueFieldAttr(Offset = 22)]
        public Int16 Source { get; set; }
        [Int16ValueFieldAttr(Offset = 24)]
        public Int16 Target { get; set; }
        [Int16ValueFieldAttr(Offset = 26)]
        public Int16 SenderTransport { get; set; }
        [ClassFieldOffset(Offset = 28)]
        public Palette Palette { get; set; }
        [Int16ValueFieldAttr(Offset = 40)]
        public Int16 Previous { get; set; }
        [Int32ValueFieldAttr(Offset = 44)]
        public Int32 MaxWeight { get; set; }
        [Int16ValueFieldAttr(Offset = 48)]
        public Int16 Confirmation { get; set; }
        [Int16ValueFieldAttr(Offset = 50)]
        public Int16 Fault { get; set; }



        public TelegramTransportTO()
        {
            Palette = new Palette();
        }

        public override short ConveyorID()
        {
            return this.SenderTransport;
        }


        public TelegramTransportTO(TelegramTransportTO to) : base()
        {
            to.WriteBuffer();
            to.ByteBuffer.CopyTo(ByteBuffer, 0);
            ReadBuffer();
        }

        protected TelegramTransportTO(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

    }


    // Transport status
    [Serializable]
    [KnownType(typeof(byte[]))]
    [TelegramSettingsAttr(ACK = true, Type = 1112, Length = 52, ValidFor = "TRANSPORT")]
    public class TelegramTransportStatus : Telegram
    {
        public const int ORDER_INFO = 1;
        public const int ORDER_AUTOON = 2;
        public const int ORDER_AUTOOFF = 3;
        public const int ORDER_ALARM = 4;
        public const int ORDER_LONGTERMBLOCKON = 5;
        public const int ORDER_LONGTERMBLOCKOFF = 6;
        public const int ORDER_RESET = 7;
        public const int ORDER_TIMESYNC = 8;


        public const int STATUS_REMOTE = 0;
        public const int STATUS_AUTOMATIC = 1;
        public const int STATUS_FAULT = 2;
        public const int STATUS_SAFETYFAULT = 3;
        public const int STATUS_LONGTERMFAULT = 4;
        public const int STATUS_LOGICALFAULT = 5;

        public const int CONFIRMATION_NONE = 0;
        public const int CONFIRMATION_OK = 101;

        public const int FAULT_NONE = 0;
        public const int FAULT_OK = 0;
        public const int FAULT_TELEGRAMSYTAX = 101;
        public const int FAULT_SENDER = 102;
        public const int FAULT_RECEIVER = 103;
        public const int FAULT_TYPE = 104;
        public const int FAULT_VERSION = 105;
        public const int FAULT_ORDER = 201;

        [Int32ValueFieldAttr(Offset = 16)]
        public Int32 MFCS_ID { get; set; }
        [Int16ValueFieldAttr(Offset = 20)]
        public Int16 Order { get; set; }
        [Int16ValueFieldAttr(Offset = 22)]
        public Int16 SegmentID { get; set; }
        [BitArrayFieldAttr(Length = 2, Offset = 24)]
        public BitArray Status { get; private set; }
        [BitArrayFieldAttr(Length = 4, Offset = 26)]
        public BitArray State { get; private set; }
        [BitArrayFieldAttr(Length = 16, Offset = 30)]
        public BitArray Alarms { get; private set; }

        [Int16ValueFieldAttr(Offset = 46)]
        public Int16 FirstAlarmID { get; set; }
 
    
        [Int16ValueFieldAttr(Offset = 48)]
        public Int16 Confirmation { get; set; }
        [Int16ValueFieldAttr(Offset = 50)]
        public Int16 Fault { get; set; }

        public TelegramTransportStatus()
        {
            Status = new BitArray(16);
            State = new BitArray(32);
            Alarms = new BitArray(128);
        }

        public override short ConveyorID()
        {
            return this.SegmentID;
        }

        protected TelegramTransportStatus(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

    }

    public class PLCSetTime
    {
        [Int16ValueFieldAttr(Offset = 0)]
        public Int16 Year { get; set; }
        [Int16ValueFieldAttr(Offset = 2)]
        public Int16 Month { get; set; }
        [Int16ValueFieldAttr(Offset = 4)]
        public Int16 Day { get; set; }
        [Int16ValueFieldAttr(Offset = 6)]
        public Int16 Hour { get; set; }
        [Int16ValueFieldAttr(Offset = 8)]
        public Int16 Minute { get; set; }
        [Int16ValueFieldAttr(Offset = 10)]
        public Int16 Seconds { get; set; }

    }

    // Transport status
    [Serializable]
    [KnownType(typeof(byte[]))]
    [TelegramSettingsAttr(ACK = true, Type = 1000, Length = 38, ValidFor = "TRANSPORT")]
    public class TelegramTransportSetTime : Telegram
    {
        public const int ORDER_SETDATETIME = 1;

        public const int CONFIRMATION_NONE = 0;
        public const int CONFIRMATION_OK = 101;


        [Int32ValueFieldAttr(Offset = 16)]
        public Int32 MFCS_ID { get; set; }
        [Int16ValueFieldAttr(Offset = 20)]
        public Int16 Order { get; set; }


        [ClassFieldOffset(Offset = 22)]
        public PLCSetTime PLCSetTime { get; set; }


        [Int16ValueFieldAttr(Offset = 34)]
        public Int16 Confirmation { get; set; }
        [Int16ValueFieldAttr(Offset = 36)]
        public Int16 Fault { get; set; }

        public TelegramTransportSetTime()
        {
        }

        protected TelegramTransportSetTime(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }


    }

    // Transport extra info
    [Serializable]
    [KnownType(typeof(byte[]))]
    [TelegramSettingsAttr(ACK = true, Type = 1113, Length = 22, ValidFor = "TRANSPORT")]
    public class TelegramPalletRemoved : Telegram
    {
        public const int CONFIRMATION_NONE = 0;
        public const int CONFIRMATION_OK = 101;

        [Int32ValueFieldAttr(Offset = 16)]
        public Int16 Location { get; set; }
        [Int16ValueFieldAttr(Offset = 18)]
        public Int16 Confirmation { get; set; }
        [Int16ValueFieldAttr(Offset = 20)]
        public Int16 Fault { get; set; }

        public TelegramPalletRemoved()
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }


    }



}
