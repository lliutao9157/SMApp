using System;

namespace SMApp
{
    public class WebApiAttribute : Attribute
    {
        public string Name { get; set; }
        public string Action { get; set; }
        public bool POST { get; set; }
        public bool GET { get; set; }
        //public bool IsWriteToken { get; set; }
        /// <summary>
        /// 返回总数的参数
        /// </summary>
        //public string RetrunTotalParam { get; set; }
        /// <summary>
        /// 是否路过验证
        /// </summary>
        //public bool IsIgnoreValidate { get; set; }
        /// <summary>
        /// 增加用户信息的参数
        /// </summary>
        //public string WriteUerParam { get; set; }
    }
}
