using System;
using System.IO;
using System.Text;
using BnsBinTool.Core.Models;
using BnsBinTool.Core.Sources;

namespace BnsBinTool.Core.Serialization
{
    public interface INameTableReader
    {
        NameTable ReadFrom(BinaryReader reader);
    }

    public class NameTableReader : INameTableReader
    {
        private static readonly Encoding KoreanEncoding
            = CodePagesEncodingProvider.Instance.GetEncoding(949);

        public ISource LazyLoadSource { get; init; }

        public NameTableReader(bool is64Bit)
        {
            _is64Bit = is64Bit;
        }

        private readonly bool _is64Bit;

        public NameTable ReadFrom(BinaryReader reader)
        {
            if (LazyLoadSource != null)
                return LazyReadFrom(reader, _is64Bit);

            var table = new NameTable();
            table.RootEntry.Begin = reader.ReadUInt32();
            table.RootEntry.End = reader.ReadUInt32();

            var entryCount = reader.ReadInt32();

            if (_is64Bit)
            {
                for (var i = 0; i < entryCount; i++)
                {
                    table.Entries.Add(ReadEntry64(reader));
                }
            }
            else
            {
                for (var i = 0; i < entryCount; i++)
                {
                    table.Entries.Add(ReadEntry(reader));
                }
            }

            var stringTableSize = reader.ReadInt32(); // Total size of string table
            var stringTable = reader.ReadBytes(stringTableSize);

            var memoryReader = new BinaryReader(new MemoryStream(stringTable), Encoding.ASCII);

            Span<byte> buffer = stackalloc byte[256];
            
            foreach (var entry in table.Entries)
            {
                memoryReader.BaseStream.Seek(entry.StringOffset, SeekOrigin.Begin);
                entry.String = ReadAliasString(memoryReader, buffer);
            }

            return table;
        }

        private static string ReadAliasString(BinaryReader reader, Span<byte> buffer)
        {
            var position = reader.BaseStream.Position;
            var size = 0;

            while (true)
            {
                if (reader.ReadByte() == 0)
                    break;

                size++;
            }

            buffer = buffer[..size];
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
            reader.Read(buffer);
            reader.ReadByte();

            return KoreanEncoding.GetString(buffer);
        }

        private static NameTableEntry ReadEntry(BinaryReader reader)
        {
            return new NameTableEntry
            {
                StringOffset = reader.ReadInt32(),
                Begin = reader.ReadUInt32(),
                End = reader.ReadUInt32()
            };
        }

        private static NameTableEntry ReadEntry64(BinaryReader reader)
        {
            return new NameTableEntry
            {
                StringOffset = reader.ReadInt64(),
                Begin = reader.ReadUInt32(),
                End = reader.ReadUInt32()
            };
        }

        private NameTable LazyReadFrom(BinaryReader reader, bool is64Bit)
        {
            var position = reader.BaseStream.Position;

            var globalStringTable = new LazyNameTable(new NameTableReader(is64Bit));
            globalStringTable.RootEntry.Begin = reader.ReadUInt32();
            globalStringTable.RootEntry.End = reader.ReadUInt32();

            var entryCount = reader.ReadInt32();

            if (is64Bit)
                reader.BaseStream.Seek(entryCount * 16, SeekOrigin.Current);
            else
                reader.BaseStream.Seek(entryCount * 12, SeekOrigin.Current);

            var stringTableSize = reader.ReadInt32();

            reader.BaseStream.Seek(stringTableSize, SeekOrigin.Current);

            globalStringTable.Source = LazyLoadSource.OffsetedSource(position, reader.BaseStream.Position - position);

            return globalStringTable;
        }
    }
}