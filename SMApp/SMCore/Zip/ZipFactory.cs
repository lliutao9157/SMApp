using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMApp
{
    class ZipFactory
    {
        private static GzipInstance _gzip = null;
        private static GzipInstance gzip 
        {
            get
            {
                if (_gzip == null) _gzip = new GzipInstance();
                return _gzip;
            }
        }
        public static IZip GetZip(string ziptype)
        {
            if (ziptype.ToStr().Contains("gzip")) return gzip;
            return null;
        }
    }
}
