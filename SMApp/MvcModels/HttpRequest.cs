using System.Collections;
using System.Collections.Generic;

namespace SMApp
{
    public class HttpRequest
    {
        public string app { get; set; }
        public string formdata { get; set; }
        public Hashtable querydata { get; set; }
        public string jsondata { get; set; }
        public string datatype { get; set; }
        public string url { get; set; }
        public string method { get; set; }
        public string controller { get; set; }
        public string action { get; set; }
        public List<AppFile> appFiles { get; set; }
        public Hashtable appForm { get; set; }
        public byte[] requestdata { get; set; }
        public string Authority { get; set; }
        public string httphead { get; set; }
        public Hashtable Headers { get; set; }
        public MyCookies Cookies { get; set; }
        public string Account { get; set; }



    }
}
