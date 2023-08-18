using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;


namespace SMApp
{
    public class HttpRequest : HttpBase
    {

        #region Private Fields
        private readonly byte[] _100continue;
        private readonly Encoding _defaultEncoding;
        private Encoding _contentEncoding;
        private HttpContext _context;
        private bool _chunked;
        private string _httpMethod;
        private long _contentLength;
        private Stream _inputStream;
        private WebHeaderCollection _headers;
        private string _userHostName;
        private Version _protocolVersion;
        private string _rawUrl;
        private Uri _url;
        private NameValueCollection _queryString;
        private CookieCollection _cookies;
        private string _method;
        private string _target;
        #endregion

        #region Private Constructors
        private HttpRequest(string method, string target, Version version, NameValueCollection headers) : base(version, headers)
        {
            _method = method;
            _target = target;
        }

        #endregion

        #region Pirvate Properties
        private string HeaderSection
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

        #region Internal Properties

        internal string RequestLine
        {
            get
            {
                return String.Format(
                         "{0} {1} HTTP/{2}{3}", _method, _target, ProtocolVersion, CrLf
                       );
            }
        }

        #endregion

        #region Internal Constructors
        internal HttpRequest(string method, string target) : this(method, target, HttpVersion.Version11, new WebHeaderCollection())
        {
            Headers["User-Agent"] = "smapp/1.0";
        }
        internal HttpRequest(HttpContext context, WebHeaderCollection header) : base(HttpVersion.Version11, new WebHeaderCollection())
        {
            _100continue = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
            _defaultEncoding = Encoding.UTF8;
            _context = context;
            _contentLength = -1;
            _headers = header;
        }
        internal HttpRequest(HttpContext context)  : this(context, new WebHeaderCollection())
        {
           
        }
        #endregion

        #region Public Properties
 
        public bool Chunked
        {
            get
            {
                return _chunked;
            }
        }
        public string HttpMethod
        {
            get
            {
                return _httpMethod;
            }
        }
        public bool KeepAlive
        {
            get
            {
                return _headers.KeepsAlive(_protocolVersion);
            }
        }
        public Stream InputStream => _inputStream;
        public Version ProtocolVersion => _protocolVersion;
        public bool Isload { get; set; } = false;
        public string RequestId { get; set; }
        public Encoding ContentEncoding
        {
            get
            {
                if (_contentEncoding == null)
                    _contentEncoding = getContentEncoding();
                return _contentEncoding;
            }
        }
        public long ContentLength64
        {
            get
            {
                return _contentLength;
            }
        }
        public string formdata { get; set; }
        public string jsondata { get; set; }
        public List<AppFile> appFiles { get; set; }
        public Hashtable appForm { get; set; }
        public byte[] requestdata { get; set; }
        public string Authority { get; set; }
        public string httphead { get; set; }
        public WebHeaderCollection Headers => _headers;
        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = _headers.GetCookies(false);

                return _cookies;
            }
        }
        public string Account { get; set; }
        public string UserHostAddress { get; set; }
        public string UserAgent { get; set; }
        public string[] UserLanguages { get; set; }
        public string RawUrl => _rawUrl;
        public string UserHostName => _userHostName;
        public System.Net.IPEndPoint RemoteEndPoint
        {
            get
            {
                return _context.Connection.RemoteEndPoint;
            }
        }
        public bool IsSecureConnection
        {
            get
            {
                return _context.Connection.Secure;
            }
        }
        public Uri Url
        {
            get
            {
                if (_url == null)
                {
                    _url = HttpUtility
                           .CreateRequestUrl(
                             _rawUrl,
                             _userHostName,
                             IsWebSocketRequest,
                             IsSecureConnection
                           );
                }

                return _url;
            }
        }
        public NameValueCollection QueryString
        {
            get
            {
                if (_queryString == null)
                {
                    var url = Url;
                    var query = url != null ? url.Query : null;

                    _queryString = QueryStringCollection.Parse(query, _defaultEncoding);
                }

                return _queryString;
            }
        }
        public string ContentType
        {
            get
            {
                return _headers["Content-Type"];
            }
        }
        public bool IsWebSocketRequest
        {
            get
            {
                return _httpMethod == "GET" && _headers.Upgrades("websocket");
            }
        }
        public bool IsAuthenticated
        {
            get
            {
                return _context.User != null;
            }
        }
        public bool IsLocal
        {
            get
            {
                return _context.Connection.IsLocal;
            }
        }
        public System.Net.IPEndPoint LocalEndPoint
        {
            get
            {
                return _context.Connection.LocalEndPoint;
            }
        }
        public override string MessageHeader
        {
            get
            {
                return RequestLine + HeaderSection;
            }
        }
 


        #endregion

        #region Public Methods
        private Encoding getContentEncoding()
        {
            var val = _headers["Content-Type"];

            if (val == null)
                return _defaultEncoding;

            Encoding ret;

            return HttpUtility.TryGetEncoding(val, out ret)
                   ? ret
                   : _defaultEncoding;
        }
        public void SetEncoding(Encoding encoding)
        {
            _contentEncoding = encoding;
        }
        public void LoadData()
        {
            if (Isload) return;
            long totallen = 0;
            int bufferlen = 102400;
            using (MemoryStream ms = new MemoryStream())
            {
                if (ContentType != null && ContentType.Contains("multipart/form-data"))
                {
                    try
                    {
                        MultipartFormDataParser formDataParser = MultipartFormDataParser.Parse(InputStream);
                        List<AppFile> appFiles = new List<AppFile>();
                        Hashtable ha = new Hashtable();
                        foreach (var file in formDataParser.Files)
                        {
                            var data = new byte[file.Data.Length];
                            file.Data.Read(data, 0, data.Length);
                            AppFile file1 = new AppFile()
                            {
                                FileName = file.FileName,
                                FileType = file.ContentType,
                                Name = file.Name,
                                FileData = data
                            };
                            appFiles.Add(file1);
                        }
                        foreach (var par in formDataParser.Parameters)
                        {
                            ha.Add(par.Name, par.Data);
                        }
                        if (appFiles.Count > 0) this.appFiles = appFiles;
                        if (ha.Count > 0) appForm = ha;
                        totallen = ContentLength64;
                    }
                    catch
                    {
                        totallen = 0;
                    }
                }
                else if (ContentType != null && ContentType.Contains("application/json"))
                {

                    var bufferdata = new byte[bufferlen];
                    while (true)
                    {
                        var near = InputStream.Read(bufferdata, 0, bufferdata.Length);
                        if (near == 0) break;
                        ms.Write(bufferdata, 0, near);
                        totallen += near;
                    }
                    var jsonstr = getContentEncoding().GetString(ms.ToArray().SubArray(0, ms.Length));
                    this.jsondata = jsonstr;
                }
                else if (ContentType != null && ContentType.Contains("application/x-www-form-urlencoded"))
                {

                    var bufferdata = new byte[bufferlen];
                    while (true)
                    {
                        var near = InputStream.Read(bufferdata, 0, bufferdata.Length);
                        if (near == 0) break;
                        ms.Write(bufferdata, 0, near);
                        totallen += near;
                    }
                    var formstr = getContentEncoding().GetString(ms.ToArray().SubArray(0, ms.Length));
                    this.formdata = formstr;
                }
                else
                {
                    var bufferdata = new byte[bufferlen];
                    while (true)
                    {
                        var near = InputStream.Read(bufferdata, 0, bufferdata.Length);
                        if (near == 0) break;
                        ms.Write(bufferdata, 0, near);
                        totallen += near;
                    }
                    requestdata = ms.ToArray().SubArray(0, ms.Length);
                }
                Isload = true;
            }
        }
        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return;

            var buff = new StringBuilder(64);

            foreach (var cookie in cookies.Sorted)
            {
                if (cookie.Expired)
                    continue;

                buff.AppendFormat("{0}; ", cookie);
            }

            var len = buff.Length;

            if (len <= 2)
                return;

            buff.Length = len - 2;

            Headers["Cookie"] = buff.ToString();
        }
        #endregion

        #region Internal Methods
        internal static HttpRequest CreateWebSocketHandshakeRequest(Uri targetUri)
        {
            var ret = new HttpRequest("GET", targetUri.PathAndQuery);
            var headers = ret.Headers;
            var port = targetUri.Port;
            var schm = targetUri.Scheme;
            var defaultPort = (port == 80 && schm == "ws")
                              || (port == 443 && schm == "wss");
            headers["Host"] = !defaultPort
                              ? targetUri.Authority
                              : targetUri.DnsSafeHost;
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";
            return ret;
        }
        internal static HttpRequest CreateConnectRequest(Uri targetUri)
        {
            var host = targetUri.DnsSafeHost;
            var port = targetUri.Port;
            var authority = String.Format("{0}:{1}", host, port);
            var ret = new HttpRequest("CONNECT", authority);
            ret.Headers["Host"] = port != 80 ? authority : host;
            return ret;
        }
        internal void AddHeader(string headerField)
        {
            var start = headerField[0];

            if (start == ' ' || start == '\t')
            {
                _context.ErrorMessage = "Invalid header field";

                return;
            }

            var colon = headerField.IndexOf(':');

            if (colon < 1)
            {
                _context.ErrorMessage = "Invalid header field";

                return;
            }

            var name = headerField.Substring(0, colon).Trim();

            if (name.Length == 0 || !name.IsToken())
            {
                _context.ErrorMessage = "Invalid header name";

                return;
            }

            var val = colon < headerField.Length - 1
                      ? headerField.Substring(colon + 1).Trim()
            : String.Empty;

            _headers.InternalSet(name, val, false);

            var lower = name.ToLower(CultureInfo.InvariantCulture);

            if (lower == "host")
            {
                if (_userHostName != null)
                {
                    _context.ErrorMessage = "Invalid Host header";

                    return;
                }

                if (val.Length == 0)
                {
                    _context.ErrorMessage = "Invalid Host header";

                    return;
                }

                _userHostName = val;

                return;
            }

            if (lower == "content-length")
            {
                if (_contentLength > -1)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";

                    return;
                }

                long len;

                if (!Int64.TryParse(val, out len))
                {
                    _context.ErrorMessage = "Invalid Content-Length header";

                    return;
                }

                if (len < 0)
                {
                    _context.ErrorMessage = "Invalid Content-Length header";

                    return;
                }

                _contentLength = len;

                return;
            }
        }
        internal void FinishInitialization()
        {
            if (_userHostName == null)
            {
                _context.ErrorMessage = "Host header required";

                return;
            }

            var transferEnc = _headers["Transfer-Encoding"];

            if (transferEnc != null)
            {
                var compType = StringComparison.OrdinalIgnoreCase;

                if (!transferEnc.Equals("chunked", compType))
                {
                    _context.ErrorStatusCode = 501;
                    _context.ErrorMessage = "Invalid Transfer-Encoding header";

                    return;
                }

                _chunked = true;
            }

            if (_httpMethod == "POST" || _httpMethod == "PUT")
            {
                if (_contentLength == -1 && !_chunked)
                {
                    _context.ErrorStatusCode = 411;
                    _context.ErrorMessage = "Content-Length header required";

                    return;
                }
            }

            var expect = _headers["Expect"];

            if (expect != null)
            {
                var compType = StringComparison.OrdinalIgnoreCase;

                if (!expect.Equals("100-continue", compType))
                {
                    _context.ErrorStatusCode = 417;
                    _context.ErrorMessage = "Invalid Expect header";

                    return;
                }

                var output = _context.Response.OutputStream;
                output.Write(_100continue, 0, _100continue.Length);
            }
        }
        internal bool Upgrades(Hashtable headers, string protocol)
        {
            return (headers.ContainsKey("Upgrade") && headers["Upgrade"].ToStr().Contains(protocol))
             && (headers.ContainsKey("Connection") && headers["Connection"].ToStr().Contains("Connection"));
        }
        internal bool KeepsAlive(Hashtable headers, Version version)
        {
            return version < HttpVersion.Version11
                   ? (headers.ContainsKey("Connection") && headers["Connection"].ToStr().Contains("keep-alive"))
                   : !(headers.ContainsKey("Connection") && headers["Connection"].ToStr().Contains("close"));
        }
        internal bool TryGetEncoding(string contentType, out Encoding result)
        {
            result = null;
            try
            {
                result = GetEncoding(contentType);
            }
            catch
            {
                return false;
            }

            return result != null;
        }
        internal Encoding GetEncoding(string contentType)
        {
            var name = "charset=";
            var compType = StringComparison.OrdinalIgnoreCase;

            foreach (var elm in SplitHeaderValue(contentType, ';'))
            {
                var part = elm.Trim();

                if (!part.StartsWith(name, compType))
                    continue;

                var val = GetValue(part, '=', true);

                if (val == null || val.Length == 0)
                    return null;

                return Encoding.GetEncoding(val);
            }

            return null;
        }
        internal IEnumerable<string> SplitHeaderValue(string value, params char[] separators)
        {
            var len = value.Length;
            var end = len - 1;

            var buff = new StringBuilder(32);
            var escaped = false;
            var quoted = false;

            for (var i = 0; i <= end; i++)
            {
                var c = value[i];
                buff.Append(c);

                if (c == '"')
                {
                    if (escaped)
                    {
                        escaped = false;

                        continue;
                    }

                    quoted = !quoted;

                    continue;
                }

                if (c == '\\')
                {
                    if (i == end)
                        break;

                    if (value[i + 1] == '"')
                        escaped = true;

                    continue;
                }

                if (Array.IndexOf(separators, c) > -1)
                {
                    if (quoted)
                        continue;

                    buff.Length -= 1;

                    yield return buff.ToString();

                    buff.Length = 0;

                    continue;
                }
            }

            yield return buff.ToString();
        }
        internal string GetValue(string nameAndValue, char separator, bool unquote)
        {
            var idx = nameAndValue.IndexOf(separator);

            if (idx < 0 || idx == nameAndValue.Length - 1)
                return null;

            var val = nameAndValue.Substring(idx + 1).Trim();

            return unquote ? Unquote(val) : val;
        }
        internal string Unquote(string value)
        {
            var first = value.IndexOf('"');

            if (first == -1)
                return value;

            var last = value.LastIndexOf('"');

            if (last == first)
                return value;

            var len = last - first - 1;

            return len > 0
                   ? value.Substring(first + 1, len).Replace("\\\"", "\"")
                   : String.Empty;
        }
        internal void SetRequestStream(byte[] initialBuffer)
        {
            if(_context.Request.Chunked) _inputStream=new ChunkedRequestStream(_context, initialBuffer);
            else  _inputStream = new RequestStream(_context, initialBuffer, _contentLength);
        }
        internal void SetRequestLine(string requestLine)
        {
            var parts = requestLine.Split(new[] { ' ' }, 3);

            if (parts.Length < 3)
            {
                _context.ErrorMessage = "Invalid request line (parts)";

                return;
            }

            var method = parts[0];

            if (method.Length == 0)
            {
                _context.ErrorMessage = "Invalid request line (method)";

                return;
            }

            if (!method.IsHttpMethod())
            {
                _context.ErrorStatusCode = 501;
                _context.ErrorMessage = "Invalid request line (method)";

                return;
            }

            var target = parts[1];

            if (target.Length == 0)
            {
                _context.ErrorMessage = "Invalid request line (target)";

                return;
            }

            var rawVer = parts[2];

            if (rawVer.Length != 8)
            {
                _context.ErrorMessage = "Invalid request line (version)";

                return;
            }

            if (!rawVer.StartsWith("HTTP/", StringComparison.Ordinal))
            {
                _context.ErrorMessage = "Invalid request line (version)";

                return;
            }

            Version ver;

            if (!rawVer.Substring(5).TryCreateVersion(out ver))
            {
                _context.ErrorMessage = "Invalid request line (version)";

                return;
            }

            if (ver != HttpVersion.Version11)
            {
                _context.ErrorStatusCode = 505;
                _context.ErrorMessage = "Invalid request line (version)";

                return;
            }
            _httpMethod = method;
            _rawUrl = target;
            _protocolVersion = ver;
        }
        internal HttpResponse GetResponse(Stream stream, int millisecondsTimeout)
        {
            WriteTo(stream);
            return HttpResponse.ReadResponse(stream, millisecondsTimeout);
        }
        #endregion
    }
}
