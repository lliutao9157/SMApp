namespace SMApp
{
    public class ResponseStream : Stream
    {
        #region  Private Fields
        private Stream _stream;
        private bool _sendChunked;


        #endregion
        #region Internal Constructors
        internal ResponseStream(HttpContext context)
        {
            _stream = context.NetWorkStream;
        }
        #endregion
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            _stream.Write(buffer, offset, count);
        }

        #region Internal Methods
        internal void Close(bool force)
        {
 

            
        }
        #endregion
    }
}
