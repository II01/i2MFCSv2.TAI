using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Warehouse.Common;
using Warehouse.ConveyorUnits;
using Warehouse.Model;

namespace Warehouse.Strategy
{

    [Serializable]
    public class StrategyDoubleForkCraneException : Exception
    {
        public StrategyDoubleForkCraneException(string s) : base(s)
        {
        }
    }


    public class LinkedConveyor
    {
        public string ConveyorCrane1Name { get; set; }
        public string ConveyorCrane2Name { get; set; }
        [XmlIgnore]
        public ConveyorIO ConveyorCrane1 { get; set; }
        [XmlIgnore]
        public ConveyorIO ConveyorCrane2 { get; set; }


        public LinkedConveyor()
        {
        }

        public void Initialize(StrategyDoubleForkCrane c)
        {
            ConveyorCrane1 = c.Strategy1.Crane.InConveyor.FirstOrDefault(prop => prop.Name == ConveyorCrane1Name);
            ConveyorCrane2 = c.Strategy2.Crane.InConveyor.FirstOrDefault(prop => prop.Name == ConveyorCrane2Name);
        }
    }


    [XmlInclude(typeof(LinkedConveyor))]
    public class StrategyDoubleForkCrane : BasicStrategy
    {
        public string Crane1Name { get; set; }
        public string Crane2Name { get; set; }

        [XmlIgnore]
        public StrategyCrane Strategy1 { get; set; }
        [XmlIgnore]
        public StrategyCrane Strategy2 { get; set; }

        public List<LinkedConveyor> LinkedInputConveyors { get; set; }


        public StrategyDoubleForkCrane()
        {
            Strategy1 = new StrategyCrane();
            Strategy2 = new StrategyCrane();
        }

        public override void Refresh()
        {
            try
            {
                Strategy();
            }
            catch (Exception e)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, e.Message);
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("{0} StrategyDoubleForkCrane.Refresh failed", Name));
                Warehouse.SteeringCommands.Run = false;
            }
        }


        public FLocation GetLocation(string device)
        {
            LPosition pos = LPosition.FromString(device);
            return pos.IsWarehouse() 
                ? 
                new FLocation {
                    X = 6020 + (pos.Travel-1) * 1055,
                    Y =  310 + (pos.Height-1) * 2458,
                    Z =    0 } 
                : 
                Warehouse.FindConveyorBasic(device).FLocation;
        }

        public bool NearestCmd1(SimpleCraneCommand cmd1, SimpleCraneCommand cmd2, FLocation location)
        {
            FLocation fpos1 = GetLocation(cmd1.Source);
            FLocation fpos2 = GetLocation(cmd2.Source);

            return (location - fpos1).Abs() <= (location - fpos2).Abs();
        }

        public override void Strategy()
        {
            try
            {

                if (!Warehouse.StrategyActive)
                    return;
                if (!Warehouse.SteeringCommands.Run)
                    return;
                //                if (!Strategy1.Crane.Automatic() || !Strategy2.Crane.Automatic())
                //                    return;

                bool remote = Warehouse.SteeringCommands.RemoteMode;
                LinkedConveyor lc = null;
                Strategy2.PrefferedInput = Strategy1.PrefferedInput;

                Strategy1.Command = Strategy1.Crane.Command;
                Strategy1.BufferCommand = Strategy1.Crane.BufferCommand;

                Strategy2.Command = Strategy2.Crane.Command;
                Strategy2.BufferCommand = Strategy2.Crane.BufferCommand;

                if (Strategy1.Crane.FastCommand == null)
                    Strategy1.Crane.FastCommand = Warehouse.DBService.FindFirstFastSimpleCraneCommand(Strategy1.Crane.Name, Warehouse.SteeringCommands.AutomaticMode);

                SimpleCraneCommand c1c = Warehouse.DBService.CheckIfPlaceBlocked(Strategy1.Crane.Name) ? null : Strategy1.GetNewCommand(remote);
                //                SimpleCraneCommand c1b = Strategy1.GetNewCommand(remote);

                Strategy2.BannedPlaces.AddRange(Strategy1.BannedPlaces);

                if (Strategy2.Crane.FastCommand == null)
                    Strategy2.Crane.FastCommand = Warehouse.DBService.FindFirstFastSimpleCraneCommand(Strategy2.Crane.Name, Warehouse.SteeringCommands.AutomaticMode);
                SimpleCraneCommand c2c = Warehouse.DBService.CheckIfPlaceBlocked(Strategy2.Crane.Name) ? null : Strategy2.GetNewCommand(remote);
                //                SimpleCraneCommand c2b = Strategy2.GetNewCommand(remote);

                Strategy1.WriteCommandToPLC(Strategy1.Crane.FastCommand, true);
                Strategy2.WriteCommandToPLC(Strategy2.Crane.FastCommand, true);

                if (c1c != null && c2c != null && c1c.Task == c2c.Task)
                //                    && ((Strategy1.Crane.Command == null && Strategy2.Crane.Command == null) || 
                //                    (Strategy1.Crane.Command != null && Strategy2.Crane.Command != null)))
                {
                    if (c1c.Task == SimpleCommand.EnumTask.Pick)
                        Strategy1.PrefferedInput = !Strategy1.PrefferedInput;
                    // new both Execute command with same task
                    bool opposite = false;
                    FLocation lastloc = null;

                    if (Strategy1.Crane.Command != null && Strategy2.Crane.Command != null)
                        lastloc = Strategy1.Crane.Command.Command_ID > Strategy2.Crane.Command.ID ? GetLocation(Strategy1.Crane.Command.Source) : GetLocation(Strategy2.Crane.Command.Source);
                    else if (Strategy1.Crane.Command != null)
                        lastloc = GetLocation(Strategy1.Crane.Command.Source);
                    else if (Strategy2.Crane.Command != null)
                        lastloc = GetLocation(Strategy2.Crane.Command.Source);
                    else
                        lastloc = Strategy1.Crane.FLocation;


                    if (c1c != null && c2c != null && c1c.Task == c2c.Task && 
                        c1c.Source.StartsWith("W") && c2c.Source.StartsWith("W") && 
                        c1c.Source.Substring(0, c1c.Source.Length-1) == c2c.Source.Substring(0, c2c.Source.Length-1))
                        opposite = (c1c.Task == SimpleCommand.EnumTask.Pick && c1c.Source.EndsWith("2") && c2c.Source.EndsWith("1")) ||
                                   (c1c.Task == SimpleCommand.EnumTask.Drop && c1c.Source.EndsWith("1") && c2c.Source.EndsWith("2"));
                    else
                        opposite = !NearestCmd1(c1c, c2c, lastloc);

                    if (!opposite)
                    {
                        Strategy1.WriteCommandToPLC(c1c);
                        Strategy2.WriteCommandToPLC(c2c);
                    }
                    else
                    {
                        Strategy2.WriteCommandToPLC(c2c);
                        Strategy1.WriteCommandToPLC(c1c);
                    }
                }
                else if (c1c != null && c2c != null && c1c.Task != c2c.Task)
                {
                    // only drop if different comands to be done
                    if (c1c.Task == SimpleCommand.EnumTask.Drop)
                        Strategy1.WriteCommandToPLC(c1c);
                    else
                        Strategy2.WriteCommandToPLC(c2c);
                }
                else if (c1c != null && Strategy2.Command == null)
                {
                    if (c1c.Task == SimpleCommand.EnumTask.Pick)
                        Strategy1.PrefferedInput = !Strategy1.PrefferedInput;
                    Strategy1.WriteCommandToPLC(c1c);
                }
                else if (c2c != null && Strategy1.Command == null)
                {
                    if (c2c.Task == SimpleCommand.EnumTask.Pick)
                        Strategy1.PrefferedInput = !Strategy1.PrefferedInput;
                    Strategy2.WriteCommandToPLC(c2c);
                }

                Strategy1.BannedPlaces.Clear();
                Strategy2.BannedPlaces.Clear();

                // logic for prefferable double pick
                if (Strategy1.PickAction != null && Strategy1.ForcedInput == null)
                    Strategy2.ForcedInput = (lc = LinkedInputConveyors.FirstOrDefault(prop => prop.ConveyorCrane1.Name == Strategy1.PickAction.Source)) != null ? lc.ConveyorCrane2 : null;

                // logic for prefferable double pick
                if (Strategy2.PickAction != null && Strategy2.ForcedInput == null)
                    Strategy1.ForcedInput = (lc = LinkedInputConveyors.FirstOrDefault(prop => prop.ConveyorCrane2.Name == Strategy2.PickAction.Source)) != null ? lc.ConveyorCrane1 : null;

            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new StrategyDoubleForkCraneException(String.Format("{0} StrategyDoubleForkCrane.Refresh failed", Name));
            }
        }

        public override void Initialize(BasicWarehouse w)
        {
            try
            {
                Warehouse = w;

                Strategy1.Warehouse = w;
                Strategy1.CraneName = Crane1Name;
                Strategy1.Crane = w.Crane[Strategy1.CraneName];
                Strategy1.Crane.OnStrategy = Strategy;
                Strategy1.Crane.Communicator.OnRefresh += Refresh;

                Strategy2.Warehouse = w;
                Strategy2.CraneName = Crane2Name;
                Strategy2.Crane = w.Crane[Strategy2.CraneName];
                Strategy2.Crane.OnStrategy = Strategy;
                Strategy2.Crane.Communicator.OnRefresh += Refresh;

                if (LinkedInputConveyors != null)
                    LinkedInputConveyors.ForEach(p => p.Initialize(this));

            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new StrategyDoubleForkCraneException(String.Format("{0} StrategyDoubleForkCrane.Initialize failed", Name));
            }
        }

    }
}
