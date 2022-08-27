using Fleck;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SMApp
{
    public abstract class ServerSocketConnection
    {
        //private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        public int Id { get; set; }
        public IWebSocketConnection Client { get; set; }
        public DateTime Hearttime { get; set; }
        public virtual void OnOpen()
        {

        }
        public virtual void OnMessage(byte[] data, string id)
        {

        }
        public virtual void OnError(Exception e)
        {

        }
        public virtual void OnSenderror(byte[] data, string id)
        {

        }
        public virtual void OnOpenMessage(byte[] data)
        {

        }
        public virtual void OnClose()
        {

        }
        public async Task<bool> Send(byte[] newdata, string id = "")
        {
            var task = Task.Run(async () =>
             {
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
                     await Client.Send(alldata);
                     return true;
                 }
                 catch (Exception e)
                 {
                     OnError(e);
                     OnSenderror(newdata, id);
                     return false;
                 }
             });
            return await task;
        }
        public async Task<bool> Send(string str, string id = "")
        {
            var data = Encoding.UTF8.GetBytes(str);
            return await Send(data, id);
        }
        public async void SendOpenData(byte[] newdata)
        {
            await Task.Run(() =>
            {
                try
                {
                    byte[] sign = new byte[] { 0 };
                    byte[] alldata = new byte[newdata.Length + sign.Length];
                    sign.CopyTo(alldata, 0);
                    newdata.CopyTo(alldata, sign.Length);
                    Client.Send(alldata);
                }
                catch (Exception e)
                {
                    OnError(e);
                }

            });
        }
    }
}
