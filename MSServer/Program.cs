using SMApp;
using System;

namespace MSServer
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpOnline.CreateHttpServer(8014);
            HttpOnline.Start();
            HttpOnline.OnHttpError = ex =>
            {
                return new HttpError
                {
                    msg = new { code = 1, msg = ex.Message }.ToJson(),
                    Contenttype = "application/json"
                };
            };
            TcpServer.OpenServer(10012);
            TcpServer.Onerror = e =>
            {
                Console.WriteLine(e.Message);
            };
            Console.WriteLine("服务运行中...");
            Console.ReadLine();
            HttpOnline.Stop();
        }
    }
}
