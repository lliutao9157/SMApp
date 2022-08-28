using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using WebSocketSharp;

namespace SMApp
{
    public class SocketClient
    {
        WebSocket client;
        Channel<string> heartchan { get; set; } = Channel.CreateUnbounded<string>();
        Action<string, byte[]> OnMessage { get; set; }
        Action<Exception> OnError { get; set; }
        Action<Exception> OnHeartError { get; set; }
        Action<byte[]> OnOpenMessage { get; set; }
        Action OnDisconnect { get; set; }
        Action OnClose { get; set; }
        Action OnOpen { get; set; }
        private CancellationTokenSource clientcts = new CancellationTokenSource();
        public SocketClient(string url, Action<string, byte[]> onMessage = null, Action<Exception> onError = null, Action<Exception> onheartError = null, Action onClose = null, Action onDisconnect = null, Action<byte[]> onOpenMessage = null, Action onOpen = null)
        {
            OnMessage = onMessage;
            OnError = onError;
            OnClose = onClose;
            OnHeartError = onheartError;
            OnDisconnect = onDisconnect;
            OnOpenMessage = onOpenMessage;
            OnOpen = onOpen;
            client = new WebSocket(url);
            client.OnMessage += onmessage;
            client.OnClose += onclose;
            client.OnError += onerror;
            client.OnOpen += onpen;

        }
        public async void Connect()
        {
            await Task.Run(() => {
                client.Connect();
                Checklive();
            });

        }
        private async void Dealdata(byte[] data)
        {
            if (clientcts.IsCancellationRequested) return;
            await Task.Run(() =>
            {
                try
                {
                    var sign = data[0];
                    if (sign == 2)
                    {
                        var iddata = data.SubArray(1, 32);
                        var newdata = data.SubArray(33, data.Length - 33);
                        string id = Encoding.UTF8.GetString(iddata);
                        if (OnMessage != null) OnMessage(id, newdata);
                    }
                    if (sign == 1)
                    {
                        Dealheart("success");
                    }
                    if (sign == 0)
                    {
                        var newdata = data.SubArray(1, data.Length - 1);
                        if (OnOpenMessage != null) OnOpenMessage(newdata);
                    }
                }
                catch (Exception e)
                {
                    if (OnError != null) OnError(e);
                }
            });
        }
        public async void Dealheart(string result)
        {
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(10);
                await heartchan.Writer.WriteAsync(result, cts.Token);
            }
            catch
            {

            }
        }
        public async Task<bool> Send(byte[] newdata, string id = "")
        {
            return await Task.Run(() => {
                try
                {
                    var iddatatemp = Encoding.UTF8.GetBytes(id);
                    byte[] iddata = new byte[32];
                    iddatatemp.CopyTo(iddata, 0);
                    byte[] sign = new byte[] { 2 };
                    byte[] alldata = new byte[newdata.Length + iddata.Length + sign.Length];
                    sign.CopyTo(alldata, 0);
                    iddata.CopyTo(alldata, sign.Length);
                    newdata.CopyTo(alldata, sign.Length + iddata.Length);
                    client.Send(alldata);
                    return true;
                }
                catch(Exception e)
                {
                    if (OnError != null) OnError(e);
                    return false;
                }
            });

        }
        public async Task<bool> Send(string str, string id = "")
        {
            var data = Encoding.UTF8.GetBytes(str);
            return await Send(data, id);
        }
        public void SendOpenData(byte[] newdata)
        {
            try
            {
                byte[] sign = new byte[] { 0 };
                byte[] alldata = new byte[newdata.Length + sign.Length];
                sign.CopyTo(alldata, 0);
                newdata.CopyTo(alldata, sign.Length);
                var buffer = new ArraySegment<byte>(alldata);
                client.Send(alldata);
            }
            catch(Exception e)
            {
                if (OnError != null) OnError(e);
            }
        }
        private async void Sendheart()
        {
            await Task.Run(() =>
            {
                Thread.Sleep(5000);
                byte[] sign = new byte[] { 1 };
                try
                {
                    client.Send(sign);
                }
                catch(Exception e)
                {
                    if (OnError != null) OnError(e);
                }
            });
        }
        private async void Checklive()
        {
            if (clientcts.IsCancellationRequested) return;
            try
            {
                Sendheart();
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(10000);
                var result = await heartchan.Reader.ReadAsync(cts.Token);
                if (result == "success")
                {
                    Checklive();
                }
                else throw new Exception(result);
            }
            catch (Exception e)
            {
                if (clientcts.IsCancellationRequested) return;
                if (OnHeartError != null) OnHeartError(e);
            }
        }
        public async void Close()
        {
            await Task.Run(() => {
                clientcts.Cancel();
                if (client != null) client.Close();
            });

        }
        private void onmessage(object sender, MessageEventArgs e)
        {
            try
            {
                Dealdata(e.RawData);
            }
            catch (Exception ex)
            {
                if (OnError != null) OnError(ex);
            }

        }
        private async void onpen(object sender, EventArgs e)
        {
            await Task.Run(() => {
                if (OnOpen != null) OnOpen();
            });
        }
        private async void onclose(object sender, CloseEventArgs e)
        {
            await Task.Run(() => {
                if (OnClose != null) OnClose();
            });
        }
        private async void onerror(object sender, ErrorEventArgs e)
        {
            await Task.Run(() => {
                if (OnError != null) OnError(e.Exception);
            });
        }

    }

}
