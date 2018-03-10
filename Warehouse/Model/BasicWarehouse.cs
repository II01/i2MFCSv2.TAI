using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Configuration;
using Database;
using Warehouse.ConveyorUnits;
using Warehouse.Strategy;
using System.Windows.Threading;
using System.Windows.Data;
using Warehouse.SteeringInput;
using Warehouse.Common;
using Warehouse.DataService;
using Warehouse.WCF;
using Warehouse.WMS;
using MFCS.Communication;
using System.Diagnostics;
using SimpleLog;
using System.Threading;
using Telegrams;
using System.Windows.Documents;

namespace Warehouse.Model
{

    [Serializable]
    public class BasicWarehouseException : Exception
    {
        public BasicWarehouseException(string s) : base(s)
        { }
    }

    [XmlInclude(typeof(MainPanel))]
    [XmlInclude(typeof(ALARM))]
    [XmlInclude(typeof(Sensor))]
    [XmlInclude(typeof(State))]
    [XmlInclude(typeof(LinkedConveyor))]
    [XmlInclude(typeof(StrategyDoubleForkCrane))]
    [XmlInclude(typeof(StrategyCrane))]
    [XmlInclude(typeof(StrategyGeneral))]
    [XmlInclude(typeof(ConveyorIO))]
    [XmlInclude(typeof(ConveyorJunction))]
    [XmlInclude(typeof(ConveyorOutput))]
    [XmlInclude(typeof(ConveyorIOAndOutput))]
    [XmlInclude(typeof(ConveyorOutputDefault))]
    [XmlInclude(typeof(SegmentCrane))]
    [XmlInclude(typeof(SegmentConveyor))]
    [XmlInclude(typeof(SegmentMainPanel))]
    [XmlInclude(typeof(Communicator))]
    [XmlInclude(typeof(BasicCommunicator))]
    [XmlInclude(typeof(Communicator))]
    [XmlInclude(typeof(EmptyCommunicator))]
    public partial class BasicWarehouse : IEventLog, IDisposable
    {

        public const int MAX_EVENTSTRLEN = 290;

        //       [XmlIgnore]
        //       public Log Log { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        public bool StrategyActive { get; set; }
        public bool TxtLog { get; set; }
        [XmlIgnore]
        public DBService DBService { get; set; }

        // Lists are mode only because of XML Serialization. 
        public List<BasicCommunicator> CommunicatorList { get; set; }
        public List<Segment> SegmentList { get; set; }
        public List<Crane> CraneList { get; set; }
        public List<Conveyor> ConveyorList { get; set; }
        public List<BasicStrategy> StrategyList { get; set; }
        [XmlIgnore]
        public SteeringCommands SteeringCommands { get; set; }

        [XmlIgnore]
        public Dictionary<string, BasicCommunicator> Communicator { get; private set; }
        [XmlIgnore]
        public Dictionary<string, Crane> Crane { get; private set; }
        [XmlIgnore]
        public Dictionary<string, Conveyor> Conveyor { get; private set; }
        [XmlIgnore]
        public Dictionary<string, Segment> Segment { get; private set; }



        [XmlIgnore]
        public List<Action <DateTime, Event.EnumSeverity, Event.EnumType, string>> OnNewEvent { get; set; }
        [XmlIgnore]
        public Action<Command> OnCommandFinish { get; set; }
        [XmlIgnore]
        public Action<Place, Database.EnumMovementTask> OnMaterialMove { get; set; }

        [XmlIgnore]
        public WCFHost WCFHost { get; set; }
        [XmlIgnore]
        public WCFBasicClient WCFClient { get; set; }
        private object _lock;

        public BasicWMS WMS { get; set; }

        public BasicWarehouse()
        {
            SteeringCommands = new SteeringCommands();
            OnNewEvent = new List<Action<DateTime, Event.EnumSeverity, Event.EnumType, string>>();
            _lock = new object();
            BindingOperations.EnableCollectionSynchronization(OnNewEvent, _lock);
        }

        public ConveyorBasic FindDeviceByPLC_ID (int plc_id)
        {
            ConveyorBasic cb = null;
            cb = ConveyorList.Find(p => p.PLC_ID == plc_id);
            if (cb == null)
                cb = CraneList.Find(p => p.PLC_ID == plc_id);
            if (cb == null)
                throw new BasicWarehouseException(String.Format("BasicWarehouse.FindDeviceByPLC_ID plc_id({0}) does not exist.", plc_id));
            return cb;
        }

        public void DeleteMaterial(UInt32 material, string place, int? mfcs_id)
        {
            try
            {
                SimpleCommand c = null;

                DBService.FindMaterialID((int)material, true);
                Place pm = DBService.FindMaterial((int)material);
                if (pm == null)
                {
                    if (mfcs_id.HasValue)
                    {
                        Command cmd = DBService.FindCommandByID(mfcs_id.Value);
                        cmd.Reason = Command.EnumCommandReason.LocationEmpty;
                        cmd.Status = Command.EnumCommandStatus.Canceled;
                        DBService.UpdateCommand(cmd);
                        OnCommandFinish?.Invoke(cmd);
                    }
                }
                else
                {
                    LPosition loc = LPosition.FromString(place);
                    if (!loc.IsWarehouse())
                    {
                        ConveyorBasic cb = FindConveyorBasic(place);
                        if (cb is Conveyor)
                            DBService.AddSimpleCommand(c = new SimpleConveyorCommand
                            {
                                Command_ID = mfcs_id,
                                Material = (int)material,
                                Source = place,
                                Status = SimpleCommand.EnumStatus.NotActive,
                                Target = place,
                                Task = SimpleCommand.EnumTask.Delete,
                                Time = DateTime.Now
                            });
                        else if (cb is Crane)
                            DBService.AddSimpleCommand(c = new SimpleCraneCommand
                            {
                                Command_ID = mfcs_id,
                                Material = (int)material,
                                Source = place,
                                Status = SimpleCommand.EnumStatus.NotActive,
                                Task = SimpleCommand.EnumTask.Delete,
                                Time = DateTime.Now,
                                Unit = cb.Name
                            });
                    }
                    else
                    {
                        DBService.MaterialDelete(pm.Place1, pm.Material);
                        OnMaterialMove?.Invoke(new Place { Place1 = place, Material = (int)material }, EnumMovementTask.Delete);
                        if (mfcs_id.HasValue)
                        {
                            Command cmd = DBService.FindCommandByID(mfcs_id.Value);
                            cmd.Status = Command.EnumCommandStatus.Finished;
                            DBService.UpdateCommand(cmd);
                            OnCommandFinish?.Invoke(cmd);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("BasicWarehouse.CreateMaterial material({0}),place({1}) failed. Reason :{2}", material, place, ex.Message));
            }

        }

        public void CommandMaterialAdd(CommandMaterial cmd)
        {
            try
            {
                DBService.AddCommand(cmd);
                if (cmd.Task == Command.EnumCommandTask.CreateMaterial)
                    CreateMaterial((uint)cmd.Material.Value, cmd.Source, cmd.ID);
                if (cmd.Task == Command.EnumCommandTask.DeleteMaterial)
                    DeleteMaterial((uint)cmd.Material.Value, cmd.Source, cmd.ID);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public void CreateMaterial(UInt32 material, string place, int? mfcs_id )
        {
            try
            {
                SimpleCommand c = null;

                DBService.FindMaterialID((int)material, true);
                Place pm = DBService.FindMaterial((int)material);
                Place pp = DBService.FindPlace(place);
                PlaceID pid = DBService.FindPlaceID(place);

                if (pm != null) // material exists
                {
                    if (mfcs_id.HasValue) 
                    {
                        Command cmd = DBService.FindCommandByID(mfcs_id.Value);
                        cmd.Reason = Command.EnumCommandReason.MFCS;
                        cmd.Status = Command.EnumCommandStatus.Canceled;
                        DBService.UpdateCommand(cmd);
                        OnCommandFinish?.Invoke(cmd);
                    }
                }
                else if (pp != null && pid.Size != 999) // place is full
                {
                    if (mfcs_id.HasValue)
                    {
                        Command cmd = DBService.FindCommandByID(mfcs_id.Value);
                        cmd.Reason = Command.EnumCommandReason.LocationFull;
                        cmd.Status = Command.EnumCommandStatus.Canceled;
                        DBService.UpdateCommand(cmd);
                        OnCommandFinish?.Invoke(cmd);
                    }
                }
                else
                {
                    LPosition loc = LPosition.FromString(place);
                    if (!loc.IsWarehouse())
                    {
                        ConveyorBasic cb = FindConveyorBasic(place);
                        if (cb is Conveyor)
                            DBService.AddSimpleCommand(c = new SimpleConveyorCommand
                            {
                                Command_ID = mfcs_id,
                                Material = (int)material,
                                Source = place,
                                Status = SimpleCommand.EnumStatus.NotActive,
                                Target = place,
                                Task = SimpleCommand.EnumTask.Create,
                                Time = DateTime.Now
                            });
                        else if (cb is Crane)
                            DBService.AddSimpleCommand(c = new SimpleCraneCommand
                            {
                                Command_ID = mfcs_id,
                                Material = (int)material,
                                Source = place,
                                Status = SimpleCommand.EnumStatus.NotActive,
                                Task = SimpleCommand.EnumTask.Create,
                                Time = DateTime.Now,
                                Unit = cb.Name
                            });
                    }
                    else
                    {
                        DBService.MaterialCreate(place, (int)material, true);
                        OnMaterialMove?.Invoke(new Place { Place1 = place, Material = (int)material }, EnumMovementTask.Create);
                        if (mfcs_id.HasValue)
                        {
                            Command cmd = DBService.FindCommandByID(mfcs_id.Value);
                            cmd.Status = Command.EnumCommandStatus.Finished;
                            DBService.UpdateCommand(cmd);
                            OnCommandFinish?.Invoke(cmd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("BasicWarehouse.CreateMaterial material({0}),place({1}) failed. Reason :{2}", material, place, ex.Message));
            }
        }


        public void CallOnNewEvent(DateTime dt, Event.EnumSeverity s, Event.EnumType t, string text)
        {
            var l = new List<Action<DateTime, Event.EnumSeverity, Event.EnumType, string>>();
            foreach (var n in OnNewEvent)
                try
                {
                    n.Invoke(dt, s, t, text);
                }
                catch(Exception)
                {
                    l.Add(n);
                }
            l.ForEach(p => OnNewEvent.Remove(p));
        }

        public void AddEvent(Event.EnumSeverity s, Event.EnumType t, string text)
        {
            string str = text.Substring(0, text.Length > MAX_EVENTSTRLEN ? MAX_EVENTSTRLEN : text.Length);
            DateTime dt = DateTime.Now;
            try
            {
                CallOnNewEvent(dt, s, t, text);
            }
            catch { }
            if (TxtLog)
                Log.AddLog(s == Event.EnumSeverity.Error ? Log.Severity.EXCEPTION : Log.Severity.EVENT, "", "BasicWarehouse.AddEvent", text);
            else
                DBService?.AddEvent(s, t, str, dt);
        }


        private BasicCommunicator CheckCommunicator(string unit, string communicator)
        {
            if (communicator == null)
                throw new BasicWarehouseException(String.Format("{0} has CommunicatorName null.", unit));
            if (!Communicator.ContainsKey(communicator))
                throw new BasicWarehouseException(String.Format("{0} has unknown communicator {1}", unit, communicator ));
            return Communicator[communicator];
        }

        public IConveyorIO CheckForConveyorIO(string name)   
        {
            ConveyorBasic cb = FindConveyorBasic(name);
            if (!(cb is IConveyorIO))
                throw new BasicWarehouseException(String.Format("{0} is not IConveyorIO", name));
            return cb as IConveyorIO;
        }

        public ConveyorBasic FindConveyorBasic(string name)
        {
            if (Conveyor.ContainsKey(name))
                return Conveyor[name];
            else if (Crane.ContainsKey(name))
                return Crane[name];
            else
                throw new BasicWarehouseException(String.Format("FindConveyorBasic({0}) failed.", name));
        }

        public List<ConveyorBasic> AllConveyorBasic()
        {
            var res = new List<ConveyorBasic>();
            res.AddRange(CraneList);
            res.AddRange(ConveyorList);
            return res;
        }

        public void ConnectCraneInConveyor(ConveyorBasic tr, RouteNode node)
        {
            Crane c = node.Next as Crane;
            if (c.InConveyor == null)
                c.InConveyor = new List<ConveyorIO>();
            if (!(tr is ConveyorIO))
                throw new BasicWarehouseException(String.Format("{0} is connected to {1} but is not ConveyorIO", tr.Name, node.Next.Name));
            c.InConveyor.Add(tr as ConveyorIO);
        }

        public void Serialize(string configFile)
        {
            AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("Serializing basic warehose from {0}.", configFile));
            // try to serialize to XML
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BasicWarehouse));
                using (TextWriter writer = new StreamWriter(configFile))
                {
                    writer.Flush();
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception e)
            {
                AddEvent(Event.EnumSeverity.Error, Event.EnumType.Program, String.Format("Serializing basic warehose error from {0}", configFile));
                throw new BasicWarehouseException(String.Format("{0} Serialize fault {1}", configFile, e.Message));
            }
        }

        public static BasicWarehouse Deserialize(string configFile)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BasicWarehouse));
                using (TextReader reader = new StreamReader(configFile))
                {
                    var wh = (BasicWarehouse)serializer.Deserialize(reader);
                    Log.AddLog(Log.Severity.EVENT, "Warehouse", $"Deserialize warehose from {configFile} finished.");
                    return wh;
                }
            }
            catch (Exception e)
            {
                Log.AddLog(Log.Severity.EXCEPTION, "Warehouse", $"Deserializing basic warehose from {configFile} error {e.Message}");
                throw new BasicWarehouseException(String.Format("{0} Deserialize fault {1}", configFile, e.Message));
            }
        }


        public void StartCommunication()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            CommunicatorList.RunCommunication(ct);
        }

        public void Dispose()
        {
            try
            {
                WCFHost?.Stop();
                WCFClient?.Dispose();
            }
            catch
            { }
        }



        public virtual void Initialize()
        {

            try
            {
                // Log = new Log(ConfigurationManager.AppSettings["txtlog"], Convert.ToBoolean(ConfigurationManager.AppSettings["logtofile"]));
                // initialize Communicators
                Communicator = CommunicatorList?.ToDictionary((p) => p.Name);

                Crane = CraneList?.ToDictionary((p) => p.Name);

                Conveyor = ConveyorList?.ToDictionary((p) => p.Name);

                Segment = SegmentList?.ToDictionary((p) => p.Name);

                // initialize ConveyorUnits
                ConveyorList?.ForEach(p => p.Initialize(this));
                SegmentList?.ForEach(p => p.Initialize(this));
                CraneList?.ForEach(p => p.Initialize(this));
                CommunicatorList?.ForEach(prop => prop.Initialize(this));
                StrategyList?.ForEach(p => p.Initialize(this));
                SteeringCommands?.Initialize(this);
                

                ConveyorList?.ForEach(prop => prop.Startup());
                CraneList?.ForEach(prop => prop.Startup());
                SteeringCommands?.Startup();

                WMS?.Initialize(this);

            }
            catch (Exception ex)
            {
                AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("{0} BasicWarehouse.Initialize failed", Name));
            }
        }

        public void TestFillRack(string rack, int num)
        {
            if (rack == null || rack.Length == 0)
                rack = "W:";
            DBService.TestFillReck(rack, num);
        }

        public void TestToOut(string rack, int num)
        {
            if (rack == null || rack.Length == 0)
                rack = "W:";
            DBService.TestToOut(rack, num);
        }
        public void TestToIn(int bcrID, string rack, int num)
        {
            if (rack==null || rack.Length == 0)
                rack = "W:";
            DBService.TestToIn(bcrID, rack, num);
        }
    }
}
