using Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warehouse.ConveyorUnits;
using Warehouse.Model;
using Warehouse.WMS;
using System.Xml.Serialization;


namespace Warehouse.Strategy
{

    [Serializable]

    public class StrategyGeneral : BasicStrategy
    {
        private DateTime _timeStampOld;
        private TimeSpan _maintenanceTime;
        private double _dataBaseSizeGBMax;
        private string _logPath;
        private int _logToKeepMonths;

        [XmlIgnore]
        public bool DatabaseToLarge { get; set; }
        public string CommunicatorName { get; set; }
        protected Task CurrentTask { get; set; }

        public StrategyGeneral(): base()
        {
        }

        public override void Initialize(BasicWarehouse w)
        {
            try
            {
                Warehouse = w;
                Warehouse.Communicator[CommunicatorName].OnRefresh += Refresh;
                _timeStampOld = DateTime.Now;
                _dataBaseSizeGBMax = double.Parse(System.Configuration.ConfigurationManager.AppSettings["DataBaseSizeGBMax"], System.Globalization.CultureInfo.InvariantCulture);
                try
                {
                    _maintenanceTime = DateTime.Parse(System.Configuration.ConfigurationManager.AppSettings["maintenanceTime"], System.Globalization.CultureInfo.InvariantCulture).TimeOfDay;
                    _logPath = System.Configuration.ConfigurationManager.AppSettings["logFolder"];
                    _logToKeepMonths = Math.Min(1, int.Parse(System.Configuration.ConfigurationManager.AppSettings["logToKeepMonths"]));
                }
                catch
                {
                    _maintenanceTime = DateTime.Parse("3:0:0", System.Globalization.CultureInfo.InstalledUICulture).TimeOfDay;
                    _logPath = "C:\\";
                    _logToKeepMonths = 3;
                }

                if (Warehouse.DBService.GetDBSizeInGB() > _dataBaseSizeGBMax)
                {
                    DatabaseToLarge = true;
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, "Database is approaching its maximum size. Create backup and delete old records in tables MOVEMENT, ALARM, EVENT, and COMMAND.");
                }
                else
                {
                    DatabaseToLarge = false;
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new StrategyDoubleForkCraneException(String.Format("{0} StrategyGeneral.Initialize failed", Name));
            }
        }
        public override void Refresh()
        {
            try
            {
                Strategy();
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("{0} Refresh failed", Name));
                Warehouse.SteeringCommands.Run = false;
            }
        }

        public void DeleteOldLogs(string path, int months)
        {
            string[] files = Directory.GetFiles(path);

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.LastAccessTime < DateTime.Now.AddMonths(-months))
                    fi.Delete();
            }
        }

        public async Task StrategyAsync()
        {
            try
            {
                Command cmd;
                DateTime timeStamp;

                // todo: timestamp: delete DB alarm, Log
                timeStamp = DateTime.Now;
                if(timeStamp.TimeOfDay > _maintenanceTime && _timeStampOld.TimeOfDay <= _maintenanceTime )
                {
                    // send time sync
                    Warehouse.SegmentList.ForEach(s => s.SetClock(0));

                    // move old commands to history
                    DateTime dt = DateTime.Now.AddDays(-30);
                    await Warehouse.DBService.MoveCommamdsToHist(dt);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, $"Copy Commands and SimpleCommands older than {dt.Date} to history tables.");

                    // check database size
                    if (Warehouse.DBService.GetDBSizeInGB() > _dataBaseSizeGBMax)
                    {
                        DatabaseToLarge = true;
                        Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, "Database is approaching its maximum size. Create backup and delete old records in tables MOVEMENT, ALARM, EVENT, and COMMAND.");
                    }
                    else
                    {
                        DatabaseToLarge = false;
                    }
                    // delete logs
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, "Deletion of old log files started.");
                    DeleteOldLogs(_logPath, _logToKeepMonths);
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, "Deletion of old log files completed.");
                }
                _timeStampOld = timeStamp;

                while ((cmd = Warehouse.DBService.FindFirstNotMoveCommand(Warehouse.SteeringCommands.RemoteMode)) != null)
                {
                    cmd.Status = Command.EnumCommandStatus.Active;
                    Warehouse.DBService.UpdateCommand(cmd);
                    switch (cmd.Task)
                    {
                        case Command.EnumCommandTask.CreateMaterial:
                        case Command.EnumCommandTask.DeleteMaterial:
                            if (cmd.Task == Command.EnumCommandTask.CreateMaterial)
                                Warehouse.CreateMaterial((uint)(cmd as CommandMaterial).Material.Value, (cmd as CommandMaterial).Source, cmd.ID);
                            else
                            {
                                int m;
                                if (!(cmd as CommandMaterial).Material.HasValue)
                                {
                                    Place p = Warehouse.DBService.FindPlace((cmd as CommandMaterial).Source);
                                    m = p != null ? p.Material : 0;
                                }
                                else
                                    m = (cmd as CommandMaterial).Material.Value;
                                Warehouse.DeleteMaterial((uint)m, (cmd as CommandMaterial).Source, cmd.ID);
                            }
                            break;
                        case Command.EnumCommandTask.SegmentInfo:
                        case Command.EnumCommandTask.SegmentOn:
                        case Command.EnumCommandTask.SegmentOff:
                        case Command.EnumCommandTask.SegmentReset:
                            List<string> segmentList = new List<string>();
                            if (Warehouse.SegmentList.FirstOrDefault(s => s.Name == (cmd as CommandSegment).Segment) != null)
                                segmentList.Add((cmd as CommandSegment).Segment);
                            else
                                segmentList = Warehouse.SegmentList.Select(s => s.Name).ToList();
                            Warehouse.DBService.GenerateSegmentSimpleCommands(cmd, segmentList);
                            if (cmd.Task == Command.EnumCommandTask.SegmentInfo)
                                foreach(string s in segmentList)
                                {
                                    Segment seg = Warehouse.SegmentList.FirstOrDefault(ss => ss.Name == s);
                                    if(seg is SegmentMainPanel)
                                        Warehouse.WMS?.SendSegmentInfo((seg as SegmentMainPanel).SegmentInfo, false);
                                    else if (seg is SegmentCrane)
                                        Warehouse.WMS?.SendSegmentInfo((seg as SegmentCrane).SegmentInfo, false);
                                }
                            break;
                        case Command.EnumCommandTask.SegmentHome:
                            var cl1 = (Warehouse.CraneList.FindAll(p => p.Segment == (cmd as CommandSegment).Segment || (cmd as CommandSegment).Segment == "*")).GroupBy(p => p.Segment).Select(q => q.First()).ToList();
                            if (cl1 != null)
                                cl1.ForEach(pp => Warehouse.DBService.AddSimpleCommand(new SimpleCraneCommand
                                                    {
                                                        Command_ID = cmd.ID,
                                                        Task = SimpleCommand.EnumTask.Move,
                                                        Unit = pp.Name,
                                                        Source = pp.HomePosition,
                                                        Status = SimpleCommand.EnumStatus.NotActive,
                                                        Time = DateTime.Now
                                                    }));
                            break;
                        case Command.EnumCommandTask.InfoMaterial:
                            Place place = Warehouse.DBService.FindPlace((cmd as CommandMaterial).Source);
                            if (place == null)
                                place = new Place { Place1 = (cmd as CommandMaterial).Source, Material = 0 };
                            await Warehouse.WMS.SendLocationInfo(place, null);
                            cmd.Status = Command.EnumCommandStatus.Finished;
                            Warehouse.DBService.UpdateCommand(cmd);
                            Warehouse.OnCommandFinish?.Invoke(cmd);
                            break;
                        case Command.EnumCommandTask.InfoSlot:
                            PlaceID placeid = Warehouse.DBService.FindPlaceID((cmd as CommandMaterial).Source);
                            if (placeid != null)
                                Warehouse.WMS.SendPlaceInfo(placeid);
                            cmd.Status = Command.EnumCommandStatus.Finished;
                            Warehouse.DBService.UpdateCommand(cmd);
                            Warehouse.OnCommandFinish?.Invoke(cmd);
                            break;
                        case Command.EnumCommandTask.InfoCommand:
                            Command command = Warehouse.DBService.FindCommandByWMS_ID((cmd as CommandCommand).CommandID.Value);
                            if(command != null)
                                Warehouse.WMS.SendCommandInfo(command);
                            cmd.Status = Command.EnumCommandStatus.Finished;
                            Warehouse.DBService.UpdateCommand(cmd);
                            Warehouse.OnCommandFinish?.Invoke(cmd);
                            break;
                        case Command.EnumCommandTask.CancelCommand:
                            Command c = Warehouse.DBService.ExecuteCancelCommand(cmd as CommandCommand);
                            if (c != null)
                            {
                                Warehouse.OnCommandFinish?.Invoke(c);
                            }
                            if (cmd.Status == Command.EnumCommandStatus.Finished)
                            {
                                Warehouse.DBService.UpdateCommand(cmd);
                                Warehouse.OnCommandFinish?.Invoke(cmd);
                            }
                            break;
                        case Command.EnumCommandTask.Move:
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                throw new StrategyCraneException(String.Format("{0} Strategy failed.", Name));
            }
        }

        public override void Strategy()
        {
            try
            {
                if (CurrentTask != null)
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, $"CurrentTask:{CurrentTask.Status}");
                else
                    Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, $"CurrentTask:null");

                if (CurrentTask == null || CurrentTask.IsCompleted || CurrentTask.IsCanceled || CurrentTask.IsFaulted)
                {
                    // Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, $"CurrentTaskStart");
                    // CurrentTask = StrategyAsync();

                    CurrentTask = Task.Run( async () =>
                                            {
                                                Warehouse.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, "strategyAsync call");
                                                await StrategyAsync();
                                            });
                    CurrentTask.ConfigureAwait(false);
                }
//                if (CurrentTask != null && CurrentTask.IsCompleted)
//                    CurrentTask = null;
            }
            catch (Exception e)
            {
                CurrentTask = null;
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                throw new StrategyCraneException(String.Format("{0} Strategy failed.", Name));
            }
        }
    }
}
