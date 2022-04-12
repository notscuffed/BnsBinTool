using System.IO;

namespace BnsBinTool.Core.Models
{
    public class TableHeader
    {
        public byte ElementCount { get; set; }
        public short Type { get; set; }
        public ushort MajorVersion { get; set; }
        public ushort MinorVersion { get; set; }
        public int Size { get; set; }
        public bool IsCompressed { get; set; }
        
        public void ReadHeaderFrom(BinaryReader reader, bool is64Bit = false)
        {
            ElementCount = reader.ReadByte();
            Type = reader.ReadInt16();
            MajorVersion = reader.ReadUInt16();
            MinorVersion = reader.ReadUInt16();
            Size = reader.ReadInt32();
            IsCompressed = reader.ReadBoolean();
        }

        public void WriteHeaderTo(BinaryWriter writer)
        {
            writer.Write(ElementCount);
            writer.Write(Type);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
        }
    }
}