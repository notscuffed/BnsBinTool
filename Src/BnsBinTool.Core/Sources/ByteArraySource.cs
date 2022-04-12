using System;
using System.IO;

namespace BnsBinTool.Core.Sources
{
    public class ByteArraySource : ISource
    {
        private readonly byte[] _array;
        private readonly int _offset;
        private readonly int _size;

        public ByteArraySource(byte[] array, int offset = 0, int size = -1)
        {
            _array = array;
            _offset = offset;
            _size = size;
        }

        public BinaryReader CreateReader()
        {
            return new BinaryReader(CreateStream());
        }

        public Stream CreateStream()
        {
            return new MemoryStream(_array, _offset, _size == -1 ? _array.Length : _size);
        }

        public ISource OffsetedSource(long offset, long size)
        {
            if (_offset + offset > int.MaxValue)
                throw new OverflowException("Offset doesn't fit inside 32-bit integer");
            
            if (size > int.MaxValue)
                throw new OverflowException("Size doesn't fit inside 32-bit integer");
            
            return new ByteArraySource(_array, (int) (_offset + offset), (int) size);
        }
    }
}