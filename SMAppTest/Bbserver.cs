using SMApp;
using SMApp.MvcModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAppTest
{
    internal class Bbserver : ISMAppCreater
    {
        public Test1 tc = new Test1();
        public void CreateApp(Smserver app)
        {
             
        }

        public void DestoryApp(Smserver app)
        {
             
        }
    }
    public class Test1 : Controller
    {

        [WebApi(GET = true, POST = true, Action = "/")]
        public ActionResult index()
        {
            Context.Response.Cookies.Add(new Cookie("abc", "888888") {Expires=DateTime.Now.AddDays(10) });
            return Content("我们很好");
        }
    }
}
