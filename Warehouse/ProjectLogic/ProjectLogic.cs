using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warehouse.ConveyorUnits;

namespace Warehouse.Model
{
    public partial class BasicWarehouse
    {
        public bool AllowRoute(ConveyorJunction cj, Route r)
        {
            switch (cj.Name)
            {
                case "T031":
                    if (r.Items.Last().Final.Name.StartsWith("C2"))
                    {
                        int freeCount = Conveyor.Count(p => p.Key.StartsWith("T21") && p.Value.Place == null);

                        List<string> convs = new List<string>(new string[] { "T032", "T033", "T034", "T035" });
                        foreach (var c in convs)
                            if (Conveyor[c].Place != null && DBService.FindCommandByPlaceID(c) != null &&
                                (DBService.FindCommandByPlaceID(c) as CommandMaterial).Target.StartsWith("W:2"))
                                freeCount--;                    

                        return freeCount > 0;
                    }
                    return true;
                default:
                    return true;
            }
        }
    }
}
