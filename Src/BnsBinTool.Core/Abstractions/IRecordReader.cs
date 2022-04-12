using System.IO;
using BnsBinTool.Core.Serialization;

namespace BnsBinTool.Core.Abstractions
{
    public interface IRecordReader
    {
        bool Initialize(BinaryReader reader, bool is64Bit);
        bool Read(BinaryReader reader, ref RecordMemory recordMemory);
    }
}