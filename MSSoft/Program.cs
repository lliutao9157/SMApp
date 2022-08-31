using SMApp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MSClient
{
    class Program
    {
        static void Main(string[] args)
        {
           
            HttpClientServer.CreateHttpServer(8013);
            HttpClientServer.Start();
            HttpClientServer.OnHttpError = ex =>
            {
                return new HttpError
                {
                    msg =ex.Message,
                    Contenttype = "application/json"
                };
            };
            //TcpClient.Init("192.168.126.128",10012, "xx", "兴兴");
            //TcpClient.Init("192.168.31.68", 10012, "xx", "兴兴");
            TcpClient.Init("118.123.213.90", 10012, "hys", "兴兴");
            TcpClient.Onerror = e =>
            {
                Console.WriteLine(e.Message);
            };
            

            Console.WriteLine("服务运行中...");
            Console.ReadLine();
            HttpClientServer.Stop();
        }
    }
    class ConfigHelper
    {
        private static string file
        {
            get
            {
                string config = AppDomain.CurrentDomain.BaseDirectory + "config.json";
                if (!File.Exists(config))
                {
                    FileStream f = File.Create(config);
                    f.Close();
                }
                return config;
            }
        }
       
        
    }
}
