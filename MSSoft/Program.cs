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
            List<AppInfo> appinfolist = ConfigHelper.ReadData("apps").ToJson().ToObject<List<AppInfo>>();
            HttpClientServer.AppInfoList = appinfolist;
            HttpClientServer.CreateHttpServer(8013);
            HttpClientServer.Start();
            HttpClientServer.OnHttpError = ex =>
            {
                return new HttpError
                {
                    msg = new { code = 1, msg = ex.Message }.ToJson(),
                    Contenttype = "application/json"
                };
            };
            //TcpClient.Init("192.168.126.128",10012, "xx", "兴兴");
            //TcpClient.Init("192.168.31.68", 10012, "xx", "兴兴");
            TcpClient.Init("118.123.213.90", 10012, "hys", "兴兴");

            

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
        public static void WriteData(string name, object data)
        {
            Hashtable ha = FileHelper.GetText(file).ToObject<Hashtable>();
            if (ha == null) ha = new Hashtable();
            if (!ha.ContainsKey(name))
            {
                ha.Add(name, data);
                FileHelper.WriteText(file, ha.ToJson(), Encoding.UTF8);
                return;
            }
            ha[name] = data;
            FileHelper.WriteText(file, ha.ToJson(), Encoding.UTF8);
        }
        public static object ReadData(string name)
        {
            Hashtable ha = FileHelper.GetText(file).ToObject<Hashtable>();
            if (ha == null) return null;
            return ha[name];
        }
        public static void DelData(string name)
        {
            Hashtable ha = FileHelper.GetText(file).ToObject<Hashtable>();
            if (ha == null) return;
            ha.Remove(name);
            FileHelper.WriteText(file, ha.ToJson(), Encoding.UTF8);
        }
    }
}
