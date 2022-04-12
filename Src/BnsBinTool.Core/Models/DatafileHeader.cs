using System;
using System.IO;
using System.Text;

namespace BnsBinTool.Core.Models
{
    public class DatafileHeader
    {
        public string Magic { get; set; }
        public byte DatafileVersion { get; set; }
        public ushort[] ClientVersion { get; } = new ushort[4];
        public long TotalTableSize { get; set; }
        public long ReadTableCount { get; private set; }
        public long AliasMapSize { get; set; }
        public long AliasCount { get; set; }
        public long MaxBufferSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte[] Reserved { get; set; }

        public void ReadHeaderFrom(BinaryReader reader, bool is64Bit)
        {
            Magic = Encoding.ASCII.GetString(reader.ReadBytes(8));
            DatafileVersion = reader.ReadByte();

            for (var i = 0; i < 4; i++)
                ClientVersion[i] = reader.ReadUInt16();

            if (is64Bit)
            {
                TotalTableSize = reader.ReadInt64();
                ReadTableCount = reader.ReadInt64();
                AliasMapSize = reader.ReadInt64();
                AliasCount = reader.ReadInt64();
                MaxBufferSize = reader.ReadInt64();
            }
            else
            {
                TotalTableSize = reader.ReadInt32();
                ReadTableCount = reader.ReadInt32();
                AliasMapSize = reader.ReadInt32();
                AliasCount = reader.ReadInt32();
                MaxBufferSize = reader.ReadInt32();
            }

            CreatedAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(reader.ReadUInt32());
            Reserved = reader.ReadBytes(58);
        }

        public Action<long> WriteHeaderTo(BinaryWriter writer, long tableCount, long aliasCount, bool is64Bit)
        {
            writer.Write(Encoding.ASCII.GetBytes(Magic));
            writer.Write(DatafileVersion);

            for (var i = 0; i < 4; i++)
                writer.Write(ClientVersion[i]);

            Action<long> overwriteSize;

            if (is64Bit)
            {
                writer.Write((long) TotalTableSize);
                writer.Write((long) tableCount);
                var offset = writer.BaseStream.Position;
                overwriteSize = x =>
                {
                    var oldPosition = writer.BaseStream.Position;
                    writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                    writer.Write((long) x);
                    writer.BaseStream.Seek(oldPosition, SeekOrigin.Begin);
                };
                writer.Write((long) 0);
                writer.Write((long) aliasCount);
                writer.Write((long) MaxBufferSize);
            }
            else
            {
                writer.Write((int) TotalTableSize);
                writer.Write((int) tableCount);
                var offset = writer.BaseStream.Position;
                overwriteSize = x =>
                {
                    var oldPosition = writer.BaseStream.Position;
                    writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                    writer.Write((int) x);
                    writer.BaseStream.Seek(oldPosition, SeekOrigin.Begin);
                };
                writer.Write((int) 0);
                writer.Write((int) aliasCount);
                writer.Write((int) MaxBufferSize);
            }

            writer.Write((int) (CreatedAt - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            writer.Write(Reserved);
            return overwriteSize;
        }
    }
}