using System.IO;
using BnsBinTool.Core.Helpers;

namespace BnsBinTool.Core.Sources
{
    public class MemorySource : ISource
    {
        private readonly byte[] _bytes;
        private readonly long _offset;
        private readonly long _size;

        public MemorySource(byte[] bytes, long offset = 0, long size = -1)
        {
            _bytes = bytes;
            _offset = offset;
            _size = size;
        }

        public BinaryReader CreateReader()
        {
            return new BinaryReader(CreateStream());
        }

        public Stream CreateStream()
        {
            var memoryStream = new MemoryStream(_bytes);

            return new SubStream(memoryStream, _offset, _size == -1 ? memoryStream.Length : _size);
        }

        public ISource OffsetedSource(long offset, long size)
        {
            return new MemorySource(_bytes, _offset + offset, size);
        }
    }
}