using System.Security.Principal;
using System.Text;

namespace SMApp
{
    public class HttpContext
    {
        #region Private Fields
        private HttpRequest _req;
        private HttpResponse _res;
        private Stream _stream;
        private string _errorMessage;
        private int _errorStatusCode;
        private HttpConnecton _httpConnecton;
        private IPrincipal _user;
        private AuthenticationSchemes _authSchemes;
        private Func<HttpRequest, AuthenticationSchemes> _authSchemeSelector;
        private string _realm;
        private readonly string _defaultRealm;
        private Func<IIdentity, NetworkCredential> _userCredFinder;
        private bool _disposed;
        private string _objectName;


        #endregion

        #region Internal Constructors
        internal HttpContext(HttpConnecton httpConnecton)
        {
            _stream = httpConnecton.Stream;
            _httpConnecton = httpConnecton;
            _req = new HttpRequest(this);
            _res = new HttpResponse(this);
            _defaultRealm = "SECRET AREA";
            _disposed = false;
            _authSchemes = AuthenticationSchemes.Anonymous;
            _objectName = GetType().ToString();
        }
        #endregion

        #region Public Properties
        public AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(_objectName);

                return _authSchemes;
            }

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(_objectName);

                _authSchemes = value;
            }
        }

        public HttpRequest Request
        {
            get
            {
                return _req;
            }
        }
        public HttpResponse Response
        {
            get
            {
                return _res;
            }
        }
        public Stream NetWorkStream
        {
            get
            {
                return _stream;
            }
        }
        public IPrincipal User
        {
            get
            {
                return _user;
            }
        }
        public string Realm => _realm;
        public Func<IIdentity, NetworkCredential> UserCredentialsFinder
        {
            get
            {
                return _userCredFinder;
            }

            set
            {
                _userCredFinder = value;
            }
        }
        public bool Disposed
        {
            get { return _disposed; }
            set { _disposed = value; }
        }
        #endregion

        #region Internal Properties
        internal Func<HttpRequest, AuthenticationSchemes> AuthenticationSchemeSelector
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(_objectName);
                return _authSchemeSelector;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(_objectName);
                _authSchemeSelector = value;
            }
        }
        internal string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }

            set
            {
                _errorMessage = value;
            }
        }
        internal int ErrorStatusCode
        {
            get
            {
                return _errorStatusCode;
            }

            set
            {
                _errorStatusCode = value;
            }
        }
        internal HttpConnecton Connection
        {
            get { return _httpConnecton; }
        }
        internal bool HasErrorMessage
        {
            get
            {
                return _errorMessage != null;
            }
        }
        #endregion

        #region Private Methods
        private static string createErrorContent(int statusCode, string statusDescription, string message)
        {
            return message != null && message.Length > 0
                   ? String.Format(
                       "<html><body><h1>{0} {1} ({2})</h1></body></html>",
                       statusCode,
                       statusDescription,
                       message
                     )
                   : String.Format(
                       "<html><body><h1>{0} {1}</h1></body></html>",
                       statusCode,
                       statusDescription
                     );
        }
        private bool authenticateClient()
        {
            var schm = selectAuthenticationScheme();

            if (schm == AuthenticationSchemes.Anonymous)
                return true;

            if (schm == AuthenticationSchemes.None)
            {
                var msg = "Authentication not allowed";
                SendError(403, msg);
                return false;
            }

            var realm = getRealm();

            if (!SetUser(schm, realm, _userCredFinder))
            {
                SendAuthenticationChallenge(schm, realm);
                return false;
            }
            return true;
        }
        private AuthenticationSchemes selectAuthenticationScheme()
        {
            var selector = _authSchemeSelector;
            if (selector == null) return AuthenticationSchemes.Anonymous;
            try
            {
                return selector(_req);
            }
            catch
            {
                return AuthenticationSchemes.None;
            }
        }
        private string getRealm()
        {
            var realm = _realm;
            return realm != null && realm.Length > 0 ? realm : _defaultRealm;
        }

        #endregion

        #region  Internal Methods
        internal bool SetUser(AuthenticationSchemes scheme, string realm, Func<IIdentity, NetworkCredential> credentialsFinder)
        {
            var user = HttpUtility.CreateUser(
                         _req.Headers["Authorization"],
                         scheme,
                         realm,
                         _req.HttpMethod,
                         credentialsFinder
                       );

            if (user == null)
                return false;

            if (!user.Identity.IsAuthenticated)
                return false;

            _user = user;
            return true;
        }
        internal void SendError()
        {
            try
            {
                _res.StatusCode = _errorStatusCode;
                _res.ContentType = "text/html";
                var content = createErrorContent(
                                _errorStatusCode,
                                _res.StatusDescription,
                                _errorMessage
                              );
                var enc = Encoding.UTF8;
                var entity = enc.GetBytes(content);
                _res.ContentEncoding = enc;
                _res.ContentLength64 = entity.LongLength;
                _res.OutputStream.Write(entity, 0, entity.Length);
            }
            catch (Exception e)
            {
                _httpConnecton.Logger.Error(e.Message);
            }
            finally
            {
                _httpConnecton.Close();
            }
        }
        internal void SendError(int statusCode)
        {
            _errorStatusCode = statusCode;
            SendError();
        }
        internal void SendError(int statusCode, string message)
        {
            _errorStatusCode = statusCode;
            _errorMessage = message;
            SendError();
        }
        internal void SendAuthenticationChallenge(AuthenticationSchemes scheme, string realm)
        {
            _res.StatusCode = 401;
            var chal = new AuthenticationChallenge(scheme, realm).ToString();
            _res.Headers.InternalSet("WWW-Authenticate", chal, true);
            _res.Close();
        }

        internal bool RegisterContext()
        {
            if (!authenticateClient())
                return false;
            return true;
        }
        #endregion
    }
}
