using System.Collections.Generic;

namespace SMApp.MvcModels
{
    public abstract class ActionResult
    {
        public virtual string Name { get; set; }
        public string Etag { get; set; }
    }
}
