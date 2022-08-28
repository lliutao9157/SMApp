using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Net;

namespace SMApp
{
    public class TcpServer
    {
        private static ConcurrentDictionary<string, System.Threading.Channels.Channel<string>> _chandic = null;
        public static ConcurrentDictionary<string, System.Threading.Channels.Channel<string>> chandic
        {
            get
            {
                if (_chandic == null) _chandic = new ConcurrentDictionary<string, System.Threading.Channels.Channel<string>>();
                return _chandic;
            }
        }
        private static ConcurrentDictionary<string, System.Threading.Channels.Channel<byte[]>> _filechandic = null;
        public static ConcurrentDictionary<string, System.Threading.Channels.Channel<byte[]>> filechandic
        {
            get
            {
                if (_filechandic == null) _filechandic = new ConcurrentDictionary<string, System.Threading.Channels.Channel<byte[]>>();
                return _filechandic;
            }
        }
        private static SocketServer<Msginfo> wssv;
        public static void OpenServer(int port, string certpath = "", string certpassword = "")
        {
            string head = "ws";
            if (!string.IsNullOrWhiteSpace(certpath)) head = "wss";
            wssv = new SocketServer<Msginfo>($"{head}://0.0.0.0:{port}", certpath: certpath, certpassword: certpassword);
            wssv.Open();
        }
        public static void Stop()
        {
            if (wssv == null) return;
            wssv.Close();
        }
        public static List<Msginfo> ClientList
        {
            get
            {
                return wssv.ClientList;
            }
        }
        public static async Task<string> GetApiresult(object obj, string id)
        {
            Msginfo client = ClientList.Find(d => d.ClientId == id);
            if (client == null) throw new Exception("未找到远程服务");
            string guid = Guid.NewGuid().ToString().Replace("-", "");
            guid = "0" + guid.Substring(1);
            System.Threading.Channels.Channel<string> objchan = System.Threading.Channels.Channel.CreateUnbounded<string>();
            chandic.TryAdd(guid, objchan);
            var issend = await client.SendObject(obj, guid);
            if (!issend) throw new Exception("系统繁忙，请稍候再试");
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(10000);
                var result = await objchan.Reader.ReadAsync(cts.Token);
                chandic.TryRemove(guid, out objchan);
                return result;
            }
            catch (Exception e)
            {
                if (Onerror != null) Onerror(e);
                chandic.TryRemove(guid, out objchan);
                throw new Exception("系统繁忙，请稍候再试");
            }

        }
        public static async Task<byte[]> GetFileresult(object obj, string id)
        {
            Msginfo client = ClientList.Find(d => d.ClientId == id);
            string guid = Guid.NewGuid().ToString().Replace("-", "");
            guid = "1" + guid.Substring(1);
            System.Threading.Channels.Channel<byte[]> objchan = System.Threading.Channels.Channel.CreateUnbounded<byte[]>();
            TcpServer.filechandic.TryAdd(guid, objchan);
            var issend = await client.SendObject(obj, guid);
            if (!issend) return new byte[0];
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(10000);
                var result = await objchan.Reader.ReadAsync(cts.Token);
                filechandic.TryRemove(guid, out objchan);
                return result;
            }
            catch (Exception e)
            {
                if (Onerror != null) Onerror(e);
                filechandic.TryRemove(guid, out objchan);
                return new byte[0];
            }
        }
        public static Action<Exception> Onerror { get; set; }
    }
    public class Msginfo : ServerSocketConnection
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        private string Sign { get; set; }
        private bool CheckSign()
        {
            return Sign == Md5.getmd5("xsdrj" + ClientId + ClientName);
        }
        public override void OnMessage(byte[] data, string id)
        {
            char sign = id[0];
            if (sign == '0')
            {
                if (!TcpServer.chandic.ContainsKey(id)) return;
                System.Threading.Channels.Channel<string> objchan = TcpServer.chandic[id];
                var newdata = GZip.Decompress(data);
                var str = System.Text.Encoding.UTF8.GetString(newdata);
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(5000);
                    objchan.Writer.WriteAsync(str, cts.Token);
                }
                catch (Exception e)
                {
                    OnError(e);
                }
            }
            if (sign == '1')
            {
                if (!TcpServer.filechandic.ContainsKey(id)) return;
                System.Threading.Channels.Channel<byte[]> objchan = TcpServer.filechandic[id];
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(5000);
                    objchan.Writer.WriteAsync(data, cts.Token);
                }
                catch (Exception e)
                {
                    OnError(e);
                }
            }
        }
        public async Task<bool> SendObject(object obj, string guid)
        {
            byte[] data = obj.ToJson().ToBytes();
            var newdata = GZip.Compress(data);
            var issend = await this.Send(newdata, guid);
            return issend;
        }
        public async override void OnOpen()
        {
            var me = this;
            await Task.Run(() =>
            {
                var path = Client.ConnectionInfo.Path;
                var str = HttpUtility.UrlDecode(path);
                var query = str.Replace("/?", "");
                string[] items = query.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                string[] values;
                Hashtable Result = new Hashtable();
                foreach (string item in items)
                {
                    values = item.Split('=');
                    Result.Add(values[0], values[1]);
                }
                ClientId = Result["id"].ToString();
                ClientName = Result["name"].ToString();
                Sign = Result["sign"].ToString();
                if (string.IsNullOrWhiteSpace(ClientId))
                {
                    Client.Close();
                    return;
                }
                if (string.IsNullOrWhiteSpace(ClientName))
                {
                    Client.Close();
                    return;
                }
                if (string.IsNullOrWhiteSpace(Sign))
                {
                    Client.Close();
                    return;
                }
                if (!CheckSign())
                {
                    Client.Close();
                    return;
                }
                var client = TcpServer.ClientList.Find(d => d.ClientId == ClientId && d.Id != Id);
                if (client != null) TcpServer.ClientList.Remove(client);
                Console.WriteLine(ClientId+"已加入");
            });
        }
        public override void OnError(Exception e)
        {
            if (e == null) return;
            Console.WriteLine(e.Message);
        }
        public override void OnClose()
        {
            Console.WriteLine(ClientId+"已退出");
        }

    }
}
