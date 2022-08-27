using System;

namespace SMApp
{
    public class TimetaskAttribute : Attribute
    {
        public string Name { get; set; }
        public int Time { get; set; }
    }
}
