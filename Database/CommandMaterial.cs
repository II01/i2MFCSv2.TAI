//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class CommandMaterial : Command
    {
        public Nullable<int> Material { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
    
        public virtual MaterialID MaterialID { get; set; }
        public virtual PlaceID PlaceID { get; set; }
        public virtual PlaceID PlaceIDT { get; set; }
    }
}
