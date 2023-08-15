using System;

namespace SMApp
{
    public class WebApiAttribute : Attribute
    {
        public string Name { get; set; }
        public string Action { get; set; }
        public bool POST { get; set; }
        public bool GET { get; set; }
    }
}
