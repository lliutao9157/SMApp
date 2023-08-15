using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMApp
{
    class GzipInstance : IZip
    {
        public string Ziptype { get; set; } = "gzip";
        public byte[] Compress(byte[] buffer)
        {
            if (buffer == null)
                return null;
            using (var ms = new MemoryStream())
            {
                using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    zip.Write(buffer, 0, buffer.Length);
                }
                return ms.ToArray();
            }
        }
        public byte[] Decompress(byte[] buffer)
        {
            if (buffer == null)
                return null;
            return Decompress(new MemoryStream(buffer));
        }
        public byte[] StreamToBytes(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }
        public  byte[] Decompress(Stream stream)
        {
            if (stream == null || stream.Length == 0)
                return null;
            using (var zip = new GZipStream(stream, CompressionMode.Decompress))
            {
                using (var reader = new StreamReader(zip))
                {
                    return Encoding.UTF8.GetBytes(reader.ReadToEnd());
                }
            }
        }

        public Stream Compress(Stream stream)
        {
            if (stream == null)
                return null;
            var ms = new MemoryStream();
            using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                byte[] buffer = StreamToBytes(stream);
                zip.Write(buffer, 0, buffer.Length);
            }
            return ms;
            //var zip = new GZipStream(stream, CompressionMode.Compress);
            //return zip;
        }

        Stream IZip.Decompress(Stream stream)
        {
            if (stream == null || stream.Length == 0)
                return null;
            using (var zip = new GZipStream(stream, CompressionMode.Decompress))
            {
                return zip;
            }
        }
    }
}
