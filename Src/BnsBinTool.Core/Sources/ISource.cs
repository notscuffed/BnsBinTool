using System.IO;

namespace BnsBinTool.Core.Sources
{
    public interface ISource
    {
        BinaryReader CreateReader();
        Stream CreateStream();
        ISource OffsetedSource(long offset, long size);
    }
}