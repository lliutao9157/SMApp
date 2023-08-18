using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;



namespace SMApp
{
    internal class HttpConnecton
    {
        #region Private Fields
        private Socket _socket;
        private HttpServer _server;
        private bool _secure;
        private int _bufferLength;
        private int _maxInputLength;
        private InputState _inputState;
        private int _timeout;
        private Timer _timer;
        private object _async;
        private Stream _stream;
        private int _reuses;
        private StringBuilder _currentLine;
        private LineState _lineState;
        private EndPoint _localEndPoint;
        private EndPoint _remoteEndPoint;
        private Logger _logger;
        private byte[] _buffer;
        private MemoryStream _requestBuffer;
        private int _position;
        private HttpContext _context;
        private bool _disposed;
        #endregion

        #region Internal Properties
        internal Stream Stream => _stream;
        internal int Reuses => _reuses;
        #endregion

        #region Internal Constructors
        internal HttpConnecton(Socket socket, HttpServer server)
        {
            _socket = socket;
            _server = server;
            _secure = server.Secure;
            _bufferLength = 8192;
            _maxInputLength = 32768;
            _localEndPoint = socket.LocalEndPoint;
            _remoteEndPoint = socket.RemoteEndPoint;
            _timer = new Timer(onTimeout, this, Timeout.Infinite, Timeout.Infinite);
            _async = new object();
            _stream = CreateNetWorkStream();
            _reuses = 0;
            _logger = server.Logger;
            _buffer = new byte[_bufferLength];

        }
        #endregion

        #region Public Properties
        public bool Secure => _secure;
        public IPEndPoint LocalEndPoint => (IPEndPoint)_localEndPoint;
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return (IPEndPoint)_remoteEndPoint;
            }
        }
        public bool IsLocal
        {
            get
            {
                return ((IPEndPoint)_remoteEndPoint).Address.IsLocal();
            }
        }
        public Logger Logger => _logger;
        public int TimeOut => _timeout;
        public bool Disposed => _disposed;
        #endregion

        #region Internal Methods
        internal void Init()
        {
            lock (_async)
            {
                if (_socket == null) return;
                _requestBuffer = new MemoryStream();
                _timeout = _server.TimeOut;
                _currentLine = new StringBuilder(64);
                _inputState = InputState.RequestLine;
                _lineState = LineState.None;
                _position = 0;
                _context = new HttpContext(this);
                if (_server.AuthenticationSchemeSelector != null) _context.AuthenticationSchemeSelector = _server.AuthenticationSchemeSelector;
                _context.AuthenticationSchemes = _server.AuthenticationSchemes;
                _reuses++;
                BeginRequest();
            }
        }
        internal void Close()
        {
            lock (_async)
            {
                if (_socket == null) return;
                disposeTimer();
                disposeStream();
                disposeRequestBuffer();
                closeSocket();
                _disposed = true;
            }
        }
        #endregion

        #region private Methods
        private void BeginRequest()
        {
            try
            {
                if (_stream == null) return;
                _stream.BeginRead(_buffer, 0, _buffer.Length, onRead, this);
            }
            catch
            {
                Close();
            }
        }
        private void onRead(IAsyncResult ar)
        {
            try
            {
                if (_stream == null) return;
                int neard = _stream.EndRead(ar);
                if (neard <= 0)
                {
                    Close();
                    return;
                }
                _requestBuffer.Write(_buffer, 0, neard);
                if (!processInput())
                {
                    BeginRequest();
                    return;
                }
                if (!_context.HasErrorMessage) _context.Request.FinishInitialization();
                if (_context.HasErrorMessage)
                {
                    _context.SendError();
                    return;
                }
                if (!_context.RegisterContext())
                {
                    Close();
                    return;
                }
                if (_context.Request.IsWebSocketRequest)
                {
                    var ctx = new HttpListenerWebSocketContext(_context, null);
                    var uri = ctx.RequestUri;
                    if (uri == null)
                    {
                        ctx.Close(HttpStatusCode.BadRequest);
                        return;
                    }
                    var path = uri.AbsolutePath;
                    if (path.IndexOfAny(new[] { '%', '+' }) > -1)
                        path = HttpUtility.UrlDecode(path, Encoding.UTF8);
                    WebSocketServiceHost host;
                    if (!_server.InternalTryGetServiceHost(path, out host))
                    {
                        ctx.Close(HttpStatusCode.NotImplemented);
                        return;
                    }
                    host.StartSession(ctx);
                    return;
                }
                if (_context.Request.ContentLength64 > 0) _context.Request.SetRequestStream(_requestBuffer.GetBuffer().SubArray(_position, _requestBuffer.Length.ToInt() - _position));
                disposeRequestBuffer();
                _timer.Change(_timeout, Timeout.Infinite);
                _server.HttpReceive(_context);
                if (_timer != null) _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch
            {
                Close();
            }
        }
        private void closeSocket()
        {

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }
            _socket.Close();
            _socket = null;
        }
        private void disposeTimer()
        {
            if (_timer == null)
                return;
            try
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch
            {
            }
            _timer.Dispose();
            _timer = null;
        }
        private void disposeStream()
        {
            if (_stream == null) return;
            _stream.Dispose();
            _stream = null;
        }
        private void disposeRequestBuffer()
        {
            if (_requestBuffer == null)
                return;
            _requestBuffer.Dispose();
            _requestBuffer = null;
        }
        //创建stream
        private Stream CreateNetWorkStream()
        {
            try
            {
                var netStream = new NetworkStream(_socket, false);
                var sslConf = _server.SslConfiguration;
                if (_server.Secure)
                {
                    var sslStream = new SslStream(netStream, false, sslConf.ClientCertificateValidationCallback);
                    sslStream.AuthenticateAsServer(
                    sslConf.ServerCertificate,
                    sslConf.ClientCertificateRequired,
                    sslConf.EnabledSslProtocols,
                    sslConf.CheckCertificateRevocation);
                    return sslStream;
                }
                else
                {
                    return netStream;
                }
            }
            catch
            {
                return null;
            }

        }
        private bool processInput()
        {
            byte[] data = _requestBuffer.GetBuffer();
            var _req = _context.Request;
            try
            {
                while (true)
                {
                    int nread;
                    var line = readLineFrom(data, _position, _requestBuffer.Length, out nread);
                    _position += nread;
                    if (line == null)
                        break;
                    if (line.Length == 0)
                    {
                        if (_inputState == InputState.RequestLine)
                            continue;
                        if (_position > _maxInputLength)
                            _context.ErrorMessage = "Headers too long";
                        return true;
                    }
                    if (_inputState == InputState.RequestLine)
                    {
                        _req.SetRequestLine(line);
                        _inputState = InputState.Headers;
                    }
                    else
                    {
                        _req.AddHeader(line);
                    }
                    if (_context.HasErrorMessage) return true;
                }
            }
            catch (Exception)
            {
                // TODO: Logging.

                _context.ErrorMessage = "Processing failure";

                return true;
            }

            if (_position >= _maxInputLength)
            {
                _context.ErrorMessage = "Headers too long";
                return true;
            }

            return false;
        }
        private string readLineFrom(byte[] buffer, int offset, long length, out int nread)
        {
            nread = 0;
            for (var i = offset; i < length; i++)
            {
                nread++;
                var b = buffer[i];
                if (b == 13)
                {
                    _lineState = LineState.Cr;
                    continue;
                }
                if (b == 10)
                {
                    _lineState = LineState.Lf;
                    break;
                }
                _currentLine.Append((char)b);
            }
            if (_lineState != LineState.Lf) return null;
            var ret = _currentLine.ToString();
            _currentLine.Length = 0;
            _lineState = LineState.None;
            return ret;
        }
        private void onTimeout(object state)
        {
            if (_context == null) return;
            lock (_context)
            {
                if (_context.Disposed) return;
                _context.Disposed = true;
            }
            _context.SendError(408);
        }
        #endregion
    }
}