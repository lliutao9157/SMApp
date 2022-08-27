using System.Threading;

namespace SMApp
{
    public class Controller
    {
        public HttpRequest Request
        {
            get
            {
                var threadid = Thread.CurrentThread.ManagedThreadId.ToString();
                return CacheFactory.Cache.GetCache<HttpRequest>("request" + threadid);
            }
        }
        public MyCookies Cookies { get; set; } = new MyCookies();
        private HttpRequest request { get; set; }

        public ContentResult Content(string content)
        {
            return new ContentResult(content) { Cookies = Cookies.Cookies };
        }
        public JsonResult Json(object data)
        {
            return new JsonResult(data) { Cookies = Cookies.Cookies };
        }
        public RedirectResult Redirect(string url)
        {
            return new RedirectResult(url) { Cookies = Cookies.Cookies };
        }
        public FileResult File(byte[] data, string contenttype)
        {
            return new FileResult
            {
                Data = data,
                ContentType = contenttype,
                Cookies = Cookies.Cookies
            };
        }

    }
}
