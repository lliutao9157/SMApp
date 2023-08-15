using Newtonsoft.Json;
using SMApp.MvcModels;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SMApp
{
    public class Smserver
    {
        #region Private Fields
        private Logger _logger;
        private Func<Exception, HttpError> _onHttpError;
        private List<TimeTaskApp> _timeTaskApps;
        private List<InitTaskApp> _initTaskApps;
        private ConcurrentDictionary<string, Routeinfo> _routedic;
        private List<CloseTaskApp> _closeTaskApps;
        private List<AppInfo>? _appInfoList;
        private Hashtable _apps;
        private List<RouteWatch> _fileSystemWatchers;
        private int _bufferlen;
        private ConcurrentDictionary<int, HttpServer> _httpservers;
        #endregion

        #region Public Constructors
        public Smserver()
        {
            _logger = new Logger();
            _timeTaskApps = new List<TimeTaskApp>();
            _initTaskApps = new List<InitTaskApp>();
            _routedic = new ConcurrentDictionary<string, Routeinfo>();
            _httpservers = new ConcurrentDictionary<int, HttpServer>();
            _closeTaskApps = new List<CloseTaskApp>();
            _fileSystemWatchers = new List<RouteWatch>();
            _bufferlen = 51200;
            _apps = new Hashtable();
        }
        #endregion

        #region public Properties
        public Logger Logger => _logger;
        public Func<Exception, HttpError> OnHttpError
        {
            get
            {
                return _onHttpError;
            }
            set
            {
                _onHttpError = value;
            }
        }
        public ConcurrentDictionary<int, HttpServer> Httpservers => _httpservers;
        #endregion

        #region private methods
        private void Dealhttpmessage(HttpContext e)
        {
            Routeinfo routeinfo = null;
            var request = e.Request;
            if (request.HttpMethod == "OPTIONS")
            {
                Return200(e);
                return;
            }
            string url = request.Url.LocalPath;
            HttpRequest httpRequest = e.Request;
            HttpResponse response = e.Response;
            try
            {
                _routedic.TryGetValue(request.HttpMethod + ":" + url.ToLower(), out routeinfo);
                if (routeinfo != null)
                {
                    routeinfo.BeforRequest(e);
                    if (routeinfo.Isstatic) Getfile(routeinfo, e);
                    else Getapi(routeinfo, e);
                    routeinfo.AfterRequest(e);
                    return;
                }
                Return404(e);
            }
            catch (Exception ex)
            {
                var exception = ex.InnerException ?? ex;
                DealError(e, exception);
                if (routeinfo != null) routeinfo.AfterRequest(e, exception);
            }
        }
        private void Return404(HttpContext e)
        {
            e.Response.StatusCode = 404;
            throw new Exception("未找到页面");
        }
        private void Return200(HttpContext e)
        {
            var res = e.Response;
            var request = e.Request;
            res.StatusCode = 200;
            res.Headers.Add("Access-Control-Allow-Origin", request.Headers["Origin"].ToStr());
            res.Headers.Add("Access-Control-Allow-Methods", "*");
            res.Headers.Add("Access-Control-Allow-Headers", "*");
            res.Headers.Add("Access-Control-Expose-Headers", "*");
            res.Headers.Add("Access-Control-Allow-Credentials", "true");
            Sendhttpmessage(e, new byte[] { });
        }
        private  async void Sendhttpmessageforfile(HttpContext e, Stream m)
        {
            lock (e)
            {
                if (e.Disposed) return;
                e.Disposed = true;
            }
            string range = "";
            if (e.Request.Headers.AllKeys.ToList().Contains("Range")) range = e.Request.Headers["Range"].ToStr();
            try
            {
                var res = e.Response;
                res.Headers.Add("accept-ranges", "bytes");
                res.ContentLength64 = m.Length;
                res.KeepAlive = true;
                long total = res.ContentLength64;
                if (!string.IsNullOrWhiteSpace(range))
                {
                    long ranges = 0;
                    long rangee = 0;
                    range = range.Replace("bytes=", "");
                    string[] rs = range.Split(new char[] { '-' });
                    ranges = rs[0].ToLong();
                    rangee = rs[1].ToLong();
                    if (rangee == 0) rangee = m.Length - 1;
                    if (ranges > 0 || rangee < m.Length - 1)
                    {
                        res.ContentLength64 = rangee - ranges + 1;
                        total = res.ContentLength64;
                        res.Headers.Add("Content-Range", $"bytes {ranges}-{rangee}/{m.Length}");
                        res.StatusCode = 206;
                        m.Position = ranges;
                    }
                }
                byte[] buffer = new byte[_bufferlen];
                var nread = 0;
                while (true)
                {
                    int len = buffer.Length;
                    if (total < len) len = total.ToInt();
                    nread = m.Read(buffer, 0, len);
                    if (nread <= 0)
                    {
                        m.Close();
                        m.Flush();
                        break;
                    }
                    await  e.Response.OutputStream.WriteAsync(buffer, 0, nread);
                    total = total - nread;
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
        private async void Sendhttpmessage(HttpContext e, byte[] contents)
        {
            lock (e)
            {
                if (e.Disposed) return;
                e.Disposed = true;
            }
            try
            {
                MemoryStream m = new MemoryStream(contents);
                var res = e.Response;
                res.ContentLength64 = contents.LongLength;
                res.KeepAlive = true;
                byte[] buffer = new byte[_bufferlen];
                var nread = 0;
                while (true)
                {
                    nread = m.Read(buffer, 0, buffer.Length);
                    if (nread <= 0)
                    {
                        m.Close();
                        m.Flush();
                        break;
                    }
                    await e.Response.OutputStream.WriteAsync(buffer, 0, nread);
                }
            }
            catch(Exception ex) {
                Logger.Error(ex.Message);
            }
        }
        private void Getapi(Routeinfo routeinfo, HttpContext e)
        {
            var res = e.Response;
            var req = e.Request;
            if (req.HttpMethod == "POST" && req.ContentLength64 > 0) req.LoadData();
            if (!string.IsNullOrWhiteSpace(res.RedirectUrl))
            {
                Regex reg = new Regex(@"^(https://|http://).+");
                Match match = reg.Match(res.RedirectUrl.ToLower());
                if (!match.Success) res.RedirectUrl = $"{req.httphead}://{req.Authority}{res.RedirectUrl}";
                res.Redirect(res.RedirectUrl);
                res.KeepAlive = true;
                res.Close();
                return;
            }
            byte[] contents = null;
            var acceptEncoding = "";
            if (req.Headers.AllKeys.Contains("Accept-Encoding")) acceptEncoding = req.Headers["Accept-Encoding"].ToStr();
            if (req.Headers.AllKeys.Contains("Origin"))
            {
                res.Headers.Add("Access-Control-Allow-Origin", req.Headers["Origin"].ToStr());
                res.Headers.Add("Access-Control-Allow-Methods", "*");
                res.Headers.Add("Access-Control-Allow-Headers", "*");
                res.Headers.Add("Access-Control-Expose-Headers", "*");
                res.Headers.Add("Access-Control-Allow-Credentials", "true");
            }
            var zip = ZipFactory.GetZip(acceptEncoding);
            if (res.IsAbort)
            {
                contents = res.ContentData == null ? new byte[0] : res.ContentData;
                if (res.IsStream)
                {
                    Stream m = new MemoryStream(contents);
                    Sendhttpmessageforfile(e, m);
                }
                else
                {
                    if (zip != null)
                    {
                        res.Headers.Add("Content-Encoding", zip.Ziptype);
                        contents = zip.Compress(contents);
                    }
                    Sendhttpmessage(e, contents);
                }
                return;
            }
            ActionResult actionresult = null;
            if (routeinfo.Action != null) actionresult = routeinfo.Action(e);
            else if (routeinfo.Instance != null && routeinfo.Method != null) actionresult = DealRequest(e, routeinfo);
            else throw new Exception("未找到数据");
            if (!string.IsNullOrWhiteSpace(res.RedirectUrl))
            {
                Regex reg = new Regex(@"^(https://|http://).+");
                Match match = reg.Match(res.RedirectUrl.ToLower());
                if (!match.Success) res.RedirectUrl = $"{req.httphead}://{req.Authority}{res.RedirectUrl}";
                res.Redirect(res.RedirectUrl);
                res.KeepAlive = true;
                res.Close();
                return;
            }
            if (res.IsAbort)
            {
                contents = res.ContentData;
                if (contents == null) contents = new byte[0];
                if (res.IsStream)
                {
                    Stream m = new MemoryStream(contents);
                    Sendhttpmessageforfile(e, m);
                }
                else
                {
                    if (zip != null)
                    {
                        res.Headers.Add("Content-Encoding", zip.Ziptype);
                        contents = zip.Compress(contents);
                    }
                    res.ContentLength64 = contents.Length;
                    Sendhttpmessage(e, contents);
                }
                return;
            }
            var etag = actionresult.Etag;
            switch (actionresult.Name)
            {
                case "JsonResult":
                    var jsonresult = (JsonResult)actionresult;
                    res.ContentType = "application/json";
                    var jsondata = res.ContentEncoding.GetBytes(jsonresult.Data.ToJson());
                    res.Write(jsondata);
                    if (zip != null)
                    {
                        res.Headers.Add("Content-Encoding", zip.Ziptype);
                        contents = zip.Compress(res.ContentData);
                    }
                    else
                    {
                        contents = res.ContentData;
                    }
                    break;
                case "ContentResult":
                    var contentresult = (ContentResult)actionresult;
                    var contentdata = res.ContentEncoding.GetBytes(contentresult.Data);
                    res.ContentType = "text/html";
                    res.Write(contentdata);
                    if (zip != null)
                    {
                        res.Headers.Add("Content-Encoding", zip.Ziptype);
                        contents = zip.Compress(res.ContentData);
                    }
                    else
                    {
                        contents = res.ContentData;
                    }
                    break;
                case "RedirectResult":
                    var redirectresult = (RedirectResult)actionresult;
                    var url = redirectresult.Url;
                    Regex reg = new Regex(@"^(https://|http://).+");
                    Match match = reg.Match(url.ToLower());
                    if (!match.Success) url = $"{req.httphead}://{req.Authority}{url}";
                    res.Redirect(url);
                    res.KeepAlive = true;
                    res.Close();
                    break;
                case "FileResult":
                    var fileresult = (FileResult)actionresult;
                    res.ContentType = fileresult.ContentType;
                    res.Write(fileresult.Data);
                    if (fileresult.IsCompress && zip != null)
                    {
                        res.Headers.Add("Content-Encoding", zip.Ziptype);
                        contents = zip.Compress(res.ContentData);
                    }
                    else
                    {
                        contents = res.ContentData;
                    }
                    break;
                case "ObjectResult":
                    var objectresult = (ObjectResult)actionresult;
                    var objectdata = res.ContentEncoding.GetBytes(objectresult.Data.ToJson());
                    res.ContentType = "text/html";
                    res.Write(objectdata);
                    if (zip != null)
                    {
                        res.Headers.Add("Content-Encoding", zip.Ziptype);
                        contents = zip.Compress(res.ContentData);
                    }
                    else
                    {
                        contents = res.ContentData;
                    }
                    break;
                case "StreamResult":
                    var streamresult = (StreamResult)actionresult;
                    res.ContentType = streamresult.ContentType;
                    res.Write(streamresult.Data);
                    Stream m = new MemoryStream(res.ContentData);
                    Sendhttpmessageforfile(e, m);
                    return;
            }
            if (etag != null) res.Headers.Add("etag", etag);
            if (req.Headers.AllKeys.Contains("If-None-Match") && req.Headers["If-None-Match"].ToStr() == etag)
            {
                res.StatusCode = 304;
                res.KeepAlive = true;
                res.Close();
                return;
            }
            if (contents != null)
            {

                Sendhttpmessage(e, contents);
            }
        }
        private void Getfile(Routeinfo routeinfo, HttpContext e)
        {
            var response = e.Response;
            var request = e.Request;
            if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
            {
                Regex reg = new Regex(@"^(https://|http://).+");
                Match match = reg.Match(response.RedirectUrl.ToLower());
                if (!match.Success) response.RedirectUrl = $"{request.httphead}://{request.Authority}{response.RedirectUrl}";
                response.Redirect(response.RedirectUrl);
                response.KeepAlive = true;
                response.Close();
                return;
            }
            Stream m = null;
            var acceptEncoding = "";
            if (e.Request.Headers.AllKeys.Contains("Accept-Encoding")) acceptEncoding = e.Request.Headers["Accept-Encoding"].ToStr();
            var zip = ZipFactory.GetZip(acceptEncoding);

            if (response.IsAbort)
            {
                byte[] contents = response.ContentData == null ? new byte[0] : response.ContentData;
                if (response.IsStream)
                {
                    m = new MemoryStream(contents);
                    Sendhttpmessageforfile(e, m);
                }
                else
                {
                    if (zip != null)
                    {
                        response.Headers.Add("Content-Encoding", zip.Ziptype);
                        contents = zip.Compress(contents);
                    }
                    response.ContentLength64 = contents.Length;
                    Sendhttpmessage(e, contents);
                }
                return;
            }
            DateTime mtime = File.GetLastWriteTime(routeinfo.Path);
            response.ContentType = routeinfo.MimeType.Mimestr;
            response.Write(File.ReadAllBytes(routeinfo.Path));
            if (routeinfo.MimeType.compress && zip != null)
            {
                var newdata = zip.Compress(response.ContentData);
                m = new MemoryStream(newdata);
                response.Headers.Add("Content-Encoding", zip.Ziptype);
            }
            else
            {
                m = new MemoryStream(response.ContentData);
            }
            string etag = ETagCreator.Create(new { mtime = mtime, len = response.ContentData.Length });
            response.Headers.Add("etag", etag);
            if (request.Headers.AllKeys.Contains("If-None-Match") && request.Headers["If-None-Match"].ToStr() == etag)
            {
                response.StatusCode = 304;
                response.KeepAlive = true;
                response.Close();
                return;
            }
            Sendhttpmessageforfile(e, m);
        }
        private void DealError(HttpContext e, Exception ex)
        {
            try
            {
                if (_onHttpError != null)
                {
                    var error = _onHttpError(ex);
                    var res = e.Response;
                    byte[] data = res.ContentEncoding.GetBytes(error.Msg);
                    res.ContentType = error.Contenttype;
                    Sendhttpmessage(e, data);
                }
                else
                {
                    var res = e.Response;
                    byte[] data = res.ContentEncoding.GetBytes(ex.Message);
                    res.ContentType = "text/html";
                    Sendhttpmessage(e, data);
                }
            }
            catch { }
        }
        private ActionResult DealRequest(HttpContext context, Routeinfo routeinfo)
        {
            var threadid = Thread.CurrentThread.ManagedThreadId.ToString();
            CacheFactory.Cache.WriteCache(context, "context" + threadid);
            var request = context.Request;
            try
            {
                Hashtable QuryData = new Hashtable();
                if (request.formdata != null)
                {
                    var Querystr = request.formdata;
                    var datas = Querystr.Split(new char[] { '&' });
                    foreach (var data in datas)
                    {
                        var keyvalue = data.Split(new char[] { '=' });
                        try
                        {
                            QuryData.Add(keyvalue[0].Trim(), keyvalue[1].Trim());
                        }
                        catch
                        {
                            QuryData.Add(keyvalue[0].Trim(), "");
                        }
                    }
                }
                if (request.jsondata != null)
                {
                    QuryData = request.jsondata.ToObject<Hashtable>();
                }
                if (request.appForm != null)
                {
                    QuryData = request.appForm;
                }
                if (request.QueryString != null)
                {
                    foreach (var key in request.QueryString.AllKeys)
                    {
                        if (!QuryData.ContainsKey(key)) QuryData.Add(key, request.QueryString.Get(key));
                    }
                }
                if (request.requestdata != null)
                {
                    QuryData.Add("data", request.requestdata);
                }
                List<object> paramlist = new List<object>();
                ParameterInfo[] plist = routeinfo.Method.GetParameters();
                for (int i = 0; i < plist.Length; i++)
                {
                    if (plist[i].ParameterType.FullName == null)
                    {
                        object o = null;
                        if (QuryData.ContainsKey(plist[i].Name)) o = JsonConvert.DeserializeObject((QuryData[plist[i].Name]).ToJson());
                        paramlist.Add(o);
                    }
                    else
                    {
                        object o = null;
                        if (QuryData.ContainsKey(plist[i].Name)) o = JsonConvert.DeserializeObject((QuryData[plist[i].Name]).ToJson(), plist[i].ParameterType);
                        if (request.appFiles != null)
                        {
                            var appfiles = request.appFiles.FindAll(d => d.Name == plist[i].Name).ToList();
                            if (appfiles != null && appfiles.Count > 0) o = appfiles;
                        }
                        paramlist.Add(o);
                    }
                }
                var parameters = paramlist.ToArray();
                var re = routeinfo.Method.Invoke(routeinfo.Instance, parameters);
                if (re == null) return new ObjectResult(re);
                Type type = re.GetType();
                ActionResult result = null;
                if (type.Name == "JsonResult" || type.Name == "FileResult" || type.Name == "RedirectResult" || type.Name == "ContentResult" || type.Name == "ObjectResult" || type.Name == "StreamResult")
                {
                    result = re as ActionResult;
                }
                else
                {
                    result = new ObjectResult(re);
                }
                return result;
            }
            catch (Exception e)
            {
                var exception = e.InnerException ?? e;
                _logger.Error(exception.Message);
                throw e;
            }
            finally
            {
                CacheFactory.Cache.RemoveCache("context" + threadid);
            }
        }
        /// <summary>
        /// 加载应用
        /// </summary>
        /// <param name="appinfolist"></param>
        private void LoadApps(List<AppInfo> appinfolist)
        {
            if (_appInfoList == null) _appInfoList = new List<AppInfo>();
            foreach (var app in appinfolist)
            {
                var apptemp = _appInfoList.Find(d => d.name == app.name);
                if (apptemp != null) continue;
                _appInfoList.Add(app);
            }
            _appInfoList = appinfolist;
            if (_appInfoList == null || _appInfoList.Count == 0) return;
            _apps.Clear();
            _timeTaskApps.Clear();
            _initTaskApps.Clear();
            _closeTaskApps.Clear();
            _routedic.Clear();
            foreach (var appinfo in _appInfoList)
            {
                string key = appinfo.name;
                if (appinfo.usestaticfiles)
                {
                    string route = "";
                    if (!string.IsNullOrWhiteSpace(key.ToStr())) route += "/" + key;
                    string path = AppDomain.CurrentDomain.BaseDirectory;
                    if (!string.IsNullOrWhiteSpace(appinfo.rootdir))
                    {
                        path = path + appinfo.rootdir;
                    }
                    IList<FileInfo> lst = FileHelper.GetFiles(path);
                    foreach (FileInfo file in lst)
                    {
                        string routemethod = "";
                        string fpath = file.FullName.Replace("\\", "/").Replace(path.Replace("\\", "/"), "");
                        routemethod += route + fpath;
                        Routeinfo approuteinfo = new Routeinfo
                        {
                            Isstatic = true,
                            Isremote = false,
                            Path = file.FullName,
                            MimeType = FileContentType.GetMimeType(file.Extension),
                            AppInfo = appinfo
                        };
                        _routedic.TryAdd("GET:" + routemethod.ToLower(), approuteinfo);
                    }
                    FileWatcher(path, route);
                }
                if (appinfo.assembly==null) continue;
                Assembly assembly = appinfo.assembly;
                foreach (var t in assembly.GetTypes())
                {
                    if (!(typeof(ISMAppCreater).IsAssignableFrom(t) && !t.IsInterface && t.IsClass && !t.IsAbstract)) continue;
                    var app = Activator.CreateInstance(t);
                    _apps.Add(key+t.Name, app);
                    List<PropertyInfo> propertyInfos = app.GetType().GetProperties().ToList();
                    foreach (var propertyInfo in propertyInfos)
                    {
                        string route = "";
                        if (!string.IsNullOrWhiteSpace(key.ToStr())) route += "/" + key;
                        var routeinfo = propertyInfo.GetCustomAttribute(typeof(RouterAttribute)) as RouterAttribute;
                        if (routeinfo != null)
                        {
                            if (!string.IsNullOrWhiteSpace(routeinfo.name) && routeinfo.name != "/") route += "/" + routeinfo.name;
                            if (string.IsNullOrWhiteSpace(routeinfo.name)) route += "/" + propertyInfo.Name;
                        }
                        else
                        {
                            route += "/" + propertyInfo.Name;
                        }
                        object severinstance = propertyInfo.GetValue(app);
                        List<MethodInfo> methodInfos = propertyInfo.PropertyType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "TimetaskAttribute") != null);
                        foreach (var method in methodInfos)
                        {
                            var taskattribute = (TimetaskAttribute)method.GetCustomAttribute(typeof(TimetaskAttribute));
                            if (taskattribute == null) continue;
                            TimeTaskApp timetaskapp = new TimeTaskApp
                            {
                                instance = severinstance,
                                methodInfo = method,
                                AppInfo = appinfo,
                                Name = taskattribute.Name,
                                Content = taskattribute.Content,
                                Time = taskattribute.Time,
                                Iscycle = true,
                                Isopen = taskattribute.isopen,
                                CanExcute = true,
                                Times = 0
                            };
                            _timeTaskApps.Add(timetaskapp);
                        }
                        List<MethodInfo> methodInfosforinit = propertyInfo.PropertyType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "InitTaskAttribute") != null);
                        foreach (var method in methodInfosforinit)
                        {
                            var taskattribute = method.GetCustomAttribute(typeof(InitTaskAttribute));
                            if (taskattribute == null) continue;
                            InitTaskApp timetaskapp = new InitTaskApp
                            {
                                instance = severinstance,
                                methodInfo = method,
                                attribute = (InitTaskAttribute)taskattribute,
                                AppInfo = appinfo
                            };
                            _initTaskApps.Add(timetaskapp);
                        }
                        List<MethodInfo> methodInfosforclose = propertyInfo.PropertyType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "CloseTaskAttribute") != null);
                        foreach (var method in methodInfosforclose)
                        {
                            var taskattribute = method.GetCustomAttribute(typeof(CloseTaskAttribute));
                            if (taskattribute == null) continue;
                            CloseTaskApp timetaskapp = new CloseTaskApp
                            {
                                instance = severinstance,
                                methodInfo = method,
                                attribute = (CloseTaskAttribute)taskattribute,
                                AppInfo = appinfo
                            };
                            _closeTaskApps.Add(timetaskapp);
                        }
                        List<MethodInfo> methodInfosforwebapi = propertyInfo.PropertyType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "WebApiAttribute") != null);
                        foreach (var method in methodInfosforwebapi)
                        {
                            string routemethod = "";
                            var taskattribute = method.GetCustomAttribute(typeof(WebApiAttribute)) as WebApiAttribute;
                            if (string.IsNullOrWhiteSpace(taskattribute.Action)) routemethod += "/" + method.Name;
                            if (!string.IsNullOrWhiteSpace(taskattribute.Action) && taskattribute.Action != "/") routemethod += "/" + taskattribute.Action;
                            routemethod = route + routemethod;
                            Routeinfo approuteinfo = new Routeinfo
                            {
                                Method = method,
                                Instance = severinstance,
                                Isstatic = false,
                                Isremote = false,
                                AppInfo = appinfo
                            };
                            if (taskattribute.GET) _routedic.TryAdd("GET:" + routemethod.ToLower(), approuteinfo);
                            if (taskattribute.POST) _routedic.TryAdd("POST:" + routemethod.ToLower(), approuteinfo);
                        }
                    }
                    List<FieldInfo> fieldInfos = app.GetType().GetFields().ToList();
                    foreach (var fieldInfo in fieldInfos)
                    {
                        string route = "";
                        if (!string.IsNullOrWhiteSpace(key.ToStr())) route += "/" + key;
                        var routeinfo = fieldInfo.GetCustomAttribute(typeof(RouterAttribute)) as RouterAttribute;
                        if (routeinfo != null)
                        {
                            if (!string.IsNullOrWhiteSpace(routeinfo.name) && routeinfo.name != "/") route += "/" + routeinfo.name;
                            if (string.IsNullOrWhiteSpace(routeinfo.name)) route += "/" + fieldInfo.Name;
                        }
                        else
                        {
                            route += "/" + fieldInfo.Name;
                        }
                        object severinstance = fieldInfo.GetValue(app);
                        List<MethodInfo> methodInfos = fieldInfo.FieldType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "TimetaskAttribute") != null);
                        foreach (var method in methodInfos)
                        {
                            var taskattribute = (TimetaskAttribute)method.GetCustomAttribute(typeof(TimetaskAttribute));
                            if (taskattribute == null) continue;
                            TimeTaskApp timetaskapp = new TimeTaskApp
                            {
                                instance = severinstance,
                                methodInfo = method,
                                AppInfo = appinfo,
                                Name = taskattribute.Name,
                                Content = taskattribute.Content,
                                Time = taskattribute.Time,
                                Iscycle = true,
                                Isopen = taskattribute.isopen,
                                CanExcute = true,
                                Times = 0
                            };
                            _timeTaskApps.Add(timetaskapp);
                        }
                        List<MethodInfo> methodInfosforinit = fieldInfo.FieldType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "InitTaskAttribute") != null);
                        foreach (var method in methodInfosforinit)
                        {
                            var taskattribute = method.GetCustomAttribute(typeof(InitTaskAttribute));
                            if (taskattribute == null) continue;
                            InitTaskApp timetaskapp = new InitTaskApp
                            {
                                instance = severinstance,
                                methodInfo = method,
                                attribute = (InitTaskAttribute)taskattribute,
                                AppInfo = appinfo
                            };
                            _initTaskApps.Add(timetaskapp);
                        }
                        List<MethodInfo> methodInfosforclose = fieldInfo.FieldType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "CloseTaskAttribute") != null);
                        foreach (var method in methodInfosforclose)
                        {
                            var taskattribute = method.GetCustomAttribute(typeof(CloseTaskAttribute));
                            if (taskattribute == null) continue;
                            CloseTaskApp timetaskapp = new CloseTaskApp
                            {
                                instance = severinstance,
                                methodInfo = method,
                                attribute = (CloseTaskAttribute)taskattribute,
                                AppInfo = appinfo
                            };
                            _closeTaskApps.Add(timetaskapp);
                        }
                        List<MethodInfo> methodInfosforwebapi = fieldInfo.FieldType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "WebApiAttribute") != null);
                        foreach (var method in methodInfosforwebapi)
                        {
                            string routemethod = "";
                            var taskattribute = method.GetCustomAttribute(typeof(WebApiAttribute)) as WebApiAttribute;
                            if (string.IsNullOrWhiteSpace(taskattribute.Action)) routemethod += "/" + method.Name;
                            if (!string.IsNullOrWhiteSpace(taskattribute.Action) && taskattribute.Action != "/") routemethod += "/" + taskattribute.Action;
                            routemethod = route + routemethod;
                            if (string.IsNullOrWhiteSpace(routemethod)) routemethod = "/";
                            Routeinfo approuteinfo = new Routeinfo
                            {
                                Method = method,
                                Instance = severinstance,
                                Isstatic = false,
                                Isremote = false,
                                AppInfo = appinfo
                            };
                            if (taskattribute.GET) _routedic.TryAdd("GET:" + routemethod.ToLower(), approuteinfo);
                            if (taskattribute.POST) _routedic.TryAdd("POST:" + routemethod.ToLower(), approuteinfo);
                        }
                        }
                }
            }
        }
        //watchfile
        private void FileWatcher(string path, string route)
        {
            path = path.Replace("\\", "/");
            RouteWatch fileSystemWatcher = new RouteWatch();
            fileSystemWatcher.Route = route;
            fileSystemWatcher.Path = path;
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.Changed += new FileSystemEventHandler(OnProcess);
            fileSystemWatcher.Created += new FileSystemEventHandler(OnProcess);
            fileSystemWatcher.Renamed += new RenamedEventHandler(OnRenamed);
            fileSystemWatcher.Deleted += new FileSystemEventHandler(OnProcess);
            fileSystemWatcher.EnableRaisingEvents = true;
            _fileSystemWatchers.Add(fileSystemWatcher);
        }
        private void OnProcess(object source, FileSystemEventArgs e)
        {
            RouteWatch route = source as RouteWatch;
            string routemethod = "";
            string fpath = e.FullPath.Replace(route.Path, "").Replace("\\", "/");
            routemethod += route.Route + "/" + fpath;
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                FileInfo fi = new FileInfo(e.FullPath);
                Routeinfo approuteinfo = new Routeinfo
                {
                    Isstatic = true,
                    Isremote = false,
                    Path = e.FullPath,
                    MimeType = FileContentType.GetMimeType(fi.Extension)
                };
                _routedic.TryAdd("GET:" + routemethod.ToLower(), approuteinfo);
            }
            else if (e.ChangeType == WatcherChangeTypes.Changed) { }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                Routeinfo routeinfo;
                _routedic.TryRemove("GET:" + routemethod.ToLower(), out routeinfo);
            }
        }
        private void OnRenamed(object source, RenamedEventArgs e)
        {
            RouteWatch route = source as RouteWatch;
            string fpath = e.FullPath.Replace(route.Path, "").Replace("\\", "/");
            string oldpath = e.OldFullPath.Replace(route.Path, "").Replace("\\", "/");
            string routemethod = route.Route + "/" + fpath;
            string oldroutemethod = route.Route + "/" + oldpath;
            Routeinfo routeinfo;
            _routedic.TryRemove("GET:" + oldroutemethod.ToLower(), out routeinfo);
            FileInfo fi = new FileInfo(e.FullPath);
            Routeinfo approuteinfo = new Routeinfo
            {
                Isstatic = true,
                Isremote = false,
                Path = e.FullPath,
                MimeType = FileContentType.GetMimeType(fi.Extension)
            };
            _routedic.TryAdd("GET:" + routemethod.ToLower(), approuteinfo);
        }
        #endregion

        #region public methods
        public HttpServer CreateHttpServer(int port, string certpath = null, string certpassword = null)
        {
            var server = new HttpServer(_logger, port, certpath, certpassword);
            server.OnHttpReceive += Dealhttpmessage;
            _httpservers.TryAdd(port, server);
            return server;
        }
        public void RemoveHttpServer(int port)
        {
            HttpServer? server = null;
            _httpservers.TryRemove(port, out server);
            if (server != null) server.Stop();
        }
        public HttpServer GetHttpServer(int port)
        {
            HttpServer? server = null;
            _httpservers.TryGetValue(port, out server);
            return server;
        }
        public void HttpServerStop()
        {

            foreach (var li in _fileSystemWatchers)
            {
                li.Changed -= new FileSystemEventHandler(OnProcess);
                li.Created -= new FileSystemEventHandler(OnProcess);
                li.Renamed -= new RenamedEventHandler(OnRenamed);
                li.Deleted -= new FileSystemEventHandler(OnProcess);
            }
            _fileSystemWatchers.Clear();

        }
        public void LoadApps(params AppInfo[] appInfos)
        {
            LoadApps(appInfos.ToList());
        }
        public void DealTimeTask()
        {
            foreach (var taskapp in _timeTaskApps)
            {
                if (taskapp.Isopen == null) continue;
                if (taskapp.Iscycle == null) continue;
                if (!taskapp.Isopen.Value) continue;
                if (taskapp.Time == null) continue;
                if (taskapp.Time.Value == 0) continue;
                if (!taskapp.CanExcute) continue;
                Thread th = new Thread(() =>
                {
                    taskapp.CanExcute = false;
                    var threadid = Thread.CurrentThread.ManagedThreadId.ToString();
                    CacheFactory.Cache.WriteCache(taskapp, "timetask" + threadid);
                    try
                    {
                        if (taskapp.ExcuteTime == null) taskapp.ExcuteTime = DateTime.Now.AddSeconds(taskapp.Time.Value);
                        if (DateTime.Now < taskapp.ExcuteTime.Value) return;
                        taskapp.BeforExcute(taskapp);
                        if (taskapp.ExcuteTime.Value.AddSeconds(taskapp.Time.Value) > DateTime.Now)
                        {
                            taskapp.ExcuteTime = taskapp.ExcuteTime.Value.AddSeconds(taskapp.Time.Value);
                        }
                        else
                        {
                            var allsecents = (DateTime.Now - taskapp.ExcuteTime.Value).TotalSeconds;
                            int bs = (allsecents / taskapp.Time.Value).ToInt();
                            taskapp.ExcuteTime = taskapp.ExcuteTime.Value.AddSeconds(taskapp.Time.Value * bs);
                        }
                        if (!taskapp.Iscycle.Value)
                        {
                            if (taskapp.Times == null) return;
                            if (taskapp.Times.Value == 0)
                            {
                                taskapp.Isopen = false;
                                return;
                            }
                            if (taskapp.Times.Value > 0) taskapp.Times = taskapp.Times.Value - 1;
                            if (taskapp.Times.Value == 0) taskapp.Isopen = false;
                        }
                        taskapp.methodInfo.Invoke(taskapp.instance, null);
                        taskapp.AfterExcute(taskapp, null);
                    }
                    catch (Exception e)
                    {
                        var exception = e.InnerException ?? e;
                        _logger.Error(exception.Message);
                        taskapp.AfterExcute(taskapp, exception);

                    }
                    finally
                    {
                        taskapp.CanExcute = true;
                        CacheFactory.Cache.RemoveCache("timetask" + threadid);
                    }
                });
                th.Start();
            }
        }
        public void DealInitTask()
        {
            foreach (var taskapp in _initTaskApps)
            {
                Thread th = new Thread(() =>
                {
                    try
                    {
                        taskapp.methodInfo.Invoke(taskapp.instance, null);
                    }
                    catch (Exception e)
                    {
                        var exception = e.InnerException ?? e;
                        _logger.Error(exception.Message);
                    }
                });
                th.Start();
            }
        }
        public void DealCloseTask()
        {
            foreach (var taskapp in _closeTaskApps)
            {
                Thread th = new Thread(() =>
                {
                    try
                    {
                        taskapp.methodInfo.Invoke(taskapp.instance, null);
                    }
                    catch (Exception e)
                    {
                        var exception = e.InnerException ?? e;
                        _logger.Error(exception.Message);
                    }
                });
                th.Start();
            }
        }
        //获取定时任务列表
        public List<TimeTaskApp> GetTimeTaskApps()
        {
            return _timeTaskApps;
        }
        //获取初始化任务列表
        public List<InitTaskApp> GetInitTaskApps()
        {
            return _initTaskApps;
        }
        public ConcurrentDictionary<string, Routeinfo> GetRoutedic()
        {
            return _routedic;
        }
        public void Start()
        {
            foreach (var server in _httpservers.Values)
            {
                server.Start();
            }
            foreach (var key in _apps.Keys)
            {
                try
                {
                    var creater = _apps[key] as ISMAppCreater;
                    if (creater != null) creater.CreateApp(this);
                }
                catch (Exception e)
                {
                    var exception = e.InnerException ?? e;
                    _logger.Error(exception.Message);
                }
            }

        }
        public void Stop()
        {
            foreach (var server in _httpservers.Values)
            {
                server.Stop();
            }
            foreach (var key in _apps.Keys)
            {
                try
                {
                    var creater = _apps[key] as ISMAppCreater;
                    if (creater != null) creater.DestoryApp(this);
                }
                catch (Exception e)
                {
                    var exception = e.InnerException ?? e;
                    _logger.Error(exception.Message);
                }
            }
        }
        public bool AddRoute(string route, Func<HttpContext, ActionResult> Action, HttpMethod httpMethod)
        {
            string r = httpMethod.ToString() + ":" + route.ToLower();
            Routeinfo approuteinfo = new Routeinfo
            {
                Isstatic = false,
                Isremote = false,
                Action = Action
            };
            return _routedic.TryAdd(r, approuteinfo);
        }
        /// <summary>
        /// 增加路径
        /// </summary>
        /// <param name="route"></param>
        /// <param name="routeinfo"></param>
        /// <param name="httpMethod"></param>
        /// <returns></returns>
        public bool AddRoute(string route, Routeinfo routeinfo, HttpMethod httpMethod)
        {
            string r = httpMethod.ToString() + ":" + route.ToLower();
            return _routedic.TryAdd(r, routeinfo);
        }
        public bool RemoveRoute(string route, HttpMethod httpMethod)
        {
            Routeinfo routeinfo = null;
            string r = httpMethod.ToString() + ":" + route.ToLower();
            return _routedic.TryRemove(r, out routeinfo);
        }
        public Routeinfo GetRoute(string route, HttpMethod httpMethod)
        {
            Routeinfo routeinfo = null;
            string r = httpMethod.ToString() + ":" + route.ToLower();
            _routedic.TryGetValue(r, out routeinfo);
            return routeinfo;
        }
        public List<AppInfo> GetAppInfos()
        {
            return _appInfoList;
        }
        #endregion
    }
}