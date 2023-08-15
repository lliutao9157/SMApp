using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SMApp
{
    interface IZip
    {
        string Ziptype { get; set; }
        byte[] Compress(byte[] buffer);
        byte[] Decompress(byte[] buffer);
        Stream Compress(Stream stream);
        Stream Decompress(Stream stream);
    }
}
