using Autofac.Extras.DynamicProxy;
using SMApp;
using System.Collections.Generic;


namespace XX.PD.Interface
{
    [Intercept(typeof(HYXInterceptor))]
    public interface ITest
    {
        [WebApi(GET = true, POST = true)]
        JsonResult Getme();
        [WebApi(GET = true, POST = true)]
        JsonResult bbb(string name, int age);
        [WebApi(GET = true, POST = true)]
        JsonResult uploaddoctorpic(List<AppFile> file);
        [WebApi(GET = true, POST = true)]
        ContentResult ddd(string name);
        [WebApi(GET = true, POST = true)]
        RedirectResult goout();
        [WebApi(GET = true, POST = true)]
        FileResult ccc();
    }
}
