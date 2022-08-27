using System.Reflection;

namespace SMApp
{
    public class TimeTaskApp
    {
        public object instance { get; set; }
        public MethodInfo methodInfo { get; set; }
        public TimetaskAttribute attribute { get; set; }
        public bool CanExcute { get; set; }
    }
}
