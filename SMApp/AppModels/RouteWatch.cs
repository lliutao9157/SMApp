using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMApp
{
    internal class RouteWatch : FileSystemWatcher
    {
        public string Route { get; set; }
    }
}
