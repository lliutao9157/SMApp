using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMApp.MvcModels
{
    public class StreamResult : ActionResult
    {
        public override string Name { get; set; } = "StreamResult";
        public byte[] Data { get; set; }
        public long Length { get; set; }
        public int Partlength { get; set; }
        public string ContentType { get; set; }
        public bool Iscompress { get; set; }
    }
}
