using System;
using System.Runtime.Serialization;

namespace Database
{
    [Serializable]
    public class SimpleCommandException : Exception
    {
        public SimpleCommandException(string s) : base()
        { }
    }

    [DataContract]
    public partial class SimpleCommand
    {
        public enum EnumTask { Move=11, Pick, Drop, Delete = 97, Create, Cancel, Reset, Info, AutoOn, AutoOff };
        public enum EnumStatus { NotInDB = -1,  NotActive=0, Written, InPlc, Canceled, Finished}
        public enum EnumReason : int
        {
            OK = 0,
            Command = 1,
            MFCS = 2,
            Warehouse = 3
        }
        public abstract string ToSmallString();

    }



    
}
