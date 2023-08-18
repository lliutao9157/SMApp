using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;


namespace SMApp
{
    public class HttpServer
    {
        #region Private Fields
        private Socket _socket;
        private IPEndPoint _endpoint;
        private ServerSslConfiguration _sslConfig;
        private bool _secure;
        private Logger _logger;
        private WebSocketServiceManager _services;
        private string _certpath;
        private string _certpassword;
        private int _port;
        private int _timeout;
        private Func<HttpRequest, AuthenticationSchemes> _authSchemeSelector;
        private AuthenticationSchemes _authSchemes;
        #endregion

        #region Public Constructors

        public HttpServer(Logger logger, int port, string certpath, string certpassword)
        {
            _logger = logger;
            _services = new WebSocketServiceManager(logger);
            Certpath = certpath;
            Certpassword = certpassword;
            Port = port;
            _timeout = 15000;
            _sslConfig = new ServerSslConfiguration();
        }
        public HttpServer(int port,string certpath, string certpassword) :this(new Logger(),port,certpath,certpassword)
        {

        }
        public HttpServer(int port):this(port,null,null)
        {

        }
        #endregion

        #region Public Events
        public Action<HttpContext>? OnHttpReceive;

        #endregion

        #region Public Properties
        public AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                return _authSchemes;
            }

            set
            {
                _authSchemes = value;
            }
        }
        public Func<HttpRequest, AuthenticationSchemes> AuthenticationSchemeSelector
        {
            get { return _authSchemeSelector; }
            set { _authSchemeSelector = value; }
        }

        public ServerSslConfiguration SslConfiguration
        {
            get
            {
                return _sslConfig;
            }
        }

        public bool Secure
        {
            get
            {
                return _secure;
            }
        }
        public TimeSpan WaitTime
        {
            get
            {
                return _services.WaitTime;
            }

            set
            {
                _services.WaitTime = value;
            }
        }
        public Logger Logger => _logger;
        public string Certpath
        { 
            get { return _certpath; }
            set { _certpath = value; }
        }
        public string Certpassword
        {
            get => _certpassword;
            set { _certpassword = value; }
        }
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
        public int TimeOut
        {
            get { 
                return _timeout; 
            }
            set { _timeout = value; }
        }
        #endregion

        #region Private Methods
        private void OnAccept(IAsyncResult ar)
        {
            Socket sock = null;
            Socket socket = ar.AsyncState as Socket;
            try
            {
                sock = socket.EndAccept(ar);
            }
            catch
            {

            }
            try
            {
                socket.BeginAccept(new AsyncCallback(OnAccept), socket);
            }
            catch
            {
                if (sock != null) sock.Close();
                return;
            }
            var conn= new HttpConnecton(sock, this);
            conn.Init();
        }
        #endregion

        #region Internal Methods
        internal void HttpReceive(HttpContext httpContext)
        {
            try
            {
                if (OnHttpReceive != null) OnHttpReceive(httpContext);
            }
            catch(Exception e) 
            {
                Logger.Error(e.Message);
            }
            finally
            {
                httpContext.Connection.Init();
            }
        }
        internal bool InternalTryGetServiceHost(string path, out WebSocketServiceHost host)
        {
            path = path.TrimSlashFromEnd();
            return _services.InternalTryGetServiceHost(path, out host);

        }
        #endregion

        #region Public Methods
        //创建http监听器
        public void Start()
        {
            if (!string.IsNullOrWhiteSpace(_certpath)) _secure = true;
            if (!string.IsNullOrWhiteSpace(_certpath))
            {
                _sslConfig.ServerCertificate = new X509Certificate2(_certpath, _certpassword);
                _sslConfig.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            }
            _endpoint = new IPEndPoint(IPAddress.Any, _port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_endpoint);
            _socket.Listen(500);
            _socket.BeginAccept(new AsyncCallback(OnAccept), _socket);
            try
            {
                _services.Start();
            }
            catch
            {
                _services.Stop(1011, String.Empty);
            }
        }
        public void AddWebSocketService<TBehavior>(string path) where TBehavior : WebSocketBehavior, new()
        {
            _services.AddService<TBehavior>(path, null);
        }
        public void RemoveWebSocketServer(string path)
        {
            _services.RemoveService(path);
        }
        public void Stop()
        {
            _services.Stop(1001, String.Empty);
            _socket.Dispose();
        }
        #endregion
    }
}
