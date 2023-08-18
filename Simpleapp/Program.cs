using SMApp;
using SMApp.MvcModels;
using System.Reflection;


namespace Simpleapp
{
    internal class Program
    {
        public static SMApp.Smserver Smserver = new SMApp.Smserver();
        static void Main(string[] args)
        {
            AppInfo app = new AppInfo
            {
                rootdir = "pub",
                usestaticfiles = true,
                name = "",
                assembly = Assembly.Load("SMAppTest")
            };
            Smserver.LoadApps(app);
            var httpserser = Smserver.CreateHttpServer(80);
            httpserser.TimeOut = 1000000;
            //
            //Smserver.CreateHttpServer(443, "test.pfx", "123456");
            httpserser.AuthenticationSchemeSelector = (request) =>
            {
                if (request.RawUrl == "/bbc") return SMApp.AuthenticationSchemes.Digest;
                else return SMApp.AuthenticationSchemes.Anonymous;
            };
            Smserver.AddRoute("/bbc", (context) =>
            {
                // Console.WriteLine(req.jsondata);
                return new ContentResult("你们好");
            }, SMApp.HttpMethod.POST);
            Smserver.AddRoute("/bbc", (context) =>
            {

                // Console.WriteLine(req.jsondata);
                return new ContentResult("你们好111");
            }, SMApp.HttpMethod.GET);
            Routeinfo routeinfo = Smserver.GetRoute("/index.html", SMApp.HttpMethod.GET);
            routeinfo.OnBeforRequest += Routeinfo_OnBeforRequest;
            Smserver.GetRoutedic().TryAdd("GET:/", routeinfo);
            httpserser.AddWebSocketService<Echotest>("/Echo");
            Smserver.Start();
            Console.WriteLine("程序已启动...");
            Smserver.Logger.Output = (a, b) =>
            {
                Console.WriteLine(a.ToString());
            };
            Smserver.Logger.Level = LogLevel.Trace;
            Console.ReadKey();
            Smserver.Stop();
        }
        private static void Routeinfo_OnBeforRequest(HttpContext e)
        {
            Console.WriteLine(e.Request.QueryString);
            Smserver.Logger.Info("欢迎使用smapp");
        }
    }
    public class Echotest : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            base.OnOpen();
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine(e.Data);
            base.OnMessage(e);
            this.Send(e.Data);
        }
    }
}