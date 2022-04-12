using System.IO;
using BnsBinTool.Core.Helpers;

namespace BnsBinTool.Core.Sources
{
    public class FileSource : ISource
    {
        private readonly string _filePath;
        private readonly long _offset;
        private readonly long _size;

        public FileSource(string filePath, long offset = 0, long size = -1)
        {
            _filePath = filePath;
            _offset = offset;
            _size = size;
        }

        public BinaryReader CreateReader()
        {
            return new BinaryReader(CreateStream());
        }

        public Stream CreateStream()
        {
            var fileStream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return new SubStream(fileStream, _offset, _size == -1 ? fileStream.Length : _size);
        }

        public ISource OffsetedSource(long offset, long size)
        {
            return new FileSource(_filePath, _offset + offset, size);
        }
    }
}