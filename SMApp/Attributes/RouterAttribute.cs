using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMApp
{
    public class RouterAttribute : Attribute
    {
        public string name { get; set; }
    }
}
