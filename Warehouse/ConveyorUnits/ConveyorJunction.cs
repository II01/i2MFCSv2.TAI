using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Telegrams;
using Warehouse.Model;

namespace Warehouse.ConveyorUnits
{

    [Serializable]
    public class ConveyorJunctionException : Exception
    {
        public ConveyorJunctionException(string s) : base(s)
        { }
    }

    public class XmlRouteNode
    {
        [XmlAttribute]
        public string Next { get; set; }
        [XmlAttribute]
        public double Cost { get; set; }
    }

    public class RouteNode
    {
        public ConveyorBasic Next { get; set; }
        public double Cost { get; set; }
    }

    public class RouteDef
    {
        [XmlIgnore]
        public List<Route> FinalRouteCost { get; set; }

        public List<XmlRouteNode> XmlRoute { get; set; }

        [XmlIgnore]
        public List<RouteNode> Node { get; set; }

    }

    public class ConveyorJunction : Conveyor
    {
        private object _lock;
        public RouteDef RouteDef { get; set; }
        private Random Random { get; set; }
        private Dictionary<string, List<string>> ConveyorNamesOnCommunicator { get; set; }
        [XmlIgnore]
        public Route ActiveRoute { get; set; }
        [XmlIgnore]
        public int? ActiveMaterial { get; set; }

        public ConveyorJunction() : base()
        {
            Random = new Random();
            ActiveRoute = null;
            _lock = new object();
        }

        // Notification status from Communicator
        public override void OnTelegramTransportStatus(Telegram t)
        {
            base.OnTelegramTransportStatus(t);
        }


        // Notification TO from Communicator 
        public override void OnTelegramTransportTO(Telegram t)
        {
            try
            {
                // make material notify on place
                base.OnTelegramTransportTO(t);

                // make junction calls in here
                Strategy();
            }
            catch (Exception ex)
            {
                Warehouse.SteeringCommands.Run = false;
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, String.Format("ConveyorJunction.OnTelegramTransportTO {0} exception : {1}", Name, ex.Message));
            }

            // search for active commands
        }


        private Dictionary<string, List<string>> FillAllConveyorNamesOnCommunicator()
        {
            Dictionary<string, List<string>> res = new Dictionary<string, List<string>>();
            foreach (var c in Warehouse.ConveyorList)
                if (res.Keys.Contains(c.CommunicatorName))
                    res[c.CommunicatorName].Add(c.Name);
                else
                    res.Add(c.CommunicatorName, new List<string> { c.Name });
            return res;
        }

        public virtual void Strategy()
        {
            try
            {
                lock (_lock)
                {


                    if (!Warehouse.StrategyActive)
                        return;

                    if (!Warehouse.SteeringCommands.Run)
                        return;

                    if (!CheckIfAllNotified())
                        return;

                    // delete ActiveRoute
                    if ((Place == null) || (Place.Material != ActiveMaterial))
                    {
                        ActiveRoute = null;
                        ActiveMaterial = null;
                    }

                    if (ConveyorNamesOnCommunicator == null)
                        ConveyorNamesOnCommunicator = FillAllConveyorNamesOnCommunicator();


                    if (Command != null && Place != null && Command.Material != Place.Material)
                    {
                        Command = null;
                        throw new ConveyorJunctionException(String.Format("Command {0} does not match Place {1},{2}", Command.ToString(), Place.Place1, Place.Material));
                    }

                    if (ActiveRoute != null)
                        return;

                    if (Command == null)
                    {
                        SimpleCommand cmd = Warehouse.DBService.FindFirstFastConveyorSimpleCommand(ConveyorNamesOnCommunicator[CommunicatorName], Warehouse.SteeringCommands.AutomaticMode);

                        // FastCommand could be connected to any Conveyor
                        if (cmd != null)
                        {
                            Conveyor cb = Warehouse.Conveyor[cmd.Source];
                            cb.Command = cmd;
                            CreateAndSendTOTelegram(cmd);
                        }
                    }

                    if (Command == null && Place != null &&
                        Warehouse.Conveyor[Name].Remote() && Warehouse.Conveyor[Name].Automatic())     // Uros
                    {
                        if (!Warehouse.SteeringCommands.RemoteMode && !Warehouse.SteeringCommands.AutomaticMode)
                        {
                            Command = Warehouse.DBService.FindFirstSimpleConveyorCommand(Place.Material, Name, false);
                            if (Command != null)
                                CreateAndSendTOTelegram(Command);
                        }
                        else if (Warehouse.SteeringCommands.AutomaticMode)
                        {
                            CommandMaterial cmd = Warehouse.DBService.FindFirstCommand(Place.Material, Warehouse.SteeringCommands.RemoteMode);
                            bool error = false;
                            // check if dimension check failed
                            if (Command_Status != null && (int)Command_Status.Palette.Barcode == Place.Material)
                            {
                                foreach (bool b in Command_Status.Palette.FaultCode)
                                    if (b)
                                    {
                                        error = true;
                                        break;
                                    }
                            }
                            // check for double barcode
                            Place p = Warehouse.DBService.FindMaterial(Place.Material); // ((int)Command_Status.Palette.Barcode);
                            if (p != null && p.Place1 != Name)
                                error = true;

                            // follow command
                            if (cmd != null && !error)
                            {
                                var routes = from route in RouteDef.FinalRouteCost
                                             where route.Items.Last().Final.Compatible(cmd.Target) && Warehouse.AllowRoute(this, route) // && route.Items[0].Final is Conveyor
                                             select route;

                                if(routes.Count() > 1 )
                                    routes = from route in RouteDef.FinalRouteCost
                                             where route.Items.Last().Final.Compatible(cmd.Target) && route.Items[0].Final is Conveyor && Warehouse.AllowRoute(this, route) &&  Warehouse.FreePlaces(route.Items[0].Next) > 0
                                             select route;

                                // transport command handle
                                if (routes.Count() > 0)
                                {
                                    ActiveRoute = routes.ElementAt(Random.Next(routes.Count())); // -1  
                                    ActiveMaterial = cmd.Material;
                                    Warehouse.DBService.AddSimpleCommand(
                                                        Command = new SimpleConveyorCommand
                                                        {
                                                            Command_ID = cmd.ID,
                                                            Material = (int)cmd.Material,
                                                            Source = ActiveRoute.Items[0].First.Name,
                                                            Target = ActiveRoute.Items[0].Final.Name,
                                                            Task = SimpleCommand.EnumTask.Move,
                                                            Status = SimpleCommand.EnumStatus.NotActive,
                                                            Time = DateTime.Now
                                                        });
                                    CreateAndSendTOTelegram(Command);
                                }
                                // crane command demand...
                                else
                                {
                                    routes = from route in RouteDef.FinalRouteCost
                                             where route.Items.Last().Final.Compatible(cmd.Target) && route.Items[0].Final is Crane 
                                             select route;
                                    if (routes.Count() > 0)
                                    {
                                        ActiveRoute = routes.ElementAt(Random.Next(routes.Count())); // -1  
                                        ActiveMaterial = cmd.Material;
                                    }
                                }
                            }
                            // default route to output
                            else if (error)
                            {
                                var routes = from route in RouteDef.FinalRouteCost
                                             where route.Items[0].Next.Place == null && route.Items[0].Final is IConveyorDefault
                                             group route by new { Route = route.Items[0] } into Group
                                             select new
                                             {
                                                 Node1 = Group.Key.Route,
                                                 RouteCost = Group.Min((x) => x.Cost)
                                             };
                                if (routes.Count() > 0)
                                {
                                    int? cmdid = null;
                                    if (cmd != null)
                                        cmdid = cmd.ID;
                                    var route = routes.ElementAt(Random.Next(routes.Count()));  // -1
                                    Warehouse.DBService.AddSimpleCommand(
                                                        Command = new SimpleConveyorCommand
                                                        {
                                                            Command_ID = cmdid,
                                                            Material = (int)Place.Material,
                                                            Source = route.Node1.First.Name,
                                                            Target = route.Node1.Final.Name,
                                                            Task = SimpleCommand.EnumTask.Move,
                                                            Status = SimpleCommand.EnumStatus.NotActive,
                                                            Time = DateTime.Now
                                                        });
                                    CreateAndSendTOTelegram(Command);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorJunctionException(String.Format("{0} ConveyorJunction.Strategy failed ", Name));
            }
        }


        // startup
        public override void Startup()
        {
            base.Startup();           
        }

        public override void Initialize(BasicWarehouse w)
        {
            try
            {
                base.Initialize(w);

                Communicator.OnRefresh += Strategy;
                // special case where only XmlRoutedef is defined. This is mostly in case of ConveyorIO
                if (RouteDef == null || RouteDef.XmlRoute == null)
                {
                    if (XmlRouteNode == null)
                        throw new ConveyorJunctionException(String.Format("{0} junction has no XmlRoute defined", Name));
                    RouteDef = new RouteDef()
                    {
                        XmlRoute = new List<XmlRouteNode> { XmlRouteNode }
                    };
                }
                RouteDef.Node = new List<RouteNode>();
                RouteDef.FinalRouteCost = new List<Route>();
                foreach (XmlRouteNode node in RouteDef.XmlRoute)
                {
                    RouteDef.Node.Add(new RouteNode { Next = Warehouse.FindConveyorBasic(node.Next), Cost = node.Cost });
                }
            }
            catch (Exception ex)
            {
                Warehouse.AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new ConveyorJunctionException(String.Format("{0} ConveyorJunction.Strategy failed ", Name));
            }
        }


    }
}
