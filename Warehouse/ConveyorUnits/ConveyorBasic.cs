using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Telegrams;
using Database;
using Warehouse.Model;
using Warehouse.Common;
using System.Collections;
using MFCS.Communication;
using System.Runtime.Serialization;
using System.Windows.Data;
using Warehouse.ConveyorUnits;

namespace Warehouse.ConveyorUnits
{
    public enum WriteToDB { Never = 0, Try, Always };

    public class FLocation
    {
        [XmlAttribute]
        public int X { get; set; }
        [XmlAttribute]
        public int Y { get; set; }
        [XmlAttribute]
        public int Z { get; set; }

        public static FLocation operator +(FLocation a, FLocation b )
        {
            return new FLocation { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z };
        }

        public static FLocation operator -(FLocation a, FLocation b)
        {
            return new FLocation { X = a.X - b.X, Y = a.Y - b.Y, Z = a.Z - b.Z };
        }

        public double Distance()
        {
            return Math.Sqrt(1 * X * X + 0 * Y * Y + 0 * Z * Z);
        }
    }



    [Serializable]
    public class ConveyorBasicException : Exception
    {
        public ConveyorBasicException(string s) : base(s)
        { }
    }

    public abstract class ConveyorBasic
    {

        [XmlIgnore]
        public bool InitialNotified { get; set; }
        [XmlIgnore]
        public RouteNode Route { get; set; }

        public XmlRouteNode XmlRouteNode { get; set; }

        [XmlIgnore]
        public BasicWarehouse Warehouse { get; set; }
        [XmlIgnore]
        public PlaceID PlaceID { get; set; }
        [XmlIgnore]
        public Place Place { get; set; }

        [XmlAttribute]
        public string Segment { get; set; }

        public short ConveyorAddress { get; set; }

        public FLocation FLocation { get; set; }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public Int16 PLC_ID { get; set; }

        [XmlAttribute]
        public WriteToDB WriteToDB { get; set; }

        [XmlIgnore]
        public BasicCommunicator Communicator { get; set; }

        [XmlAttribute]
        public string CommunicatorName { get; set; }

        [XmlIgnore]
        public List<Action<ConveyorBasicInfo>> NotifyVM { get; set; }
        private object _lock;

        public ConveyorBasic()
        {
            NotifyVM = new List<Action<ConveyorBasicInfo>>();
            _lock = new object();
            BindingOperations.EnableCollectionSynchronization(NotifyVM, _lock);
        }


        public abstract bool Automatic();
        public abstract bool Remote();
        public abstract bool LongTermBlock();

        public abstract bool CheckIfAllNotified();

        public abstract void OnReceiveTelegram(Telegram t);

        public virtual void Startup()
        {
        }

        public virtual bool Online()
        {
            return Communicator.Online();
        }

        public virtual void Initialize(BasicWarehouse w)
        {
            try
            {
                Warehouse = w;
                Communicator = Warehouse.Communicator[CommunicatorName];
                PlaceID = Warehouse.DBService.FindPlaceID(Name);
                Place = Warehouse.DBService.FindPlace(Name);
                if (PlaceID == null)
                    throw new ConveyorBasicException(String.Format("ConveyorBasic.Startup {0} does not exist in dbo.[PlaceID]", Name));
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorBasicException(String.Format("{0} ConveyorBasic.InitializePlace failed", Name));
            }
        }


        public void OnSimpleCommandFinish(SimpleCommand cmd)
        {
            try
            {
                // automatic mode
                if (cmd.Command_ID.HasValue)
                {
                    Command Command = Warehouse.DBService.FindCommandByID(cmd.Command_ID.Value);
                    if (Command == null)
                        throw new ConveyorBasicException(String.Format("{0} has no corresponding Command", cmd != null ? cmd.ToString() : "null"));
                    if (Command.Task == Database.Command.EnumCommandTask.Move)
                    {
                        Place p = Warehouse.DBService.FindMaterial((Command as CommandMaterial).Material.Value);
                        if (p != null && p.Place1 == (Command as CommandMaterial).Target)
                        {
                            Command.Status = Database.Command.EnumCommandStatus.Finished;
                            Warehouse.DBService.UpdateCommand(Command);
                            Warehouse.OnCommandFinish?.Invoke(Command);
                        }
                        else
                        {
                            Conveyor conv = Warehouse.ConveyorList.FirstOrDefault(c => c.Name == (Command as CommandMaterial).Target);
                            if (conv != null && conv is ConveyorOutputDefault)
                            {
                                Command.Status = Database.Command.EnumCommandStatus.Finished;
                                Warehouse.DBService.UpdateCommand(Command);
                                Warehouse.OnCommandFinish?.Invoke(Command);
                            }
                        }
                    }
                    else if (cmd.Status == SimpleCommand.EnumStatus.Finished && Warehouse.DBService.AllSimpleCommandWithCommandIDFinished(cmd.Command_ID.Value))
                    {
                        Command.Status = Database.Command.EnumCommandStatus.Finished;
                        Warehouse.DBService.UpdateCommand(Command);
                        Warehouse.OnCommandFinish?.Invoke(Command);
                    }
                    else if (cmd.Status == SimpleCommand.EnumStatus.Canceled &&
                             (cmd.Task == SimpleCommand.EnumTask.Create || cmd.Task == SimpleCommand.EnumTask.Delete ||
                              (cmd.Task == SimpleCommand.EnumTask.Move && Command.Task == Command.EnumCommandTask.SegmentHome)))
                    {
                        if ((int)cmd.Reason > 100)
                            Command.Reason = Command.EnumCommandReason.PLC;
                        else
                            Command.Reason = Command.EnumCommandReason.MFCS;
                        Command.Status = Database.Command.EnumCommandStatus.Canceled;
                        Warehouse.DBService.UpdateCommand(Command);
                        Warehouse.OnCommandFinish?.Invoke(Command);
                    }

                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorBasicException(String.Format("{0} ConveyorBasic.OnSimpleCommandFinish ({1}) fault.", Name, cmd != null ? cmd.ToString() : "null"));
            }
        }

        public virtual void InitialNotify(Telegram t, UInt32 material)
        {
            try
            {
                //                InitialNotified = true;
                //                Warehouse.DBService.InitialNotify(Name, (int) material);
                //                Place = Warehouse.DBService.FindPlace(Name);
                InitialNotified = true;
                Place p = Warehouse.DBService.FindMaterial((int)material);
                if (WriteToDB == WriteToDB.Always ||
                    (WriteToDB == WriteToDB.Try && (p == null || p.Place1 == Name)))
                {
                    Warehouse.DBService.InitialNotify(Name, (int)material);
                    Place = Warehouse.DBService.FindPlace(Name);
                    if (Place != null)
                        Warehouse.OnMaterialMove?.Invoke(Place, EnumMovementTask.Move);
                }
                else if (material != 0)
                {
                    Place = new Place { Material = (int)material, Place1 = Name };
                    Warehouse.OnMaterialMove?.Invoke(Place, EnumMovementTask.Move);
                }
                if (this is ConveyorJunction)
                {
                    ConveyorJunction jc = this as ConveyorJunction;
                    TelegramTransportTO to = t as TelegramTransportTO;

                    if (to != null && jc.ActiveRoute != null && (to.Target != jc.ActiveRoute.Items[0].Final.PLC_ID))
                    {
                        jc.ActiveRoute = null;
                        jc.ActiveMaterial = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorBasicException(String.Format("{0} ConveyorBasic.InitialNotify material({1}) fault.", Name, material));
            }
        }

        public void Move(UInt32 material, ConveyorBasic source, ConveyorBasic target)
        {
            // for input points
            try
            {

                if (source == target)
                    return;

                if (source == null)
                    throw new ConveyorBasicException(String.Format("ConveyorBasic.Move source is null ({0}).", source.Name));

                if (target == null)
                    throw new ConveyorBasicException(String.Format("ConveyorBasic.Move target is null ({0}).", source.Name));

                if (source.Place == null)
                    throw new ConveyorBasicException(String.Format("ConveyorBasic.Move source({0}) is empty.", source.Name));

                if (target.Place != null)
                    throw new ConveyorBasicException(String.Format("ConveyorBasic.Move target ({0}) is not empty.", target.Name));

                if (source.Place.Material != material)
                    throw new ConveyorBasicException(String.Format("ConveyorBasic.Move wrong pallet ({0}!={1}) at source ({2}).", material, source.Place.Material, source.Name));

                if (target is Conveyor && source is Conveyor)
                {
                    (target as Conveyor).Command = (source as Conveyor).Command;
                    (source as Conveyor).Command = null;
                }
                target.Place = source.Place;
                target.Place.Place1 = Name;
                source.Place = null;

                if (source is ConveyorJunction)
                {
                    (source as ConveyorJunction).ActiveRoute = null;
                    (source as ConveyorJunction).ActiveMaterial = null;
                }
                if (target is ConveyorJunction)
                {
                    (target as ConveyorJunction).ActiveRoute = null;
                    (target as ConveyorJunction).ActiveMaterial = null;
                }

                //                Warehouse.DBService.MaterialMove((int) material, source.Name, target.Name);
                Place ps = Warehouse.DBService.FindPlace(source.Name);
                Place pt = Warehouse.DBService.FindPlace(target.Name);
                if (target.WriteToDB == WriteToDB.Never)
                {
                    if (source.WriteToDB == WriteToDB.Always || (source.WriteToDB == WriteToDB.Try && ps != null))
                        Warehouse.DBService.MaterialDelete(source.Name, (int)material);
                }
                else if (target.WriteToDB == WriteToDB.Always || (target.WriteToDB == WriteToDB.Try && pt == null))
                {
                    if (source.WriteToDB == WriteToDB.Always || (source.WriteToDB == WriteToDB.Try && ps != null))
                        Warehouse.DBService.MaterialMove((int)material, source.Name, target.Name);
                    else
                        Warehouse.DBService.MaterialCreate(target.Name, (int)material, true);
                }

                Warehouse.OnMaterialMove?.Invoke(new Place { Place1 = target.Name, Material = (int)material }, EnumMovementTask.Move);

                // add force UI notify
                source.DirectVMNotify();
                target.DirectVMNotify();
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorBasicException(String.Format("{0} ConveyorBasic.Move fault ({1},{2},{3}).", Name, material, source != null ? source.Name : "null", target != null ? target.Name : "null"));
            }
        }


        public void MaterialDelete(UInt32 material)
        {
            try
            {
                if (material == 0)
                    throw new ConveyorBasicException("MaterialDelete(0)");
                if (Place == null)
                    throw new ConveyorBasicException(String.Format("{0} is empty already.", Name));
                if (Place.Material != material && material != 0)
                    throw new ConveyorBasicException(String.Format("{0} material mismatch ({1}!={2})", Name, Place.Material, material));

                //                Warehouse.DBService.MaterialDelete(Name, (int) material);
                if (WriteToDB != WriteToDB.Never)
                    Warehouse.DBService.MaterialDelete(Name, (int)material);
                Warehouse.OnMaterialMove?.Invoke(new Place { Place1 = Name, Material = (int)material }, EnumMovementTask.Delete);
                Place = null;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorBasicException(String.Format("{0} ConveyorBasic.MaterialDelete fault material({1}).", Name, material));
            }
        }


        public void MaterialCreate(UInt32 material)
        {
            try
            {

                if (Place != null && !(Place.Material == material))  // !(...) added to allo height class overwrite 
                    throw new ConveyorBasicException(String.Format("{0} is not empty.", Name));

                //                Warehouse.DBService.MaterialCreate( Name,  (int) material, true);
                //                Warehouse.OnMaterialMove?.Invoke(new Place { Place1 = Name, Material = (int)material }, EnumMovementTask.Create);
                //                Place = Warehouse.DBService.FindPlace(Name);
                Place p = Warehouse.DBService.FindMaterial((int)material);
                if (p != null && WriteToDB != WriteToDB.Never)
                {
                    string msg = string.Format("Create on place {0} not possible: pallet {1} exists in the system (place {2})", Name, material, p.Place1);
                    Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Material, msg);
                    if (WriteToDB == WriteToDB.Always)
                        throw new ConveyorBasicException(msg);
                }
                if (WriteToDB == WriteToDB.Always || (WriteToDB == WriteToDB.Try && p == null))
                {
                    Warehouse.DBService.MaterialCreate(Name, (int)material, true);
                    Warehouse.OnMaterialMove?.Invoke(new Place { Place1 = Name, Material = (int)material }, EnumMovementTask.Create);
                    Place = Warehouse.DBService.FindPlace(Name);
                }
                else
                {
                    Warehouse.OnMaterialMove?.Invoke(new Place { Place1 = Name, Material = (int)material }, EnumMovementTask.Create);
                    Place = new Place { Place1 = Name, Material = (int)material };
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorBasicException(String.Format("{0} ConveyorBasic.MaterialCreate fault material ({1}).", Name, material));
            }
        }

        public void CreateOrUpdateMaterialID(Palette pal)
        {
            try
            {
                if (pal != null)
                    Warehouse.DBService.CreateOrUpdateMaterialID((int)pal.Barcode, pal.Weight / 10000, pal.Weight % 10000);
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorBasicException(String.Format("{0} ConveyorBasic.CreateOrUpdateMaterialID fault material ({1}).", Name, pal.Barcode));
            }
        }

        public bool Compatible(string target)
        {
            try
            {
                LPosition pos = LPosition.FromString(target);
                if (pos.IsWarehouse())
                {
                    if (this is Crane)
                        return (this as Crane).Shelve.Contains((short)pos.Shelve);
                    else
                        return false;
                }
                else
                    return this.Name == target;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new CraneException(String.Format("{0} ConveyorJunction failed ({1})", Name, target));
            }
        }


        public abstract void FinishCommand(Int32 id, SimpleCommand cmd, SimpleCommand.EnumStatus s);

        public virtual void DirectVMNotify() { }

        public abstract void CreateAndSendTOTelegram(SimpleCommand cmd);

        public void CallNotifyVM(ConveyorBasicInfo cbi)
        {
            List<Action<ConveyorBasicInfo>> notActive = new List<Action<ConveyorBasicInfo>>();

            for (int i = 0; i < NotifyVM.Count; i++)
                try
                {
                    NotifyVM[i].Invoke(cbi); // , null, null);
                }
                catch (Exception)
                {
                    notActive.Add(NotifyVM[i]);
                }
            notActive.ForEach(p => NotifyVM.Remove(p));
        }

    }
}
