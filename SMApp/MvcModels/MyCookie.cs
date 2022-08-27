using System;

namespace SMApp
{
    public class MyCookie
    {
        public MyCookie(string name, string value)
        {
            Name = name;
            Value = value;
        }
        #region Public Properties
        public string Domain
        {
            get; set;
        }

        public DateTime Expires
        {
            get; set;
        }

        public bool HttpOnly
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }


        public string Path { get; set; } = "/";

        public bool Secure
        {
            get; set;
        }

        /// </exception>
        public string Value
        {
            get; set;
        }
        #endregion
    }
}
