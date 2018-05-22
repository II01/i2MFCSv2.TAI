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
                        return Conveyor["T032"].Place == null && Conveyor["T033"].Place == null && Conveyor["T034"].Place == null && Conveyor["T035"].Place == null && Conveyor["T211"].Place == null;
                    }
                    return true;
                default:
                    return true;
            }
        }
    }
}
