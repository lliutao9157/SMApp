using System.Collections.Specialized;
using System.Globalization;
using System.Text;

namespace SMApp
{
    public class HttpResponse : HttpBase
    {
        #region Private Fields
        private HttpContext _context;
        private WebHeaderCollection _headers;
        private string _contentType;
        private Encoding _contentEncoding;
        private bool _sendChunked;
        private long _contentLength;
        private bool _keepAlive;
        private int _statusCode;
        private Uri _redirectLocation;
        private CookieCollection _cookies;
        private Stream _outputStream;
        private Version _version;
        private string _statusDescription;
        private bool _issendheader;
        private string _reason;
        private MemoryStream _buffer;
        #endregion

        #region Internal Propertes
        internal string StatusLine
        {
            get
            {
                if (_reason != null)
                {
                    return String.Format("HTTP/{0} {1} {2}{3}", ProtocolVersion, _statusCode, _reason, CrLf);
                }
                return String.Format("HTTP/{0} {1} {2}{3}",_version, _statusCode, _statusDescription,CrLf);
            }
        }

        #endregion

        #region Protected Propertes
        protected string HeaderSection
        {
            get
            {
                var buff = new StringBuilder(64);

                foreach (var key in _headers.AllKeys)
                    buff.AppendFormat("{0}: {1}{2}", key, _headers[key], CrLf);

                buff.Append(CrLf);

                return buff.ToString();
            }
        }
        #endregion

        #region Internal Constructors
        internal HttpResponse(HttpStatusCode code, WebHeaderCollection headers) : base(HttpVersion.Version11, headers)
        {
            _statusCode = (int)code;
            _headers = headers;
            _headers["Server"] = "smapp/1.0";
        }
        internal HttpResponse(HttpStatusCode code) : this(code, new WebHeaderCollection())
        {

        }
        internal HttpResponse(HttpContext context, WebHeaderCollection headers) : base(HttpVersion.Version11, headers)
        {
            _context = context;
            _outputStream = new ResponseStream(context);
            _statusCode = 200;
            _statusDescription = "OK";
            _version = HttpVersion.Version11;
            _issendheader = false;
            _headers = headers;
            _buffer=new MemoryStream();
        }
        internal HttpResponse(HttpContext context) : this(context, new WebHeaderCollection(HttpHeaderType.Response, true))
        {

        }
        #endregion

        #region Private Constructors
        private HttpResponse(int code, string reason, Version version, NameValueCollection headers): base(version, headers)
        {
            _statusCode = code;
            _reason = reason;
        }
        #endregion

        #region Public Properties
        public override string MessageHeader
        {
            get
            {
                return StatusLine + HeaderSection;
            }
        }
        public Stream OutputStream
        {
            get
            {
                if (_contentEncoding == null) _contentEncoding = Encoding.UTF8;
                if (!_issendheader) SendHeader();
                return _outputStream;
            }
        }
        public string StatusDescription
        {
            get { return _statusDescription; }
            set { _statusDescription = value; }
        }
        public Encoding ContentEncoding
        {
            get
            {
                if (_contentEncoding == null) _contentEncoding = Encoding.UTF8;
                return _contentEncoding;
            }
            set { _contentEncoding = value; }
        }
        public long ContentLength64
        {
            get { return _contentLength; }
            set { _contentLength = value; }
        }
        public string ContentType
        {
            get { return _contentType; }
            set { _contentType = value; }
        }
        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = _headers.GetCookies(false);

                return _cookies;
            }
        }
        public WebHeaderCollection Headers
        {
            get
            {
                return _headers;
            }
        }
        public bool KeepAlive
        {
            get { return _keepAlive; }
            set { _keepAlive = value; }
        }
        public MemoryStream ContentData => _buffer;
        public int StatusCode
        {
            get
            {
                return _statusCode;
            }

            set
            {
                if (value < 100 || value > 999)
                {
                    var msg = "A value is not between 100 and 999 inclusive.";
                    throw new System.Net.ProtocolViolationException(msg);
                }

                _statusCode = value;
                _statusDescription = value.GetStatusDescription();
            }
        }
        public string RedirectLocation
        {
            get
            {
                return _redirectLocation != null
                       ? _redirectLocation.OriginalString
                       : null;
            }

            set
            {

                if (_issendheader)
                {
                    var msg = "The response is already being sent.";
                    throw new InvalidOperationException(msg);
                }

                if (value == null)
                {
                    _redirectLocation = null;
                    return;
                }

                if (value.Length == 0)
                {
                    var msg = "An empty string.";
                    throw new ArgumentException(msg, "value");
                }

                Uri uri;
                if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
                {
                    var msg = "Not an absolute URL.";
                    throw new ArgumentException(msg, "value");
                }

                _redirectLocation = uri;
            }
        }
        public bool IsAbort { get; set; } = false;
        public string RedirectUrl { get; set; }
        public bool IsStream { get; set; } = false;
        public long Length { get; set; }
        public int Partlength { get; set; }
        public bool Isloadcatch { get; set; } = false;
        public bool Isnopage { get; set; }

        #endregion

        #region Public Methods

        public void Abort()
        {
            IsAbort = true;
        }
        public void AppendHeader(string name, string value)
        {
            Headers.Add(name, value);
        }
        public void SetHeader(string name, string value)
        {
            Headers.Set(name, value);
        }
        public void Redirect(string url)
        {
            if (_issendheader)
            {
                var msg = "The response is already being sent.";
                throw new InvalidOperationException(msg);
            }

            if (url == null)
                throw new ArgumentNullException("url");

            if (url.Length == 0)
            {
                var msg = "An empty string.";
                throw new ArgumentException(msg, "url");
            }
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                var msg = "Not an absolute URL.";
                throw new ArgumentException(msg, "url");
            }

            _redirectLocation = uri;
            _statusCode = 302;
            _statusDescription = "Found";
        }
        public void Write(byte[] data)
        {
            _buffer.Write(data, 0, data.Length);
            _buffer.Position= 0;
        }
        public void WriteStream(byte[] data)
        {
            Write(data);
            IsStream = true;
        }
        public Encoding GetEncoding()
        {
            return ContentEncoding;
        }

        public void Close()
        {
            if (_contentEncoding == null) _contentEncoding = Encoding.UTF8;
            if (!_issendheader) SendHeader();
        }
        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return;

            var headers = Headers;

            foreach (var cookie in cookies.Sorted)
            {
                var val = cookie.ToResponseString();

                headers.Add("Set-Cookie", val);
            }
        }
        #endregion

        #region Internal Properties
        internal WebHeaderCollection FullHeaders
        {
            get
            {
                var headers = new WebHeaderCollection(HttpHeaderType.Response, true);

                if (_headers != null)
                    headers.Add(_headers);

                if (_contentType != null)
                {
                    headers.InternalSet(
                      "Content-Type",
                      createContentTypeHeaderText(_contentType, _contentEncoding),
                      true
                    );
                }
                if (headers["Server"] == null)
                    headers.InternalSet("Server", "smapp/1.0", true);

                if (headers["Date"] == null)
                {
                    headers.InternalSet(
                      "Date",
                      DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture),
                      true
                    );
                }

                if (_sendChunked)
                {
                    headers.InternalSet("Transfer-Encoding", "chunked", true);
                }
                else
                {
                    headers.InternalSet(
                      "Content-Length",
                      _contentLength.ToString(CultureInfo.InvariantCulture),
                      true
                    );
                }

                /*
                 * Apache forces closing the connection for these status codes:
                 * - 400 Bad Request
                 * - 408 Request Timeout
                 * - 411 Length Required
                 * - 413 Request Entity Too Large
                 * - 414 Request-Uri Too Long
                 * - 500 Internal Server Error
                 * - 503 Service Unavailable
                 */
                var closeConn = !_context.Request.KeepAlive
                                || !_keepAlive
                                || _statusCode == 400
                                || _statusCode == 408
                                || _statusCode == 411
                                || _statusCode == 413
                                || _statusCode == 414
                                || _statusCode == 500
                                || _statusCode == 503;

                var reuses = _context.Connection.Reuses;

                if (closeConn || reuses >= 100)
                {
                    headers.InternalSet("Connection", "close", true);
                }
                else
                {
                    headers.InternalSet(
                      "Keep-Alive",
                      String.Format("timeout={0},max={1}", _context.Connection.TimeOut/1000, 100 - reuses),
                      true
                    );

                    if (_context.Request.ProtocolVersion < HttpVersion.Version11)
                        headers.InternalSet("Connection", "keep-alive", true);
                }

                if (_redirectLocation != null)
                    headers.InternalSet("Location", _redirectLocation.AbsoluteUri, true);

                if (_cookies != null)
                {
                    foreach (var cookie in _cookies)
                    {
                        headers.InternalSet(
                          "Set-Cookie",
                          cookie.ToResponseString(),
                          true
                        );
                    }
                }
                return headers;
            }
        }
        public bool IsRedirect
        {
            get
            {
                return _statusCode == 301 || _statusCode == 302;
            }
        }
        public bool IsUnauthorized
        {
            get
            {
                return _statusCode == 401;
            }
        }
        public bool IsWebSocketResponse
        {
            get
            {
                return _version > HttpVersion.Version10
                       && _statusCode == 101
                       && Headers.Upgrades("websocket");
            }
        }
        public bool CloseConnection
        {
            get
            {
                var compType = StringComparison.OrdinalIgnoreCase;

                return Headers.Contains("Connection", "close", compType);
            }
        }
        public bool IsProxyAuthenticationRequired
        {
            get
            {
                return _statusCode == 407;
            }
        }
        public bool IsSuccess
        {
            get
            {
                return _statusCode >= 200 && _statusCode <= 299;
            }
        }
        #endregion

        #region Internal Methods
        internal static HttpResponse CreateCloseResponse(HttpStatusCode code)
        {
            var ret = new HttpResponse(code);
            ret.Headers["Connection"] = "close";
            return ret;
        }
        internal void SendHeader()
        {
            var headers = FullHeaders;
            if (_contentEncoding == null) _contentEncoding = Encoding.UTF8;
            byte[] statusline_to_bytes = _contentEncoding.GetBytes(StatusLine);
            _outputStream.Write(statusline_to_bytes, 0, statusline_to_bytes.Length);
            var headerstr = headers.ToStringMultiValue(true);
            byte[] header_to_bytes = _contentEncoding.GetBytes(headerstr);
            _outputStream.Write(header_to_bytes, 0, header_to_bytes.Length);
            _issendheader = true;
        }
        internal static HttpResponse CreateWebSocketHandshakeResponse()
        {
            var ret = new HttpResponse(HttpStatusCode.SwitchingProtocols);
            var headers = ret.Headers;
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";
            return ret;
        }
        internal static HttpResponse Parse(string[] messageHeader)
        {
            var len = messageHeader.Length;

            if (len == 0)
            {
                var msg = "An empty response header.";

                throw new ArgumentException(msg);
            }

            var slParts = messageHeader[0].Split(new[] { ' ' }, 3);
            var plen = slParts.Length;

            if (plen < 2)
            {
                var msg = "It includes an invalid status line.";

                throw new ArgumentException(msg);
            }

            var code = slParts[1].ToInt32();
            var reason = plen == 3 ? slParts[2] : null;
            var ver = slParts[0].Substring(5).ToVersion();

            var headers = new WebHeaderCollection();

            for (var i = 1; i < len; i++)
                headers.InternalSet(messageHeader[i], true);

            return new HttpResponse(code, reason, ver, headers);
        }
        internal static HttpResponse ReadResponse(Stream stream, int millisecondsTimeout)
        {
            return Read<HttpResponse>(stream, Parse, millisecondsTimeout);
        }
        #endregion

        #region  Private methods

        private string createContentTypeHeaderText(string value, Encoding encoding)
        {
            if (value.IndexOf("charset=", StringComparison.Ordinal) > -1)
                return value;

            if (encoding == null)
                return value;

            return String.Format("{0}; charset={1}", value, encoding.WebName);
        }
        #endregion
    }
}
