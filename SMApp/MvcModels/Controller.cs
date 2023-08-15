using System.Threading;
using SMApp;

namespace SMApp.MvcModels
{
    public class Controller
    {
        public HttpContext Context
        {
            get
            {
                try
                {
                    var threadid = Thread.CurrentThread.ManagedThreadId.ToString();
                    return CacheFactory.Cache.GetCache<HttpContext>("context" + threadid);
                }
                catch
                {
                    return null;
                }
            }
        }
       
        public TimeTaskApp TimeTaskApp
        {
            get
            {
                try
                {
                    var threadid = Thread.CurrentThread.ManagedThreadId.ToString();
                    return CacheFactory.Cache.GetCache<TimeTaskApp>("timetask" + threadid);
                }
                catch
                {
                    return null;
                }
            }
        }
        public ContentResult Content(string content)
        {
            return new ContentResult(content);
        }
        public JsonResult Json(object data)
        {
            return new JsonResult(data);
        }
        public RedirectResult Redirect(string url)
        {
            return new RedirectResult(url);
        }
        public FileResult File(byte[] data, string contenttype)
        {
            return new FileResult
            {
                Data = data,
                ContentType = contenttype
            };
        }
    }
}
