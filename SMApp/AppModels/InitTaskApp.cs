using System.Reflection;

namespace SMApp
{
    public class InitTaskApp
    {
        public object instance { get; set; }
        public MethodInfo methodInfo { get; set; }
        public InitTaskAttribute attribute { get; set; }
        public AppInfo AppInfo { get; set; }
    }
}
