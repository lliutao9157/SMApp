using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SMApp;
using SMApp.MvcModels;

namespace SMApp
{
    public class Routeinfo
    {
        public AppInfo AppInfo { get; set; }
        public object Instance { get; set; }
        public MethodInfo Method { get; set; }
        public bool Isstatic { get; set; }
        public bool Isremote { get; set; }
        public string Path { get; set; }
        public Mime MimeType { get; set; }
        public Func<HttpContext,ActionResult> Action { get; set; }
        public event Action<HttpContext> OnBeforRequest;
        public event Action<HttpContext, Exception> OnAfterRequest;
        internal void BeforRequest(HttpContext context)
        {
            if (OnBeforRequest == null) return;
            var list= OnBeforRequest.GetInvocationList();
            for(var i = 0; i < list.Length; i++)
            {
                list[i].DynamicInvoke(context);
                if (context.Response.IsAbort) break;
            }
        }
        internal void AfterRequest(HttpContext context, Exception e=null)
        {
            if (OnAfterRequest != null) OnAfterRequest(context,e);
        }
    }
}
