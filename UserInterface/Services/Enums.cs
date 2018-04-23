using System.ComponentModel;

namespace UserInterface.Services
{
    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumAlarmSeverity { Info = 0, Warning, Error }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumAlarmStatus { None = 0, Active, Ack, Removed }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumEventSeverity { Event = 0, Error }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumEventType { Material = 0, Command, Program, Exception, WMS }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumSimpleCommandTask { Move = 11, Pick, Drop, Delete = 97, Create, Cancel, Reset=100, Info, AutoOn, AutoOff };

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumSimpleCommandConveyorTask { Move = 11, Delete = 97, Create};

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumSimpleCommandCraneTask { Move = 11, Pick, Drop, Delete = 97, Create, Cancel};

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumSimpleCommandSegmentTask { Reset = 100, Info, AutoOn, AutoOff };

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumSimpleCommandStatus { NotActive = 0, Written, InPlc, Canceled, Finished }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumCommandStatus : int
    {
        Waiting = 0,
        Active = 1,
        Canceled = 2,
        Finished = 3,
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumWMSOrderStatus : int
    {
        Waiting = 0,
        Active = 1,
        OnTarget = 2,
        ReadyToTake = 3,
        Cancel = 4,
        Finished = 5
    }
    public enum EnumCommandWMSStatus : int
    {
        Waiting = 0,
        Active = 1,
        Canceled = 2,
        Finished = 3,
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumCommandERPStatus : int
    {
        Waiting = 0,
        Active = 1,
        Canceled = 2,
        Finished = 3,
        Error = 4
    }
    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumBlockedWMS : int
    {
        Available = 0,
        Rack = 1,
        Vehicle = 2,
        VehicleRack = 3,
        Quality = 4,
        QualityRack = 5,
        QualityVehicle = 6,
        QualityVehicleRack = 7
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumLogWMS: int
    {
        Event = 0,
        Exception = 1
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumCommandTask : int
    {
        Move = 0,
        CreateMaterial = 1,
        DeleteMaterial = 2,
        InfoMaterial = 3,
        InfoSlot = 4,
        SegmentInfo = 10,
        SegmentOn = 11,
        SegmentOff = 12,
        SegmentReset = 13,
        SegmentHome = 14,
        CancelCommand = 100,
        InfoCommand = 101
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumCommandTUTask : int
    {
        Move = 0,
        CreateMaterial = 1,
        DeleteMaterial = 2,
        InfoMaterial = 3
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumCommandSegmentTask : int
    {
        SegmentInfo = 10,
        SegmentOn = 11,
        SegmentOff = 12,
        SegmentReset = 13,
        SegmentHome = 14
    }
    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumCommandCommandTask : int
    {
        CancelCommand = 100,
        InfoCommand = 101
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumMovementTask : int
    {
        Create = 1,
        Move = 2,
        Delete = 3
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumCommandReason : int
    {
        OK = 0,
        PLC = 1,
        MFCS = 2,
        Operator = 3,
        LocationEmpty = 4,
        LocationFull = 5
    }

    [TypeConverter(typeof(LocalizedEnumConverter))]
    public enum EnumUserAccessLevel { Operator = 0, SuperUser, Admin };
}
