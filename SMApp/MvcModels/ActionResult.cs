using System.Collections.Generic;

namespace SMApp
{
    public abstract class ActionResult
    {
        public virtual string Name { get; set; }
        public List<MyCookie> Cookies { get; set; }
    }
}
