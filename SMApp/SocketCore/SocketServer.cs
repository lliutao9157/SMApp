using Fleck;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMApp
{
    public class SocketServer<T> where T : ServerSocketConnection, new()
    {
        private WebSocketServer server;
        private CancellationTokenSource cts { get; set; } = new CancellationTokenSource();
        Action OnClose { get; set; }
        public List<T> ClientList { get; set; }
        public SocketServer(string url, Action onClose = null, string certpath = null, string certpassword = null)
        {
            server = new WebSocketServer(url);
            OnClose = onClose;
            if (!string.IsNullOrWhiteSpace(certpath))
            {
                X509Certificate2 serverCertificate = null;
                server.EnabledSslProtocols = SslProtocols.Tls12;
                if (string.IsNullOrWhiteSpace(certpassword)) serverCertificate = new X509Certificate2(certpath);
                else serverCertificate = new X509Certificate2(certpath, certpassword);
                server.Certificate = serverCertificate;
            }
        }
        public void Open()
        {
            ClientList = new List<T>();
            server.Start(socket =>
            {
                T connection = new T();
                connection.Id = socket.GetHashCode();
                connection.Hearttime = DateTime.Now;
                connection.Client = socket;
                ClientList.Add(connection);
                socket.OnOpen = connection.OnOpen;
                socket.OnError = connection.OnError;
                socket.OnClose = () =>
                {
                    ClientList.Remove(connection);
                    connection.OnClose();
                };
                socket.OnBinary = data =>
                {
                    Dealdata(data, connection);
                };
            });
            Dealclientlist();
        }
        private async void Dealdata(byte[] data, T connection)
        {
            await Task.Run(() =>
            {
                try
                {
                    var client = ClientList.Find(d => d.Id == connection.Id);
                    if (client == null) return;
                    var sign = data[0];
                    if (sign == 2)
                    {
                        var iddata = data.SubArray(1, 32);
                        var msgdata = data.SubArray(33, data.Length - 33);
                        string id = Encoding.UTF8.GetString(iddata, 0, iddata.Length);
                        connection.OnMessage(msgdata, id);
                    }
                    if (sign == 1)
                    {
                        Dealheart(connection);
                    }
                    if (sign == 0)
                    {
                        var msgdata = data.SubArray(1, data.Length - 1);
                        connection.OnOpenMessage(msgdata);
                    }
                }
                catch (Exception e)
                {
                    connection.OnError(e);
                }
            });
        }
        private async void Dealheart(T connection)
        {
            await Task.Run(() =>
            {
                try
                {
                    connection.Hearttime = DateTime.Now;
                    byte[] sign = new byte[] { 1 };
                    connection.Client.Send(sign);
                }
                catch (Exception e)
                {
                    connection.OnError(e);
                }
            });

        }
        private async void Dealclientlist()
        {
            if (cts.IsCancellationRequested) return;
            await Task.Run(() =>
            {
                Thread.Sleep(30000);
                for (var i = 0; i < ClientList.Count; i++)
                {
                    T client = ClientList[0];
                    if ((DateTime.Now - client.Hearttime).TotalSeconds < 60) continue;
                    ClientList.Remove(client);
                }
                Dealclientlist();
            });
        }
        public async void Close()
        {
            await Task.Run(() =>
            {
                cts.Cancel();
                ClientList.ForEach(d =>
                {
                    if (d.Client != null) d.Client.Close();
                });
                ClientList.Clear();
                if (OnClose != null) OnClose();
            });

        }

    }
}
