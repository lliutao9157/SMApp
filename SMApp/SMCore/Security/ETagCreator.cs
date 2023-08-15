using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SMApp
{
    /// <summary>
    /// Help class to calculate ETags.
    /// </summary>
    public class ETagCreator
    {
        /// <summary>
        /// Calculates an ETag from an object.
        /// </summary>
        /// <param name="obj">The object to calculate an ETag for.</param>
        /// <returns>The calculated ETag.</returns>
        public static string Create(object obj)
        {
            var objJson = JsonConvert.SerializeObject(obj);
            var objJsonBytes = Encoding.UTF8.GetBytes(objJson);

            var serviceProvider = new SHA1CryptoServiceProvider();
            string eTag = string.Empty;
            using (var memoryStream = new MemoryStream())
            {
                serviceProvider.ComputeHash(objJsonBytes);
                eTag = Convert.ToBase64String(serviceProvider.Hash);
            }
            eTag = string.Concat("\"", eTag, "\"");
            return eTag;
        }
    }
}

