using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMApp.MvcModels
{
    class ObjectResult : ActionResult
    {
        public override string Name { get; set; } = "ObjectResult";
        public object Data { get; set; }
        public ObjectResult(object data)
        {
            Data = data;
        }
    }
}
