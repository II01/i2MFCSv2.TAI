using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DatabaseWMS;
using UserInterfaceGravityPanel.Model;

namespace UserInterfaceGravityPanel.DataServiceWMS
{
    class DBServiceWMS
    {
        public Orders GetCurrentOrderForRamp(int ramp)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    string loc = $"W:32:{ramp:d2}";
                    var order = dc.Orders
                                .Where(p => p.Destination == loc && p.Status > (int)EnumWMSOrderStatus.Waiting && p.Status < (int)EnumWMSOrderStatus.Cancel)
                                .FirstOrDefault();
                    return order;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public int? GetOrderERPID (int? erpidref)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    if (erpidref.HasValue)
                    {
                        var order = dc.CommandERPs.Find(erpidref);
                        return order?.ERP_ID;
                    }
                    else
                        return null;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public Orders GetCurrentSubOrderForRamp(int ramp)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    string loc = $"W:32:{ramp:d2}";
                    var order = dc.Orders
                                .Where(p => p.Destination == loc && p.Status == (int)EnumWMSCommandStatus.Active)
                                .FirstOrDefault();
                    return order;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public OrderCount GetCurrentOrderActivity(Orders o)
        {
            try
            {
                if (o == null)
                    return null;
                using (var dc = new EntitiesWMS())
                {
                    var order = dc.Orders
                                .Where(p => p.ERP_ID == o.ERP_ID && p.OrderID == o.OrderID)
                                .GroupBy(
                                    (by) => by.SubOrderID,
                                    (key, group) => new
                                    {
                                        Suborder = group.FirstOrDefault()
                                    }).ToList();

                    var oc = new OrderCount
                    {
                        Status = order.Where(p => p.Suborder.Status > (int)EnumWMSOrderStatus.Waiting).Min(p => p == null ? 0 : p.Suborder.Status),
                        All = order.Count(),
                        Active = order.Count(p => p.Suborder.Status == (int)EnumWMSOrderStatus.Active),
                        Done = order.Count(p => p.Suborder.Status >= (int)EnumWMSOrderStatus.OnTarget && p.Suborder.Status <= (int)EnumWMSOrderStatus.ReadyToTake),
                        Finished = order.Count(p => p.Suborder.Status >= (int)EnumWMSOrderStatus.Cancel)
                    };

                    return oc;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public OrderCount GetCurrentSubOrderActivity(Orders ord)
        {
            try
            {
                if (ord == null)
                    return null;

                using (var dc = new EntitiesWMS())
                {
                    var order = dc.Orders
                                .Where(p => p.ERP_ID == ord.ERP_ID && p.OrderID == ord.OrderID && p.SubOrderID == ord.SubOrderID)
                                .Join(dc.Commands,
                                      o => o.ID,
                                      c => c.Order_ID,
                                      (o, c) => new { Order = o, Command = c }).ToList();

                    var oc = new OrderCount
                    {
                        Status = order.Where(p => p.Command.Status > (int)EnumWMSCommandStatus.Waiting).Min(p => (p == null || p.Command == null) ? 0 : p.Command.Status),
                        All = order.Count(p => p.Command.Target.StartsWith("W:32")),
                        Active = order.Count(p => p.Command.Status == (int)EnumWMSCommandStatus.Active),
                        Done = order.Count(p => p.Command.Target.StartsWith("W:32") && p.Command.Status >= (int)EnumWMSCommandStatus.Canceled),
                        Finished = order.Count(p => p.Command.Target.StartsWith("W:32") && p.Command.Status == (int)EnumWMSCommandStatus.Finished)
                    };

                    return oc;

                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public List<LaneData> GetLastPallets(int ramp)
        {
            try
            {
                using (var dc = new EntitiesWMS())
                {
                    string loc = $"W:32:{ramp:d2}";

                    var laneOrd = dc.Places
                                  .Where(p => p.PlaceID.StartsWith(loc))
                                  .OrderBy(p => p.PlaceID).ThenBy(p => p.Time)
                                  .ToList();
                    var lanes = laneOrd
                                .GroupBy(
                                    (by) => by.PlaceID,
                                    (key, group) => new
                                    {
                                        Place = group.FirstOrDefault(),
                                        Count = group.Count()
                                    })                                
                                .ToList();

                    var List = new List<LaneData>();

                    foreach(var p in lanes)
                    {
                        LaneData laneData = null;
                        if (p.Place != null)
                        {

                            var tu = dc.TUs.FirstOrDefault(pp => pp.TU_ID == p.Place.TU_ID);
                            SKUData sku = null;
                            if (tu != null)
                                sku = new SKUData { SKU = tu.SKU_ID, SKUBatch = tu.Batch, SKUQty = tu.Qty };

                            var cmd = dc.Commands
                                      .Where(pp => pp.TU_ID == p.Place.TU_ID && pp.Target.StartsWith("W:32"))
                                      .Join(dc.Orders,
                                            c => c.Order_ID,
                                            o => o.ID,
                                            (c, o) => new { o.SubOrderID, o.SubOrderName, o.Status })
                                      .Where(pp => pp.Status < (int)EnumWMSOrderStatus.Cancel)
                                      .FirstOrDefault();
                            SubOrderData so = null;
                            if (cmd != null)
                                so = new SubOrderData {SubOrderID = cmd.SubOrderID, SubOrderName = cmd.SubOrderName };

                            string[] split = p.Place.PlaceID.Split(':');

                            laneData = new LaneData
                            {
                                LaneID = Int32.Parse(split[2]) % 10,
                                Count = p.Count,
                                FirstTUID = p.Place.TU_ID,
                                SKU = sku,
                                Suborder = so
                            };
                        }

                        List.Add(laneData);
                    }

                    return List;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
    }
}


