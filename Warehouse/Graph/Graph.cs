using Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Warehouse.ConveyorUnits;

namespace Warehouse.Model
{

    public class Route
    {
        public List<RouteDescription> Items { get; set; }
        public double Cost { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Cost = {0}, {1}", Cost, Items[0].ToString());
            for (int i = 1; i < Items.Count; i++)
                sb.AppendFormat(" -> {0}", Items[i].ToString());
            return sb.ToString();
        }
    }


    public class RouteDescription
    {
        public ConveyorBasic First { get; set; }
        public ConveyorBasic Next { get; set; }
        public ConveyorBasic Final { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[{0} : {1} .. {2}]", First.Name, Next.Name, Final.Name);
            return sb.ToString();
        }
    }

    public partial class BasicWarehouse
    {
        private List<ConveyorBasic> visited = null;



        public int CountSimpleCommandToAcumulation(ConveyorBasic cb, List<ConveyorBasic> visited, SimpleCraneCommand otherDeck)
        {
            int res = 0;
            if ((cb == null) || visited.Contains(cb) || !(cb is Conveyor) || (cb is Conveyor && (cb as Conveyor).AcumulationMark))
                return 0;
            res = DBService.CountSimpleCraneCommandForTarget(cb.Name, true);
            if (otherDeck != null && cb.Name == otherDeck.Source && otherDeck.Task == SimpleCommand.EnumTask.Drop)
                res++;

            visited.Add(cb);
            if (cb is ConveyorJunction)
            {
                foreach (var item in (cb as ConveyorJunction).RouteDef.Node)
                    res += CountSimpleCommandToAcumulation(item.Next, visited, otherDeck);
            }
            else if (cb.Route != null)
                res += CountSimpleCommandToAcumulation(cb.Route.Next, visited, otherDeck);
            return res;
        }

/*
        private int CalcFreePlace1(Route route, List<ConveyorBasic> visited)
        {
            int res = 0;
            foreach (var r in route.Items)
            {
                ConveyorBasic cb = r.Next;
                if (!(cb is Conveyor))
                    return res;
                while (cb != r.Final)
                {
                    Conveyor c = cb as Conveyor;
                    if (c.Accumulation && c.Place == null)
                        res++;
                    cb = cb.Route.Next;
                }
            }
        }
*/

        private int CalcFreePlace(ConveyorBasic cb, List<ConveyorBasic> visited)
        {
            int res = 0;
            if ((cb == null) || visited.Contains(cb) || !(cb is Conveyor))
                return 0;
            if (cb.Place == null)
                res++;

            if ((cb is Conveyor) && (cb as Conveyor).AcumulationMark)
                return res;

            visited.Add(cb);
            if (cb is ConveyorJunction)
            {
                foreach (var item in (cb as ConveyorJunction).RouteDef.Node)
                    res += CalcFreePlace(item.Next, visited);
            }
            else if (cb.Route != null)
                res += CalcFreePlace(cb.Route.Next, visited);
            return res;
        }


        public int FreePlaces(ConveyorBasic cb)
        {
            return CalcFreePlace(cb, new List<ConveyorBasic>());
        }

        private void LocalConveyors(ConveyorBasic cb, List<ConveyorBasic> list)
        {
            if ((cb == null) || list.Contains(cb) || !(cb is Conveyor))
                return;
            list.Add(cb);
            if (cb is ConveyorJunction)
            {
                foreach (var item in (cb as ConveyorJunction).RouteDef.Node)
                    LocalConveyors(item.Next, list);
            }
            else if (cb.Route != null)
                LocalConveyors(cb.Route.Next, list);
        }

        public List<ConveyorBasic> LocalConveyors(ConveyorBasic cb)
        {
            List<ConveyorBasic> list = new List<ConveyorBasic>();
            LocalConveyors(cb, list);
            return list;
        }


        public void BuildRoutes(bool ignoreBlocked)
        {
            int routeIndex = 0;
            try
            {
                visited = new List<ConveyorBasic>();
                foreach (Crane c in Crane.Values)
                    if(!DBService.FindPlaceID(c.Name).Blocked || ignoreBlocked)
                    {
                        visited.Clear();
                        var d = BuildRouteCost(c, 0, ignoreBlocked);
                        if (d.Item2)
                        {
                            if (c.OutRouteDef == null)
                                c.OutRouteDef = new RouteDef();
                            c.OutRouteDef.FinalRouteCost = d.Item1;
                        }

                        if (c.OutRouteDef != null)
                            foreach (Route r in c.OutRouteDef.FinalRouteCost)
                                AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("New route ({2}): {0}, {1}", c.Name, r.ToString(), ++routeIndex));

                    }
                foreach (ConveyorBasic t in ConveyorList)
                    if (t is ConveyorJunction && (!DBService.FindPlaceID(t.Name).Blocked || ignoreBlocked))
                    {
                        ConveyorJunction cj = t as ConveyorJunction;
                        visited.Clear();
                        var d = BuildRouteCost(t, 0, ignoreBlocked);
                        if (d.Item2)
                            cj.RouteDef.FinalRouteCost = d.Item1;

                        if (cj.RouteDef.FinalRouteCost != null)
                            foreach (Route r in cj.RouteDef.FinalRouteCost)
                                AddEvent(Database.Event.EnumSeverity.Event, Database.Event.EnumType.Program, String.Format("New route ({2}): {0}, {1}", cj.Name, r.ToString(), ++routeIndex));
                    }
            }
            catch (Exception ex)
            {
                AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new BasicWarehouseException(String.Format("{0} Warehouse.BuildRoutes failed", Name));
            }
        }


        private ConveyorBasic FindNextNonConveyor (ConveyorBasic cb)
        {
            try
            {
                ConveyorBasic res = cb;
                while (res != null)
                {
                    if (res is ConveyorJunction || res is IConveyorIO || res is IConveyorOutput || res is Crane || res is IConveyorDefault || res is ConveyorOutputDefault)
                        return res;
                    res = res.Route.Next;
                }
                return res;
            }
            catch (Exception ex)
            {
                AddEvent(Event.EnumSeverity.Error, Event.EnumType.Exception, ex.Message);
                throw new BasicWarehouseException(String.Format("{0} Graph.FindNextNonCoveyor failed", cb != null ? cb.Name : "null"));
            }
        }

        private Tuple<List<Route>,bool> BuildRouteCost(ConveyorBasic t, int level, bool ignoreBlocked)
        {

            if (visited.Contains(t))
                return new Tuple<List<Route>, bool>(null,false);
            if (t is ConveyorJunction)
            {
                visited.Add(t);
                ConveyorJunction jt = t as ConveyorJunction;
                List<Route> newRes = new List<Route>();
                Tuple<List<Route>, bool> res = null;
                foreach (RouteNode jt1 in jt.RouteDef.Node)
                {
                    ConveyorBasic final = FindNextNonConveyor(jt1.Next);
                    if (final == null || (!ignoreBlocked && DBService.FindPlaceID(final.Name).Blocked))
                        res = new Tuple<List<Route>, bool>(new List<Route>(), true);
                    else
                        res = BuildRouteCost(final, level+1, ignoreBlocked);

                    if (res.Item2)
                    {
                        RouteDescription rd = new RouteDescription { First = jt, Next = jt1.Next, Final = final };
                        res.Item1.ForEach(p => { p.Items.Insert(0, rd); p.Cost += jt1.Cost; });
                        res.Item1.ForEach(p => newRes.Add(p));
                    }
                }
                if (t is IConveyorOutput && level > 0)
                {
                    RouteDescription rd = new RouteDescription { First = t, Next = t, Final = t };
                    newRes.Add(new Route { Cost = 0, Items = new List<RouteDescription> { rd } } );
                }
                visited.Remove(t);
                return new Tuple<List<Route>, bool>(newRes, newRes.Count > 0);
            }
            else if (t is Crane)
            {
                visited.Add(t);
                Crane jt = t as Crane;
                List<Route> newRes = new List<Route>();

                // crane could be final location
                bool shuttle = !jt.FinalDevice;
                if (jt.Shelve != null && jt.Shelve.Count > 0)
                {
                    RouteDescription rd = new RouteDescription { First = jt, Next = jt, Final = jt };
                    newRes.Add(new Route { Items = new List<RouteDescription> { rd } , Cost = 0 });
                }

                Tuple<List<Route>, bool> res = null;

                if (jt.OutRouteDef != null && jt.OutRouteDef.Node != null)
                    foreach (RouteNode jt1 in jt.OutRouteDef.Node)
                    {
                        ConveyorBasic final = FindNextNonConveyor(jt1.Next);
                        if (final == null || (!ignoreBlocked && DBService.FindPlaceID(final.Name).Blocked))
                            res = new Tuple<List<Route>, bool>(new List<Route>(), true);
                        else
                            res = BuildRouteCost(final, level+1, ignoreBlocked);

                        if (res.Item2 &&  (level == 0 || shuttle) )  
                        { 
                            RouteDescription rd = new RouteDescription { First = jt, Next = jt1.Next, Final = final };
                            res.Item1.ForEach(p => { p.Items.Insert(0, rd); p.Cost += jt1.Cost; });
                            res.Item1.ForEach(p => newRes.Add(p));
                        }
                    }
                visited.Remove(t);
                return new Tuple<List<Route>, bool>(newRes, newRes.Count > 0);
            }
            else if (t is IConveyorOutput)
            {
                RouteDescription rd = new RouteDescription { First = t, Next = t, Final = t };
                return new Tuple<List<Route>, bool>(new List<Route> { new Route { Cost = 0, Items = new List<RouteDescription> { rd } } }, true);
            }
            throw new BasicWarehouseException("Unknown type in path calculation...");
        }

        public bool RouteExists(string source, string target, bool isSimpleCommand)
        {
            try
            {
                ConveyorBasic deviceSource = null;
                ConveyorBasic deviceTargetConveyor = null;
                List<Crane> deviceTargetCrane = null;

                if (source == null || target == null)
                    return false;

                // define source
                // search for junction or first device
                if (ConveyorList.Exists(p => p.Name == source))
                {
                    deviceSource = Conveyor[source];
                    while (deviceSource.Route != null)
                        deviceSource = deviceSource.Route.Next;
                }
                else if (CraneList.Exists(p => p.Name == source))
                    deviceSource = Crane[source];
                else
                    deviceSource = CraneList.Find(p => p.Shelve.Exists(s => source.StartsWith("W:" + s.ToString())));
                if (deviceSource == null)
                    return false;
                if (!(deviceSource is ConveyorJunction) && !(deviceSource is ConveyorOutput) && !(deviceSource is ConveyorOutputDefault)  && !(deviceSource is Crane))
                        return false;

                // define target
                if (ConveyorList.Exists(p => p.Name == target))
                    deviceTargetConveyor = Conveyor[target];
                else
                    deviceTargetCrane = CraneList.FindAll(p => p.Shelve.Exists(s => target.StartsWith("W:" + s.ToString())));
                if (deviceTargetConveyor == null && deviceTargetCrane == null)
                    return false;

                // check if target is in the route
                List<Route> frc = null;
                if (deviceSource is ConveyorJunction)
                    frc = (deviceSource as ConveyorJunction).RouteDef.FinalRouteCost;
                else if (deviceSource is Crane)
                    frc = (deviceSource as Crane).OutRouteDef.FinalRouteCost;
                if (frc == null)
                    return false;
                if (isSimpleCommand)
                {
                    if (deviceTargetConveyor != null && !frc.Exists(p => p.Items.Exists(s => s.Final.Name == deviceTargetConveyor.Name)))
                        return false;
                    if (deviceTargetCrane != null && !frc.Exists(p => p.Items.Exists(s => deviceTargetCrane.Contains(s.Final as Crane))))
                        return false;
                }
                else
                {
                    if (deviceTargetConveyor != null && !frc.Exists(p => p.Items[p.Items.Count-1].Final.Name == deviceTargetConveyor.Name))
                        return false;
                    if (deviceTargetCrane != null && !frc.Exists(p => deviceTargetCrane.Contains(p.Items[p.Items.Count-1].Final as Crane)))
                        return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AddEvent(Database.Event.EnumSeverity.Error, Database.Event.EnumType.Exception,
                         string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
                return false;
            }
        }
    }
}
