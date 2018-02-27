using System;
using System.Collections;
using System.Runtime.Serialization;

namespace Telegrams
{

    public class Position
    {
        [Int16ValueFieldAttr(Offset =0)]
        public Int16 R { get; set; }
        [Int16ValueFieldAttr(Offset = 2)]
        public Int16 X { get; set; }
        [Int16ValueFieldAttr(Offset = 4)]
        public Int16 Y { get; set; }
        [Int16ValueFieldAttr(Offset = 6)]
        public Int16 Z { get; set; }
    }

    public class Palette
    {
        const int FAULTCODE_LEFT = 0;
        const int FAULTCODE_RIGHT = 1;
        const int FAULTCODE_FRONT = 2;
        const int FAULTCODE_BACK = 3;
        const int FAULTCODE_HEIGHT = 4;
        const int FAULTCODE_WEIGHT = 5;
        const int FAULTCODE_BOTTOM = 6;
        const int FAULTCODE_BARCODE = 7;
        const int FAULTCODE_EMPTY = 8;
        const int FAULTCODE_MFCS = 9;


        [Int16ValueFieldAttr(Offset = 0)]
        public Int16 Type { get; set; }
        [Int16ValueFieldAttr(Offset = 2)]
        public Int16 Quantity { get; set; }
        [UInt32ValueFieldAttr(Offset = 4)]
        public UInt32 Barcode { get; set; }
        [UInt16ValueFieldAttr(Offset = 8)]
        public UInt16 Weight { get; set; }
        [BitArrayFieldAttr(Offset = 10, Length = 2)]
        public BitArray FaultCode { get; private set; }

        public Palette()
        {
            FaultCode = new BitArray(2);
        }
    }

    // Command TO to crane
    [Serializable]
    [KnownType(typeof(byte[]))]
    [TelegramSettingsAttr(ACK = true, Type = 1211, Length = 56, ValidFor = "CRANE")]
    public class TelegramCraneTO : Telegram
    {
        public const Int16 ORDER_GOTO = 11;
        public const Int16 ORDER_PICKUP = 12;
        public const Int16 ORDER_DROP = 13;
        public const Int16 ORDER_DELETEPALETTE = 97;
        public const Int16 ORDER_CREATEPALETTE = 98;
        public const Int16 ORDER_CANCEL = 99;

        public const Int16 CONFIRMATION_OK = 101;
        public const Int16 CONFIRMATION_CANCELBYMFCS = 102;
        public const Int16 CONFIRMATION_CANCELBYWAREHOUSE = 103;
        public const Int16 CONFIRMATION_INITIALNOTIFY = 104;
        public const Int16 CONFIRMATION_FAULT = 0;

        public const Int16 FAULT_OK = 0;
        public const Int16 FAULT_TELEGRAMSYNTAX = 101;
        public const Int16 FAULT_SENDER = 102;
        public const Int16 FAULT_RECEIVER = 103;
        public const Int16 FAULT_TYPE = 104;
        public const Int16 FAULT_MFCSID = 201;
        public const Int16 FAULT_ORDER = 202;
        public const Int16 FAULT_TARGET = 203;
        public const Int16 FAULT_TARGETUSE = 204;
        public const Int16 FAULT_PLACEBLOCKED = 205;
        public const Int16 FAULT_UNKNOWN = 206;
        public const Int16 FAULT_CONFIRMATIONNOTNULL = 207;
        public const Int16 FAULT_FAULTNOTNULL = 208;
        public const Int16 FAULT_PALETTE = 209;
        public const Int16 FAULT_PALETTEHEIGHT = 210;
        public const Int16 FAULT_CANCEL_NOCMD = 211;
        public const Int16 FAULT_CREATEPALETTE = 212;
        public const Int16 FAULT_CANCEL_NOTPOSSIBLE = 213;
        public const Int16 FAULT_MEMORY = 301;
        public const Int16 FAULT_NOTREPEATORDER = 302;
        public const Int16 FAULT_REPEATORDER = 303;
        public const Int16 FAULT_COMMLOAD = 304;
        public const Int16 FAULT_COMMDROP = 305;
        public const Int16 FAULT_TARGETLOADED = 306;
        public const Int16 FAULT_SOURCEEMPTY = 307;
        public const Int16 FAULT_TARGETNOTREACHABLE = 308;
        public const Int16 FAULT_FORKEMPTY = 309;
        public const Int16 FAULT_FORKOCCUPIED = 310;
        public const Int16 FAULT_BARCODE = 311;
        public const Int16 FAULT_PALETTEHEIGHT1 = 312;
        public const Int16 FAULT_PALETTEHEIGHT2 = 313;
        public const Int16 FAULT_UNKNOWN1 = 314;
        public const Int16 FAULT_FORKNOTMIDDLE = 315;
        public const Int16 FAULT_OBSTICLE = 316;
        public const Int16 FAULT_WRONGTARGET = 318;
        public const Int16 FAULT_PALETTETRANSFER = 319;

        [Int32ValueFieldAttr(Offset = 16)]
        public Int32 MFCS_ID { get; set; }
        [Int16ValueFieldAttr(Offset = 20)]
        public Int16 Order { get; set; }
        [ClassFieldOffset(Offset = 22)]
        public Position Position { get; set; }
        [Int16ValueFieldAttr(Offset = 30)]
        public Int16 ErrorControl { get; set; }
        [ClassFieldOffset(Offset = 32)]
        public Palette Palette  { get; set; }
        [Int32ValueFieldAttr(Offset = 44)]
        public Int32 Buffer_ID { get; set; }
        [Int16ValueFieldAttr(Offset = 48)]
        public Int16 ID { get; set; }

        [Int16ValueFieldAttr(Offset = 52)]
        public Int16 Confirmation { get; set; }
        [Int16ValueFieldAttr(Offset = 54)]
        public Int16 Fault { get; set; }

        public TelegramCraneTO()
        { }

        public override short ConveyorID()
        {
            return this.ID;
        }

        public TelegramCraneTO(TelegramCraneTO to) : base()
        {
            to.WriteBuffer();
            to.ByteBuffer.CopyTo(ByteBuffer, 0);
            ReadBuffer();
        }

        protected TelegramCraneTO(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

    }

    // Crane command status
    [Serializable]
    [KnownType(typeof(byte[]))]
    [TelegramSettingsAttr(ACK = true, Type = 1212, Length = 90, ValidFor = "CRANE")]
    public class TelegramCraneStatus : Telegram
    {
        public const int ORDER_NOORDER = 0;
        public const int ORDER_SYSTEMQUERY = 1;
        public const int ORDER_AUTOMATICON = 2;
        public const int ORDER_AUTOMATICOFF = 3;
        public const int ORDER_MFCSFAULT = 4;
        public const int ORDER_LONGTERMBLOCKON = 5;
        public const int ORDER_LONGTERMBLOCKOFF = 6;
        public const int ORDER_RESET = 7;
        public const int ORDER_SETDATETIME = 8;

        public const int STATUS_AUTOMATIC = 0;
        public const int STATUS_FAULT = 1;
        public const int STATUS_COMMANDACTIVE = 2;
        public const int STATUS_COMMANDINBUFFER = 3;
        public const int STATUS_REPEATORDER = 4;
        public const int STATUS_FORKSOCCUPIED = 5;
        public const int STATUS_FORKINMIDDLE = 6;
        public const int STATUS_REMOTE = 7;
        public const int STATUS_SAFETYFAULT = 8;
        public const int STATUS_LONGTERMBLOCK = 9;
        public const int STATUS_SATELITEOCCUPIED = 10;
        public const int STATUS_HANDMODE = 11;
        public const int STATUS_SATELITESAFETY = 12;
        public const int STATUS_AUTOMATICSTOP = 13;
        public const int STATUS_LOCAL = 14;
        public const int STATUS_SAFETYOVERRIDE = 15;

        public const int CONFIRMATION_NONE = 0;
        public const int CONFIRMATION_OK = 101;

        public const int FAULT_NONE = 0;
        public const int FAULT_OK = 0;
        public const int FAULT_TELEGRAMSYNTAX = 101;
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
        public Int16 ID { get; set; }
        [BitArrayFieldAttr(Length = 2, Offset = 24)]
        public BitArray Status { get; set; }
        [Int16ValueFieldAttr(Offset = 26)]
        public Int16 NumCommands { get; set; }
        [Int32ValueFieldAttr(Offset = 28)]
        public Int32 Command_ID { get; set; }
        [Int32ValueFieldAttr(Offset = 32)]
        public Int32 BufferCommand_ID { get; set; }
        [ClassFieldOffset(Offset = 36)]
        public Position LPosition { get; set; }
        [ClassFieldOffset(Offset = 44)]
        public Position FPosition { get; set; }
        [Int16ValueFieldAttr(Offset = 52)]
        public Int16 StateMachine { get; set; }
        [ClassFieldOffset(Offset = 54)]
        public Palette Palette { get; private set; }

        [BitArrayFieldAttr(Length = 16, Offset = 66)]
        public BitArray CurrentAlarms { get; private set; }
        // alarms bits are missing

        [Int16ValueFieldAttr(Offset = 82)]
        public Int16 AlarmID { get; set; }
        [Int16ValueFieldAttr(Offset = 86)]
        public Int16 Confirmation { get; set; }
        [Int16ValueFieldAttr(Offset = 88)]
        public Int16 Fault { get; set; }

        public TelegramCraneStatus()
        {
            Status = new BitArray(2 * 8);
            CurrentAlarms = new BitArray(16 * 8);
        }

        public override short ConveyorID()
        {
            return this.ID;
        }

        protected TelegramCraneStatus(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

    }



}
