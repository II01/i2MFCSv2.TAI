using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Telegrams
{

    [Serializable]
    public class TelegramException : Exception
    {
        public TelegramException(string message) : base(message)
        {
        }
    }
 
    public abstract class ValueFieldAttr : System.Attribute
    {
        public int Offset {get;set;}
        public abstract void WriteToByteBuffer(byte[] buffer, object value, int baseOffset);
        public abstract object ReadFromByteBuffer(byte[] buffer, int baseOffset);
        public virtual string ToSpecialStringFormat(object o)
        {
            return o.ToString();
        }
    }

    public class ClassFieldOffset : ValueFieldAttr
    {
        public override object ReadFromByteBuffer(byte[] buffer, int baseOffset)
        {
            throw new NotImplementedException();
        }
        public override void WriteToByteBuffer(byte[] buffer, object value, int baseOffset)
        {
            throw new NotImplementedException();
        }
    }

    public class ByteValueFieldAttr : ValueFieldAttr 
    {
        public override void  WriteToByteBuffer( byte[] buffer, object value, int baseOffset)
        {
            buffer[baseOffset+Offset] = (byte) value;
        }

        public override object ReadFromByteBuffer(byte[] buffer, int baseOffset)
        {
            return buffer[baseOffset+Offset];
        }

    }

    public class BitArrayFieldAttr : ValueFieldAttr
    {
        public int Length { get; set; }   // length in bytes
        public override void WriteToByteBuffer(byte[] buffer, object value, int baseOffset)
        {
            BitArray ba = value as BitArray;
            if (value is BitArray)
                ((BitArray)value).CopyTo(buffer, baseOffset + Offset);
        }

        public override object ReadFromByteBuffer(byte[] buffer, int baseOffset)
        {

            byte[] b = new byte[Length];
            Array.Copy(buffer, baseOffset + Offset, b, 0, Length);
            return new BitArray(b);
        }

        public override string ToSpecialStringFormat(object o)
        {
            BitArray ba = o as BitArray;
            bool first = true;
            StringBuilder sb = new StringBuilder();
            foreach (bool b in ba)
            {
                if (first)
                {
                    first = false;
                    sb.Append('[');
                }
                else
                    sb.Append('|');
                sb.Append(b ? '1' : '0' );
            }
            sb.Append(']');
            return sb.ToString();
        }
    }

    public class Int16ValueFieldAttr : ValueFieldAttr
    {
        public override void WriteToByteBuffer(byte[] buffer, object value, int baseOffset)
        {
            buffer[baseOffset+Offset] = (byte)  (((System.Int16) value) >> 8);
            buffer[baseOffset+Offset + 1] = (byte) (((System.Int16) value) % 256);
        }

        public override object ReadFromByteBuffer(byte[] buffer, int baseOffset)
        {
            return (System.Int16) (256 * buffer[baseOffset+Offset] + buffer[baseOffset+Offset + 1]);
        }
    }

    public class UInt16ValueFieldAttr : ValueFieldAttr
    {
        public override void WriteToByteBuffer(byte[] buffer, object value, int baseOffset)
        {
            buffer[baseOffset+Offset] = (byte)(((System.UInt16)value) >> 8);
            buffer[baseOffset+Offset + 1] = (byte)(((System.UInt16)value) % 256);
        }

        public override object ReadFromByteBuffer(byte[] buffer, int baseOffset)
        {
            return (System.UInt16)(256 * buffer[baseOffset+Offset] + buffer[baseOffset+Offset + 1]);
        }
    }

    public class UInt32ValueFieldAttr : ValueFieldAttr
    {
        public override void WriteToByteBuffer(byte[] buffer, object value, int baseOffset)
        {
            buffer[baseOffset+Offset] = (byte)(((System.UInt32)value) >> 24 % 256);
            buffer[baseOffset+Offset + 1] = (byte)(((System.UInt32)value) >> 16 % 256);
            buffer[baseOffset+Offset + 2] = (byte)(((System.UInt32)value) >> 8 % 256);
            buffer[baseOffset+Offset + 3] = (byte)(((System.UInt32)value) % 256);
        }

        public override object ReadFromByteBuffer(byte[] buffer, int baseOffset)
        {
            return ((UInt32)(buffer[baseOffset + Offset]) << 24) +
                   ((UInt32)(buffer[baseOffset + Offset + 1]) << 16) +
                   ((UInt32)(buffer[baseOffset + Offset + 2]) << 8) +
                   ((UInt32)(buffer[baseOffset + Offset + 3]));
        }
    }

    public class Int32ValueFieldAttr : ValueFieldAttr
    {
        public override void WriteToByteBuffer(byte[] buffer, object value, int baseOffset)
        {
            buffer[baseOffset+Offset] = (byte)((((Int32)value) >> 24) % 256);

            buffer[baseOffset+Offset + 1] = (byte)((((Int32)value) >> 16) % 256);
            buffer[baseOffset+Offset + 2] = (byte)((((Int32)value) >> 8) % 256);
            buffer[baseOffset+Offset + 3] = (byte)(((Int32)value) % 256);
        }

        public override object ReadFromByteBuffer(byte[] buffer, int baseOffset)
        {
            return ((Int32)(buffer[baseOffset + Offset]) << 24) +
                   ((Int32)(buffer[baseOffset + Offset + 1]) << 16) +
                   ((Int32)(buffer[baseOffset + Offset + 2]) << 8) +
                   ((Int32)(buffer[baseOffset + Offset + 3]));
        }
    }


    public class StringFieldAttr : ValueFieldAttr
    {
        public int MaxLen { get; set; }
        public override void WriteToByteBuffer(byte[] buffer, object value, int baseOffset)
        {
            string s = (string)value;
            if (s.Length > MaxLen)
                s = s.Substring(0, MaxLen);
            ASCIIEncoding encoding = new ASCIIEncoding();
            if (s != null)
            {
                buffer[baseOffset + Offset] = (byte)MaxLen;
                buffer[baseOffset + Offset + 1] = (byte) s.Length;
                encoding.GetBytes(s).CopyTo(buffer, baseOffset + Offset + 2);
            }
            else
            {
                buffer[baseOffset + Offset] = 0; 
            }
        }

        public override object ReadFromByteBuffer(byte[] buffer, int baseOffset)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            return encoding.GetString(buffer, baseOffset + Offset + 2, buffer[baseOffset+Offset+1]);
        }
 
    }

 

    // basic class for telegrams
    public class TelegramBasic
    {
        public byte[] ByteBuffer { get; set;}

        public TelegramBasic()
        {
        }

        private void WriteBuffer( object obj, int baseOffset)
        {
            foreach (var pi in obj.GetType().GetProperties())
            {
                foreach (var ca in pi.GetCustomAttributes<ValueFieldAttr>())
                    if (ca is ClassFieldOffset)
                    {
                        if (pi.GetValue(this) != null)
                            WriteBuffer(pi.GetValue(obj), baseOffset + ca.Offset);
                    }
                    else
                        ca.WriteToByteBuffer(ByteBuffer, pi.GetValue(obj), baseOffset);
            }
        }

        public void WriteBuffer()
        {
            WriteBuffer(this, 0);
        }

        private void ReadBuffer( Object obj, int baseOffset)
        {
            foreach (var pi in obj.GetType().GetProperties())
            {
                foreach (var ca in pi.GetCustomAttributes<ValueFieldAttr>())
                    if (ca is ClassFieldOffset)
                    {                        
                        object o = Activator.CreateInstance(pi.PropertyType);
                        pi.SetValue(obj, o);
                        ReadBuffer(o, baseOffset + ca.Offset);
                    }
                    else
                        pi.SetValue(obj, ((ValueFieldAttr)ca).ReadFromByteBuffer(ByteBuffer, baseOffset));
            }
        }

        public void ReadBuffer()
        {
            ReadBuffer(this, 0);
        }

        public string ToDirectString( object obj, int baseOffset)
        {
            StringBuilder sb = new StringBuilder();
            var linq = (from p in obj.GetType().GetProperties()
                        where p.GetCustomAttributes<ValueFieldAttr>().Any()
                        orderby p.GetCustomAttribute<ValueFieldAttr>().Offset
                        select p).ToList();
            foreach (var pi in linq)
            {
                if (pi.GetCustomAttributes<ClassFieldOffset>().Any())
                {
                    if (pi.GetValue(obj) == null)
                        pi.SetValue(obj, Activator.CreateInstance(pi.PropertyType));
                    sb.AppendFormat(ToDirectString(pi.GetValue(obj), baseOffset + pi.GetCustomAttribute<ClassFieldOffset>().Offset));
                }
                else if (pi.GetCustomAttributes<ValueFieldAttr>().Any())
                    sb.AppendFormat("|{0}", pi.GetCustomAttribute<ValueFieldAttr>().ToSpecialStringFormat(pi.GetValue(obj)));
            }
            return sb.ToString();
        }

        public string ToNameString(object obj, int baseOffset, string name)
        {
            var linq = (from p in obj.GetType().GetProperties()
                        where p.GetCustomAttributes<ValueFieldAttr>().Any()
                        orderby p.GetCustomAttribute<ValueFieldAttr>().Offset
                        select p).ToList();

            StringBuilder sb = new StringBuilder();
            foreach (var pi in linq)
            {
                if (pi.GetCustomAttributes<ClassFieldOffset>().Any())
                {
                    if (pi.GetValue(obj) == null)
                        pi.SetValue(obj, Activator.CreateInstance(pi.PropertyType));
                    sb.AppendFormat(ToNameString(pi.GetValue(obj), baseOffset + pi.GetCustomAttribute<ClassFieldOffset>().Offset, String.Format("{0}.{1}", name, pi.Name)));
                }
                else if (pi.GetCustomAttributes<ValueFieldAttr>().Any())
                    sb.AppendFormat("|{0}={1}", String.Format("{0}.{1}",name, pi.Name), pi.GetCustomAttribute<ValueFieldAttr>().ToSpecialStringFormat(pi.GetValue(obj)));
            }
            return sb.ToString(); 
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().FullName);
            sb.AppendLine(ToDirectString(this, 0));
            sb.AppendLine(ToNameString(this, 0, "T"));

            return sb.ToString();
        }

    }


    public abstract class ValidateAttr : Attribute
    {
        public object Value { get; set;}
        public abstract bool Validate(object value);
        public abstract void Build(object value);
    }

    public class FixedByteValidateAttr : ValidateAttr
    {
        public override bool Validate(object value)
        {
            return (byte) Value == (byte) value;
        }
        public override void Build(object value)
        {
        }
    }

    public class HeaderAttr : Attribute
    {
        public int Length {get;set;}
    }

    // Header for all telegrams
    [HeaderAttr(Length=14)]
    public class TelegramHeader : TelegramBasic
    {
        [FixedByteValidateAttr(Value=(byte) 10)]
        [ByteValueFieldAttr(Offset=0)]
        public byte StartByte { get; set; }
        [FixedByteValidateAttr(Value =(byte) 11)]
        [ByteValueFieldAttr(Offset = 1)]
        public byte SecondByte { get; set; }
        [Int16ValueFieldAttr(Offset = 2)]
        public Int16 Sequence { get; set; }
        [Int16ValueFieldAttr(Offset = 4)]
        public Int16 Sender { get; set; }
        [Int16ValueFieldAttr(Offset = 6)]
        public Int16 Receiver { get; set; }
        [Int16ValueFieldAttr(Offset = 8)]
        public Int16 TelType { get; set; }
        [Int16ValueFieldAttr(Offset = 10)]
        public Int16 Length { get; set; }
        [UInt16ValueFieldAttr(Offset = 12)]
        public UInt16 TelCode { get; set; }
        [UInt16ValueFieldAttr(Offset=14)]
        public UInt16 CRC { get; set; }

        public int HeaderLength()
        {
            return this.GetType().GetCustomAttribute<HeaderAttr>().Length;
        }

        public bool CheckCRC()
        {
            UInt16 crc = 0;
            for (int i=0;i<Length/2;i++)
                crc ^= (UInt16) ( 256 * ByteBuffer[i * 2] + ByteBuffer[i * 2 + 1]);
            return crc == 0; 
        }

        public void SetCRC()
        {
            UInt16 crc = 0;
            CRC = 0;
            WriteBuffer();
            for (int i = 0; i < Length / 2; i++)
                crc ^= (UInt16)( 256 * ByteBuffer[i * 2] + ByteBuffer[i * 2 + 1]);
            CRC = crc;

            WriteBuffer();
        }
    }

 
}