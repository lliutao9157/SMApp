using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMApp
{
    internal class RequestStream : Stream
    {
        #region  Private Fields
        private Stream _innerStream;
        private MemoryStream _initStream;
        private bool _isReadinitialBuffer;
        private long _contentLength;
        private long _total;

        #endregion

        #region Internal Constructors
        internal RequestStream(HttpContext context, byte[] initialBuffer,long contentLength)
        {
            _innerStream= context.NetWorkStream;
            _isReadinitialBuffer=false;
            _initStream=new MemoryStream(initialBuffer);
            _contentLength=contentLength;
            _total = 0;
        }
        #endregion

        #region Public Properties
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        #endregion

        #region Public Methods
        public override void Flush()
        {
             
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int neard = 0;
            if (_total == _contentLength)
                return neard;
            if (!_isReadinitialBuffer)
            {
                neard = _initStream.Read(buffer, offset, count);
                if (_initStream.Position == _initStream.Length)
                {
                    _isReadinitialBuffer = true;
                    _initStream.Close();
                    _initStream.Flush();
                }
                _total += neard;
                return neard;
            }
            neard = _innerStream.Read(buffer, offset, count);
            _total += neard;
            return neard;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
