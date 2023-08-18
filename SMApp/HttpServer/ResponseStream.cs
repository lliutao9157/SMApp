namespace SMApp
{
    public class ResponseStream : Stream
    {
        #region  Private Fields
        private Stream _stream;
        private bool _sendChunked;
        private HttpContext _context;


        #endregion
        #region Internal Constructors
        internal ResponseStream(HttpContext context)
        {
            _stream = context.NetWorkStream;
            _context = context;
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
            try
            {
                _stream.Write(buffer, offset, count);
            }
            catch(Exception e)
            {
                _context.Connection.Logger.Error(e.Message);
                _context.Connection.Close();
            }
        }

        #region Internal Methods
        internal void Close(bool force)
        {
        }
        #endregion
    }
}
