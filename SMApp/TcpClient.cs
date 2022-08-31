﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace SMApp
{
    public class TcpClient
    {
        static bool isclose = false;
        static string ip = "";
        static string id = "";
        static string name = "";
        static bool isrun = false;
        static string head = "ws";
        static int port;
        static SocketClient client { get; set; } = null;
        static ConcurrentQueue<MsgData> Errqueue = new ConcurrentQueue<MsgData>();
        static Channel<bool> errdatechan { get; set; } = Channel.CreateUnbounded<bool>();
        async static void CreateSocket()
        {
            await Task.Run(() =>
            {
                string sign = Md5.getmd5("xsdrj" + id + name);
                client = new SocketClient($"{head}://{ip}:{port}/?id={id}&name={name}&sign={sign}", onMessage: OnMessage, onError: OnError, onheartError: OnHeartError);
                client.Connect();
            });
        }
        async static void OnMessage(string id, byte[] data)
        {
            char sign = id[0];
            var newdata = GZip.Decompress(data);
            var str = System.Text.Encoding.UTF8.GetString(newdata);
            byte[] returndata = null;
            if (sign == '0' && OnReceiveObj != null)
            {
                var obj = OnReceiveObj(str);
                returndata = GZip.Compress(System.Text.Encoding.UTF8.GetBytes(obj.ToJson()));
            }
            if (sign == '1' && OnReceiveFile != null)
            {
                returndata = OnReceiveFile(str);
            }
            var issend = await client.Send(returndata, id);
            if (!issend)
            {
                client.Dealheart("对象发送失败");
                wrieerrorqueue(data, id);
            }
        }
        static void OnError(Exception e)
        {
            if (e == null) return;
            if (Onerror != null) Onerror(e);
        }
        static void OnHeartError(Exception e)
        {
            if (isclose) return;
            CreateSocket();
        }
        static async void wrieerrorqueue(byte[] data, string id)
        {
            MsgData msg = new MsgData
            {
                id = id,
                Data = data
            };
            Errqueue.Enqueue(msg);
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(10);
                await errdatechan.Writer.WriteAsync(true, cts.Token);
            }
            catch
            {
                
            }
        }
        public static Func<string, object> OnReceiveObj { get; set; } = null;
        public static Func<string, byte[]> OnReceiveFile { get; set; } = null;
        public static Action<Exception> Onerror { get; set; }
        public static void Close()
        {
            isclose = true;
            isrun = false;
            client.Close();
        }
        public static void Init(string ip, int port, string id, string name, bool isssl = false)
        {
            isclose = false;
            TcpClient.ip = ip;
            TcpClient.id = id;
            TcpClient.name = name;
            TcpClient.port = port;
            if (isssl) head = "wss";
            CreateSocket();
            if (!isrun)
            {
                isrun = true;
                Task.Run(async () =>
                {
                    while (true)
                    {
                        if (Errqueue.Count == 0)
                        {
                            var re = await errdatechan.Reader.ReadAsync();
                            if (!re) continue;
                        }
                        if (Errqueue.Count > 0)
                        {
                            MsgData data = null;
                            Errqueue.TryDequeue(out data);
                            var issend = await client.Send(data.Data, data.id);
                            if (!issend) Errqueue.Enqueue(data);
                        }
                    }
                });
            }
        }

    }
    public class MsgData
    {
        public string id { get; set; }
        public byte[] Data { get; set; }
    }
}
