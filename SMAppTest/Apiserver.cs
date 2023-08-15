using SMApp;
using SMApp.MvcModels;

namespace SMAppTest
{
    public class Apiserver : ISMAppCreater
    {
        
        public Test tt=new Test();
        public void CreateApp(Smserver app)
        {
           var server= app.GetHttpServer(8013);
        }

        public void DestoryApp(Smserver app)
        {
            
        }
    }

    public class Test:Controller
    {
        
        [WebApi(GET = true, POST = true,Action ="/")]
        public ActionResult index()
        {
            var list = Context.Request.Cookies.ToList();

            return Json(list); 
        }
    }
}