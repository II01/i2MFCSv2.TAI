using System;
using System.Runtime.Serialization;

namespace Telegrams
{
    [Serializable]
    // Place comparison
    [TelegramSettingsAttr(ACK = true, Type = 1213, Length = 48, ValidFor = "CRANE;TRANSPORT")]
    public class TelegramComparison : Telegram
    {
        public const int ORDER_MFCSINFO = 1;
        public const int ORDER_MFCSSET = 2;
        public const int ORDER_MFCSDELETE = 3;
        public const int ORDER_MFCSCHANGE = 4;

        public const int CONFIRMATION_NONE = 0;
        public const int CONFITMATION_OK = 101;

        public const int FAULT_NONE = 0;
        public const int FAULT_OK = 0;
        public const int FAULT_TELEGRAMSYNTAX = 101;
        public const int FAULT_SENDER = 102;
        public const int FAULT_RECEIVER = 103;
        public const int FAULT_TYPE = 104;
        public const int FAULT_ORDER = 201;
        public const int FAULT_LOCATION = 202;
        public const int FAULT_CONFIRMATION = 204;
        public const int FAULT_FAULT = 205;
        public const int FAULT_PALETTE = 206;
        public const int FAULT_PALETTEPRESENT = 207;
        public const int FAULT_PALETTENOTPRESENT = 208;
        public const int FAULT_CREATENOTPOSSIBLE = 209;
        public const int FAULT_MOVEPALETTE = 210;


        [Int32ValueFieldAttr(Offset = 16)]
        public Int32 MFCS_ID { get; set; }
        [Int16ValueFieldAttr(Offset = 20)]
        public Int16 Order { get; set; }
        [Int16ValueFieldAttr(Offset = 22)]
        public Int16 CraneID { get; set; }

        [ClassFieldOffset(Offset = 24)]
        public Position Position { get; set; }
        [ClassFieldOffset(Offset = 32)]
        public Palette Palette { get; set; }
        [Int16ValueFieldAttr(Offset = 44)]
        public Int16 Confirmation { get; set; }
        [Int16ValueFieldAttr(Offset = 46)]
        public Int16 Fault { get; set; }

        protected TelegramComparison(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public TelegramComparison() : base() { }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
