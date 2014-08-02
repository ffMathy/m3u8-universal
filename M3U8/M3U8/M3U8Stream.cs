using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace M3U8
{
    public class M3U8Stream : Stream
    {
        public override void Flush()
        {
            throw new InvalidOperationException("This stream can't be written to.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("This stream can't have its length set because it is based on an infinite-length stream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("This stream can't be written to.");
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return long.MaxValue; }
        }

        public override long Position { get; set; }
    }
}
