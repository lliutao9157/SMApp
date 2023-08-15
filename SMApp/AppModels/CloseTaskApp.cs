using System.Reflection;

namespace SMApp
{
    public class CloseTaskApp
    {
        public object instance { get; set; }
        public MethodInfo methodInfo { get; set; }
        public CloseTaskAttribute attribute { get; set; }
        public AppInfo AppInfo { get; set; }
    }
}
