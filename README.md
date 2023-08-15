SMApp 介绍
SMApp是用c#编写的用于构建web应用的模块。支持http、https、websocket,可发布静态资源和动态资源。

SMApp 软件架构
支持MVC模式和直接用路由绑定方法的模式。


SMApp 使用方法

1.  先在依赖项中引入SMApp
2.  在主方法中  using SMApp;using SMApp.MvcModels; 
3.  static void Main(string[] args)
        {
            var httpserser = Smserver.CreateHttpServer(80);
            Smserver.AddRoute("/bbc", (context) =>
            {
                return new ContentResult("hello world");
            }, SMApp.HttpMethod.GET);
            Smserver.Start();
            Console.ReadKey();
            Smserver.Stop();
        }
4. 在浏览器输入http://localhost/bbc
SMApp 使用说明

以上是一个简单的例子，更多的使用方法，请看源码中的示例。