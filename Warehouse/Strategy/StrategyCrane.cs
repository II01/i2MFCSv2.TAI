using System;
using System.Collections.Generic;
using Database;
using System.Xml.Serialization;
using Warehouse.ConveyorUnits;
using Warehouse.Common;
using Warehouse.Model;

namespace Warehouse.Strategy
{

    [Serializable]
    public class StrategyCraneException : Exception
    {
        public StrategyCraneException(string s) : base(s)
        { }
    }


    public class StrategyCrane : BasicStrategy
    {
        [XmlIgnore]
        public SimpleCraneCommand Command { get; set; }
        [XmlIgnore]
        public SimpleCraneCommand BufferCommand { get; set; }
        [XmlIgnore]
        public List<string> BannedPlaces { get; private set; }

        [XmlIgnore]
        public Crane Crane { get; set; }
        public string CraneName { get; set; }

        [XmlIgnore]
        public bool PrefferedInput { get; set; }

        [XmlIgnore]
        public SimpleCraneCommand PickAction { get; set; }
        [XmlIgnore]
        public SimpleCraneCommand DropAction { get; set; }

        [XmlIgnore]
        public IConveyorIO ForcedInput { get; set; }
        private bool LastInBound { get; set; } // was last command inbound

        public StrategyCrane() : base()
        {
            BannedPlaces = new List<string>();
        }


        public override void Refresh()
        {
            // call only for in thread
            try
            {
                Strategy();
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("{0} Refresh failed", Name));
                Warehouse.SteeringCommands.Run = false;
            }
        }

        private int Distance(Telegrams.Position p1, LPosition p2)
        {
            return Math.Abs(p1.X - p2.Travel) + Math.Abs(p1.Y - p2.Height);
        }


        private ConveyorIO BestOutput(List<ConveyorIO> list)
        {
            try
            {
                if (list.Count == 0)
                    throw new StrategyCraneException(String.Format("{0} StrategyCraneDD.BestOutput list empty", Name));
                foreach (var u in list)
                    if (u.Place == null)
                        return u;
                return list[0]; // lucky guess 
            }
            catch
            {
                throw new StrategyCraneException(String.Format("{0} StrategyCraneDD.BestOutput for {1} failed.", Name, Crane.Name));
            }
        }

        private SimpleCraneCommand GetCommandFromFreeState(bool inputPreference, bool automatic, List<string> bannedPlaces, SimpleCraneCommand otherDeck)
        {
            try
            {
                // pick pallet from input line

                var cmdInput = Crane.FindBestInput(automatic, ForcedInput);
                var CmdWarehouse = Crane.FindBestWarehouse(automatic, bannedPlaces, otherDeck);

                if ((inputPreference && cmdInput != null) || (cmdInput != null && CmdWarehouse == null))
                {
                    // Warehouse.DBService.AddSimpleCommand(cmdInput);
                    ForcedInput = null;
                    return cmdInput;
                }
                else if (CmdWarehouse != null)
                {
                    bannedPlaces.Add(CmdWarehouse.Source);
                    // Warehouse.DBService.AddSimpleCommand(CmdWarehouse);
                    return CmdWarehouse;
                }

                return null;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new StrategyCraneException(String.Format("{0} GetCommandFreeState failed", Name));
            }
        }

        private SimpleCraneCommand GetCommandFromOccupiedState(int material, bool automatic)
        {
            try
            {
                SimpleCraneCommand res = null;
                // check if outbound
                var target = Warehouse.DBService.FindFirstCommand(material, automatic);
                if (target != null)
                {
                    LPosition position = LPosition.FromString(target.Target);
                    if (!position.IsWarehouse() || (!Crane.Shelve.Contains((short)position.Shelve)))
                    {
                        res = Crane.FindBestOutput(target);
                        // if (res != null)
                        //    Warehouse.DBService.AddSimpleCommand(res);
                    }
                    else
                        // Warehouse.DBService.AddSimpleCommand(
                        res = new SimpleCraneCommand
                        {
                            Unit = Crane.Name,
                            Command_ID = target.ID,
                            Material = material,
                            Task = SimpleCommand.EnumTask.Drop,
                            Source = target.Target,
                            Status = SimpleCommand.EnumStatus.NotInDB,
                            Time = DateTime.Now
                        };
                }
                return res;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new StrategyCraneException(String.Format("{0} GetCommandFromOccupiedState failed ({1})", Name, material));
            }
        }

        public SimpleCraneCommand GetNewCommand(bool remote, SimpleCraneCommand otherDeck)
        {
            if (Command == null || Command.Status >= SimpleCommand.EnumStatus.Canceled)
            {
                if (!Warehouse.SteeringCommands.AutomaticMode)
                    Command = Warehouse.DBService.FindFirstSimpleCraneCommand(Crane.Name, false);  // for simple commands
                else
                {
                    Command = Warehouse.DBService.FindFirstSimpleCraneCommand(Crane.Name, true);  // for move to work
                    if (!(Command != null && Command.Task == SimpleCommand.EnumTask.Move))
                    {
                        if (Crane.Place != null)
                            Command = GetCommandFromOccupiedState(Crane.Place.Material, remote);
                        else
                            Command = GetCommandFromFreeState(PrefferedInput, remote, BannedPlaces, otherDeck);
                    }
                }

                return Command;
            }

            if (BufferCommand == null || BufferCommand.Status >= SimpleCommand.EnumStatus.Canceled)
            {
                if (Warehouse.SteeringCommands.AutomaticMode)
                {
                    if (Command.Task == SimpleCommand.EnumTask.Move)
                    {
                        // check for outbound
                        if (Crane.Place != null)
                            BufferCommand = GetCommandFromOccupiedState(Crane.Place.Material, remote);
                        else
                            BufferCommand = GetCommandFromFreeState(PrefferedInput, remote, BannedPlaces, otherDeck);
                    }
                    else if (Command.Task == SimpleCommand.EnumTask.Pick)
                        BufferCommand = GetCommandFromOccupiedState(Command.Material.Value, remote);
                    else if (Command.Task == SimpleCommand.EnumTask.Drop)
                        BufferCommand = GetCommandFromFreeState(PrefferedInput, remote, BannedPlaces, otherDeck);
                }

                return BufferCommand;
            }

            return null;
        }

        private SimpleCraneCommand GetFastCommand(bool remote)
        {
            try
            {
                return Warehouse.DBService.FindFirstFastSimpleCraneCommand(Crane.Name, remote);
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new StrategyCraneException(String.Format("{0} GetFastCommand failed", Name));
            }
        }

        public void WriteCommandToPLC(SimpleCraneCommand cmd, bool fastCommand = false)
        {
            if (cmd != null && cmd.Status <= SimpleCommand.EnumStatus.NotActive)
            {
                Crane.WriteCommandToPLC(cmd, fastCommand);
                if (cmd.Task == SimpleCommand.EnumTask.Pick)
                    PickAction = cmd;
                if (cmd.Task == SimpleCommand.EnumTask.Drop)
                    DropAction = cmd;
            }
        }

        public override void Strategy()
        {
            if (Crane.PLC_Status != null )
                Crane.FastCommand = null;

            if (!Warehouse.StrategyActive)
                return;
            if (!Crane.Communicator.Online())
                    return;
            if (!Crane.CheckIfAllNotified())
                return;

            try
            {
                PickAction = null;
                Command = Crane.Command;

                BufferCommand = Crane.BufferCommand;
                bool remote = Warehouse.SteeringCommands.RemoteMode;

                if (Crane.FastCommand == null)
                    Crane.FastCommand = Warehouse.DBService.FindFirstFastSimpleCraneCommand(Crane.Name, Warehouse.SteeringCommands.AutomaticMode);

                WriteCommandToPLC(Crane.FastCommand, true);

                if (!Warehouse.SteeringCommands.Run)
                    return;
                if ((!Crane.Remote() || Crane.LongTermBlock()))
                    return;
                if (!Crane.Automatic())
                    return;

                GetNewCommand(remote, null);
                GetNewCommand(remote, null);

                    // make double cycles
                if (PickAction != null)
                    PrefferedInput = LPosition.FromString(PickAction.Source).IsWarehouse();

                WriteCommandToPLC(Command);
                WriteCommandToPLC(BufferCommand);

                BannedPlaces.Clear();
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                throw new StrategyCraneException(String.Format("{0} Strategy failed.", Name));
            }
        }

        public override void Initialize(BasicWarehouse w)
        {
            try
            {
                Warehouse = w;
                if (CraneName == "")
                    throw new StrategyCraneException(String.Format("{0} CranName is null", Name));
                if (!w.Crane.ContainsKey(CraneName))
                    throw new StrategyCraneException(String.Format("{0} CranName={1} does is unknown.", Name, CraneName));
                Crane = w.Crane[CraneName];
                Crane.OnStrategy = Strategy;
                Crane.Communicator.OnRefresh += Refresh;
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new StrategyDoubleForkCraneException(String.Format("{0} StrategyCrane.Initialize failed", Name));
            }
        }
    }
}
