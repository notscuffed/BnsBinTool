using System.IO;

namespace BnsBinTool.Core.Helpers
{
    internal class SubStream : Stream
    {
        private Stream _baseStream;
        private readonly long _offset;
        private readonly long _length;
        private long _position;

        public SubStream(Stream baseStream, long offset, long length)
        {
            ArgGuard.ThrowIfNull(baseStream, nameof(baseStream));
            ArgGuard.ThrowIfFalse(baseStream.CanRead, nameof(baseStream));
            ArgGuard.ThrowIfFalse(baseStream.CanSeek, nameof(baseStream));
            ArgGuard.ThrowIfFalse(offset >= 0, nameof(offset));

            _baseStream = baseStream;
            _position = 0;
            _offset = offset;
            _length = length;

            baseStream.Seek(offset, SeekOrigin.Current);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckIfDisposed();
            var remaining = _length - _position;
            if (remaining <= 0) return 0;
            if (remaining < count) count = (int) remaining;
            var read = _baseStream.Read(buffer, offset, count);
            _position += read;
            return read;
        }

        private void CheckIfDisposed()
        {
            if (_baseStream == null)
                ThrowHelper.ThrowObjectDisposedException(GetType().Name);
        }

        public override long Length
        {
            get
            {
                CheckIfDisposed();
                return _length;
            }
        }

        public override bool CanRead
        {
            get
            {
                CheckIfDisposed();
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                CheckIfDisposed();
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                CheckIfDisposed();
                return false;
            }
        }

        public override long Position
        {
            get
            {
                CheckIfDisposed();
                return _position;
            }
            set => ThrowHelper.ThrowNotSupportedException("Use Seek instead");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return _position = _baseStream.Seek(_offset + offset, origin) - _offset;
                case SeekOrigin.Current:
                    return _position = _baseStream.Seek(offset, origin) - _offset;
                case SeekOrigin.End:
                    return _position = _baseStream.Seek(_baseStream.Length - (_offset + _length) + offset, origin) - _offset;
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(origin), origin);
                    return 0;
            }
        }

        public override void SetLength(long value)
        {
            ThrowHelper.ThrowNotSupportedException("Setting substream length is not supported");
        }

        public override void Flush()
        {
            CheckIfDisposed();
            _baseStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            if (_baseStream == null)
                return;

            try
            {
                _baseStream.Dispose();
            }
            catch
            {
                // ignore
            }

            _baseStream = null;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowHelper.ThrowNotImplementedException();
        }
    }
}