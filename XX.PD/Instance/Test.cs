using SMApp;
using System;
using System.Collections.Generic;
using System.IO;
using XX.PD.Interface;

namespace XX.PD.Instance
{
    public class Test : Controller, ITest
    {
        public JsonResult bbb(string name, int age)
        {
            var tt = Request;
            Cookies.Add(new MyCookie("uuuu", "65666"));
            Cookies.Add(new MyCookie("oooo", "iiii"));
            Cookies.Add(new MyCookie("ddd", "7777"));
            return Json(new { name = name, age = age, cookie = Cookies.GetCookie("uuuu") });
        }

        public FileResult ccc()
        {
            byte[] code = new byte[2];
            return File(code, FileContentType.GetMimeType("png"));
        }

        public ContentResult ddd(string name)
        {
            return Content(Request.Cookies.GetCookie(name).Value);
        }

        public JsonResult Getme()
        {

            return Json(new { data = "hellworld" });
        }

        public RedirectResult goout()
        {
            return Redirect("/xx/test/bbb");
        }

        public JsonResult uploaddoctorpic(List<AppFile> file)
        {
            foreach(var li in file)
            {
                string filedicpath = $"{AppDomain.CurrentDomain.BaseDirectory}" + li.FileName;
               
                FileStream fsRead = new FileStream(filedicpath, FileMode.Create);
                fsRead.Write(li.FileData, 0, li.FileData.Length);
                fsRead.Close();
            }

            var result = new JsonResult(new { data = "上传成功" });
            return result;
        }
    }
}
