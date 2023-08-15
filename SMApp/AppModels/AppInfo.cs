using System.Reflection;

namespace SMApp
{

    public class AppInfo
    {
        public string name { get; set; }
        public string content { get; set; }
        public string rootdir { get; set; }
        public bool usestaticfiles { get; set; }
        public Assembly assembly { get; set; }
    }

}
