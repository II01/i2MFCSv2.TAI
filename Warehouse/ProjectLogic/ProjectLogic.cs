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
                        int availablePlaces = 4;

                        List<string> convs = new List<string>(new string[] { "T032", "T033", "T034", "T035", "T211", "T212", "T213", "T214" });
                        foreach (var c in convs)
                            if (Conveyor[c].Place != null && DBService.FindCommandByPlaceID(c) != null &&
                                (DBService.FindCommandByPlaceID(c) as CommandMaterial).Target.StartsWith("W:2"))
                                availablePlaces--;                    

                        return availablePlaces > 0;
                    }
                    return true;
                case "T125":
                    if (Conveyor["T032"].Place != null)
                        return false;
                    return true;
                case "T225":
                    if (Conveyor["T036"].Place != null)
                        return false;
                    return true;
                default:
                    return true;
            }
        }
    }
}
