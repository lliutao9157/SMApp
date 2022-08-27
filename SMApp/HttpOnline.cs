using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace SMApp
{
    public class HttpOnline
    {
        private static HttpServer server;
        public static void CreateHttpServer(int port, string certpath = null, string certpassword = null)
        {
            server = new HttpServer(port.ToInt());
            server.OnGet += Dealhttpmessage;
            server.OnPost += Dealhttpmessage;
            server.OnDelete += Dealhttpmessage;
            server.OnPut += Dealhttpmessage;
            server.OnOptions += Dealhttpmessage;
            server.OnTrace += Dealhttpmessage;
            if (!string.IsNullOrWhiteSpace(certpath)) server.SslConfiguration.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(certpath, certpassword);
        }
        public static void Stop()
        {
            if (server != null) server.Stop();
        }
        private  static void Dealhttpmessage(object sender, HttpRequestEventArgs e)
        {
            try
            {
                var request = e.Request;
                var response = e.Response;
                var datatype = request.ContentType;
                string url = request.Url.LocalPath;
                string[] routedates = url.Split(new char[] { '/' });
                string endstr = routedates[routedates.Length - 1];
                string account = routedates.Length > 1 ? routedates[1] : "";
                string app = routedates.Length > 2 ? routedates[2] : "";
                string controller = routedates.Length > 3 ? routedates[3] : "";
                string action = routedates.Length > 4 ? routedates[4] : "";
                var r = Regex.IsMatch(endstr, @"\.\w+$");
                if (!(TcpServer.ClientList != null && TcpServer.ClientList.Count > 0&&TcpServer.ClientList.Find(d=>d.ClientId==account)!=null))
                {
                     account = "";
                     app = routedates.Length > 1 ? routedates[1] : "";
                     controller = routedates.Length > 2 ? routedates[2] : "";
                     action = routedates.Length > 3 ? routedates[3] : "";
                }
                HttpRequest httpRequest = new HttpRequest
                {
                    datatype = datatype,
                    method = request.HttpMethod,
                    url = url,
                    controller = controller,
                    action = action,
                    app = app,
                    Authority = request.Url.Authority,
                    httphead = request.Url.Scheme,
                    Account = account
                };
                Hashtable headers = new Hashtable();
                foreach (var key in request.Headers.AllKeys)
                {
                    headers.Add(key, request.Headers[key]);
                }
                httpRequest.Headers = headers;
                if (request.Cookies.Count > 0)
                {
                    MyCookies cookies = new MyCookies();
                    foreach (var cookie in request.Cookies.ToList())
                    {
                        cookies.Add(cookie.ToJson().ToObject<MyCookie>());
                    }
                    httpRequest.Cookies = cookies;
                }
                var querydata = request.QueryString;
                if (querydata.Count > 0)
                {
                    Hashtable ha = new Hashtable();
                    foreach (var key in querydata.AllKeys)
                    {
                        ha.Add(key, querydata[key]);
                    }
                    httpRequest.querydata = ha;
                }
                if (datatype != null && datatype.Contains("multipart/form-data"))
                {

                    MultipartFormDataParser formDataParser = MultipartFormDataParser.Parse(request.InputStream);
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
                    if (appFiles.Count > 0) httpRequest.appFiles = appFiles;
                    if (ha.Count > 0) httpRequest.appForm = ha;
                }
                else if (datatype == "application/json")
                {
                    var jsondata = new byte[request.ContentLength64];
                    request.InputStream.Read(jsondata, 0, jsondata.Length);
                    var jsonstr = Encoding.UTF8.GetString(jsondata);
                    httpRequest.jsondata = jsonstr;
                }
                else if (datatype == "application/x-www-form-urlencoded")
                {

                    var formdata = new byte[request.ContentLength64];
                    request.InputStream.Read(formdata, 0, formdata.Length);
                    var formstr = Encoding.UTF8.GetString(formdata);
                    httpRequest.formdata = formstr;
                }
                else if (request.ContentLength64 > 0)
                {
                    var data = new byte[request.ContentLength64];
                    request.InputStream.Read(data, 0, data.Length);
                    httpRequest.requestdata = data;

                }
                if (!string.IsNullOrWhiteSpace(account))
                {
                    if (r)
                    {
                        var task= GetRemoteFile(httpRequest, response);
                        task.Wait();
                    }
                    else
                    {
                        var task= GetRemoteApi(httpRequest,response);
                        task.Wait();
                    }
                    return;
                }
                if (r)
                {
                    string filedicpath = $"{AppDomain.CurrentDomain.BaseDirectory}" + url.Substring(1);
                    var result = Getfile(filedicpath, response);
                    if (result) return;
                    string errorpage = "/404.html";
                    filedicpath = $"{AppDomain.CurrentDomain.BaseDirectory}" + errorpage.Substring(1);
                    Getfile(filedicpath, response);
                    return;
                }
                if (!Apps.Contains(app) && !Apps.Contains("")) throw new Exception("未找到该应用");
                if (!Apps.Contains(app) && Apps.Contains(""))
                {
                    app = "";
                    controller = routedates.Length > 1 ? routedates[1] : "";
                    action = routedates.Length > 2 ? routedates[2] : "";
                }
                
            
                Getapi(httpRequest, response);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null) DealError(e, ex.InnerException);
                DealError(e, ex);
            }
        }
        private static bool Getfile(string path, WebSocketSharp.Net.HttpListenerResponse res)
        {
            Regex reg = new Regex(@"^.+\.(?<type>[\w]+)$");
            Match result = reg.Match(path);
            if (!result.Success) return false;
            if (!File.Exists(path)) return false;
            byte[] contents = File.ReadAllBytes(path);
            res.ContentLength64 = contents.LongLength;
            res.ContentType = FileContentType.GetMimeType(result.Result("${type}"));
            res.Close(contents, true);
            return true;
        }
        private static void Getapi(HttpRequest request, WebSocketSharp.Net.HttpListenerResponse res)
        {
            var actionresult = DealRequest(request);
            if (actionresult.Cookies != null && actionresult.Cookies.Count > 0)
            {
                foreach (var cookie in actionresult.Cookies)
                {
                    res.Cookies.Add(cookie.ToJson().ToObject<Cookie>());
                }
            }
            switch (actionresult.Name)
            {
                case "JsonResult":
                    var jsonresult = (JsonResult)actionresult;
                    byte[] jsoncontents = Encoding.UTF8.GetBytes(jsonresult.Data.ToJson());
                    res.ContentLength64 = jsoncontents.LongLength;
                    res.ContentEncoding = Encoding.UTF8;
                    res.ContentType = "application/json";
                    res.Close(jsoncontents, true);
                    break;
                case "ContentResult":
                    var contentresult = (ContentResult)actionresult;
                    byte[] contentcontents = Encoding.UTF8.GetBytes(contentresult.Data);
                    res.ContentLength64 = contentcontents.LongLength;
                    res.ContentEncoding = Encoding.UTF8;
                    res.ContentType = "text/html";
                    res.Close(contentcontents, true);
                    break;
                case "RedirectResult":
                    var redirectresult = (RedirectResult)actionresult;
                    var url = redirectresult.Url;
                    Uri uri;
                    if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    {
                        url = $"{request.httphead}://{request.Authority}{url}";
                        Uri.TryCreate(url, UriKind.Absolute, out uri);
                    }
                    if (uri == null) throw new Exception("url错误");
                    res.Redirect(uri.AbsoluteUri);
                    res.Close();
                    break;
                case "FileResult":
                    var fileresult = (FileResult)actionresult;
                    res.ContentType = fileresult.ContentType;
                    res.ContentLength64 = fileresult.Data.Length;
                    res.Close(fileresult.Data, true);
                    break;

            }

        }
        private async static Task GetRemoteFile(HttpRequest request, WebSocketSharp.Net.HttpListenerResponse res)
        {
            var result = await TcpServer.GetFileresult(request, request.Account);
            if (result.Length == 0)
            {
                string errorpage = "/404.html";
                string filedicpath = $"{AppDomain.CurrentDomain.BaseDirectory}" + errorpage.Substring(1);
                Getfile(filedicpath, res);
                return;
            }
            Regex reg = new Regex(@"^.+\.(?<type>[\w]+)$");
            Match type = reg.Match(request.url);
            res.ContentLength64 = result.LongLength;
            res.ContentType = FileContentType.GetMimeType(type.Result("${type}"));
            res.Close(result, true);
        }
        private async static Task GetRemoteApi(HttpRequest request, WebSocketSharp.Net.HttpListenerResponse res)
        {
            var result = await TcpServer.GetApiresult(request, request.Account);
            OtherResult actionresult = result.ToObject<OtherResult>();
            if (actionresult.Cookies != null && actionresult.Cookies.Count > 0)
            {
                foreach (var cookie in actionresult.Cookies)
                {
                    res.Cookies.Add(cookie.ToJson().ToObject<Cookie>());
                }
            }
            switch (actionresult.Name)
            {
                case "JsonResult":
                    var jsonresult = result.ToObject<JsonResult>();
                    byte[] jsoncontents = Encoding.UTF8.GetBytes(jsonresult.Data.ToJson());
                    res.ContentLength64 = jsoncontents.LongLength;
                    res.ContentEncoding = Encoding.UTF8;
                    res.ContentType = "application/json";
                    res.Close(jsoncontents, true);
                    break;
                case "ContentResult":
                    var contentresult = result.ToObject<ContentResult>();
                    byte[] contentcontents = Encoding.UTF8.GetBytes(contentresult.Data);
                    res.ContentLength64 = contentcontents.LongLength;
                    res.ContentEncoding = Encoding.UTF8;
                    res.ContentType = "text/html";
                    res.Close(contentcontents, true);
                    break;
                case "RedirectResult":
                    var redirectresult = result.ToObject<RedirectResult>();
                    var url = redirectresult.Url;
                    Uri uri;
                    if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    {
                        url = $"{request.httphead}://{request.Authority}{url}";
                        Uri.TryCreate(url, UriKind.Absolute, out uri);
                    }
                    if (uri == null) throw new Exception("url错误");
                    res.Redirect(uri.AbsoluteUri);
                    res.Close();
                    break;
                case "FileResult":
                    FileResult fileresult = result.ToObject<FileResult>();
                    res.ContentType = fileresult.ContentType;
                    res.ContentLength64 = fileresult.Data.Length;
                    res.Close(fileresult.Data, true);
                    break;

            }
 

        }
        private static void DealError(HttpRequestEventArgs e, Exception ex)
        {
            if (OnHttpError != null)
            {
                var error = OnHttpError(ex);
                var res = e.Response;
                byte[] data = Encoding.UTF8.GetBytes(error.msg);
                res.ContentLength64 = data.LongLength;
                res.ContentEncoding = Encoding.UTF8;
                res.ContentType = error.Contenttype;
                res.Close(data, true);
            }

        }
        public static Hashtable Apps { get; set; } = new Hashtable();
        public static List<AppInfo> AppInfoList { get; set; }
        private static List<TimeTaskApp> TimeTaskApps { get; set; } = new List<TimeTaskApp>();
        private static List<InitTaskApp> InitTaskApps { get; set; } = new List<InitTaskApp>();
        private static List<CloseTaskApp> CloseTaskApps { get; set; } = new List<CloseTaskApp>();
        public static void Start()
        {
            server.Start();
            LoadApps();
        }
        private static void LoadApps()
        {
            if (AppInfoList == null || AppInfoList.Count == 0) return;
            Apps.Clear();
            TimeTaskApps.Clear();
            InitTaskApps.Clear();
            CloseTaskApps.Clear();
            foreach (var appinfo in AppInfoList)
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + appinfo.dllfie;
                Assembly assembly = Assembly.LoadFile(path);
                //Assembly assembly = Assembly.Load(appinfo.dllfie);
                Type instancetype = assembly.GetType(appinfo.instance);
                var instance = Activator.CreateInstance(instancetype);
                Apps.Add(appinfo.name, instance);
            }
            foreach (var app in Apps.Values)
            {
                List<PropertyInfo> propertyInfos = app.GetType().GetProperties().ToList();
                foreach (var propertyInfo in propertyInfos)
                {
                    object severinstance = propertyInfo.GetValue(app);
                    List<MethodInfo> methodInfos = propertyInfo.PropertyType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "TimetaskAttribute") != null);
                    foreach (var method in methodInfos)
                    {
                        var taskattribute = method.GetCustomAttribute(typeof(TimetaskAttribute));
                        if (taskattribute == null) continue;
                        TimeTaskApp timetaskapp = new TimeTaskApp
                        {
                            instance = severinstance,
                            methodInfo = method,
                            attribute = (TimetaskAttribute)taskattribute,
                            CanExcute = true
                        };
                        TimeTaskApps.Add(timetaskapp);
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
                        };
                        InitTaskApps.Add(timetaskapp);
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
                        };
                        CloseTaskApps.Add(timetaskapp);
                    }
                }
                List<FieldInfo> fieldInfos = app.GetType().GetFields().ToList();
                foreach (var fieldInfo in fieldInfos)
                {
                    object severinstance = fieldInfo.GetValue(app);
                    List<MethodInfo> methodInfos = fieldInfo.FieldType.GetMethods().ToList().FindAll(m => m.CustomAttributes.ToList().Find(d => d.AttributeType.Name == "TimetaskAttribute") != null);
                    foreach (var method in methodInfos)
                    {
                        var taskattribute = method.GetCustomAttribute(typeof(TimetaskAttribute));
                        if (taskattribute == null) continue;
                        TimeTaskApp timetaskapp = new TimeTaskApp
                        {
                            instance = severinstance,
                            methodInfo = method,
                            attribute = (TimetaskAttribute)taskattribute
                        };
                        TimeTaskApps.Add(timetaskapp);
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
                        };
                        InitTaskApps.Add(timetaskapp);
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
                        };
                        CloseTaskApps.Add(timetaskapp);
                    }
                }
            }
        }
        public static ActionResult DealRequest(HttpRequest request)
        {
            var threadid = Thread.CurrentThread.ManagedThreadId.ToString();
            CacheFactory.Cache.WriteCache(request, "request" + threadid);
            var hYSServer = Apps[request.app];
            if (hYSServer == null) throw new Exception("未找到该应用");
            MemberInfo memberInfo = hYSServer.GetType().GetMembers().ToList().Find(d => d.Name.ToLower() == request.controller.ToLower());
            if (memberInfo == null) throw new Exception("未找到接口");
            Type fieldtype = null;
            object severinstance = null;
            if (memberInfo.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = (System.Reflection.PropertyInfo)memberInfo;
                fieldtype = propertyInfo.PropertyType;
                severinstance = propertyInfo.GetValue(hYSServer);
            }
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                FieldInfo field = (System.Reflection.FieldInfo)memberInfo;
                fieldtype = field.FieldType;
                severinstance = field.GetValue(hYSServer);
            }
            MethodInfo methodInfo = null;
            List<MethodInfo> methodInfos = fieldtype.GetMethods().ToList();
            WebApiAttribute eapi = null;
            object userinfo = null;
            int totalindex = 0;
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
            if (request.querydata != null)
            {
                foreach (var key in request.querydata.Keys)
                {
                    if (!QuryData.ContainsKey(key)) QuryData.Add(key, request.querydata[key]);
                }
            }
            foreach (var method in methodInfos)
            {
                var webapi = method.GetCustomAttribute(typeof(WebApiAttribute));
                if (webapi == null) continue;
                var api = (WebApiAttribute)webapi;
                string action = api.Action;
                if (string.IsNullOrWhiteSpace(action)) action = method.Name;
                if (request.action.ToLower() != action.ToLower()) continue;
                if (request.method == "GET" && !api.GET) continue;
                if (request.method == "POST" && !api.POST) continue;
                methodInfo = method;
                eapi = api;
            }
            if (methodInfo == null) throw new Exception("未找到接口");
            List<object> paramlist = new List<object>();
            ParameterInfo[] plist = methodInfo.GetParameters();
            for (int i = 0; i < plist.Length; i++)
            {
                if (plist[i].ParameterType.FullName == null)
                {
                    object o = null;
                    if (eapi.WriteUerParam == plist[i].Name) o = JsonConvert.DeserializeObject(userinfo.ToJson());
                    if (eapi.RetrunTotalParam == plist[i].Name)
                    {
                        o = 0;
                        totalindex = i;
                    }
                    if (QuryData.ContainsKey(plist[i].Name)) o = JsonConvert.DeserializeObject((QuryData[plist[i].Name]).ToJson());
                    paramlist.Add(o);
                }
                else
                {
                    object o = null;
                    if (eapi.WriteUerParam == plist[i].Name) o = JsonConvert.DeserializeObject(userinfo.ToJson(), plist[i].ParameterType);
                    if (eapi.RetrunTotalParam == plist[i].Name)
                    {
                        o = 0;
                        totalindex = i;
                    }
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
            var result = methodInfo.Invoke(severinstance, parameters) as ActionResult;
            CacheFactory.Cache.RemoveCache("request" + threadid);
            return result;
        }
        public static void DealTimeTask()
        {
            foreach (var taskapp in TimeTaskApps)
            {
                if (!taskapp.CanExcute) continue;
                Thread th = new Thread(() =>
                {
                    taskapp.CanExcute = false;
                    try
                    {
                        var threadid = Thread.CurrentThread.ManagedThreadId.ToString();
                        CacheFactory.Cache.WriteCache("定时任务", "TASK" + threadid);
                        Thread.Sleep(taskapp.attribute.Time * 1000);
                        taskapp.methodInfo.Invoke(taskapp.instance, null);
                    }
                    catch
                    {

                    }
                    taskapp.CanExcute = true;
                });
                th.Start();
            }
        }
        public static void DealInitTask()
        {
            foreach (var taskapp in InitTaskApps)
            {
                Thread th = new Thread(() =>
                {
                    try
                    {
                        taskapp.methodInfo.Invoke(taskapp.instance, null);
                    }
                    catch
                    {

                    }
                });
                th.Start();
            }
        }
        public static void DealCloseTask()
        {
            foreach (var taskapp in CloseTaskApps)
            {
                Thread th = new Thread(() =>
                {
                    try
                    {
                        taskapp.methodInfo.Invoke(taskapp.instance, null);
                    }
                    catch
                    {

                    }
                });
                th.Start();
            }
        }
        public static Func<Exception, HttpError> OnHttpError { get; set; }

    }
}
