using System.Collections.Generic;

namespace SMApp
{
    public class MyCookies
    {
        public List<MyCookie> Cookies { get; set; } = new List<MyCookie>();
        public void Add(MyCookie cookie)
        {
            MyCookie myCookie = Cookies.Find(d => d.Name == cookie.Name);
            if (myCookie != null)
            {
                myCookie.Value = cookie.Value;
                return;
            }
            Cookies.Add(cookie);
        }
        public MyCookie GetCookie(string name)
        {
            MyCookie myCookie = Cookies.Find(d => d.Name == name);
            return myCookie;
        }
    }
}
