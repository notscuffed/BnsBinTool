using System.IO;

namespace BnsBinTool.Core.Abstractions
{
    public interface ISerializableRecord : IRecord
    {
        unsafe ushort Serialize(byte* buffer, StreamWriter stringWriter);
    }
}