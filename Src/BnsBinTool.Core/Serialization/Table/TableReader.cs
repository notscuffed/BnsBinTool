using System;
using System.IO;
using System.Runtime.InteropServices;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;
using BnsBinTool.Core.Sources;

namespace BnsBinTool.Core.Serialization
{
    public interface ITableReader
    {
        Table ReadFrom(BinaryReader reader);
    }

    public class TableReader : ITableReader
    {
        private readonly RecordCompressedReader _recordCompressedReader;
        private readonly RecordUncompressedReader _recordUncompressedReader;
        private readonly bool _is64Bit;

        public TableReader(
            RecordCompressedReader recordCompressedReader = null,
            RecordUncompressedReader recordUncompressedReader = null,
            bool is64bit = false)
        {
            _recordUncompressedReader = recordUncompressedReader ?? new RecordUncompressedReader();
            _recordCompressedReader = recordCompressedReader ?? new RecordCompressedReader();
            _is64Bit = is64bit;
        }

        public ISource LazyLoadSource { get; init; }

        public Table ReadFrom(BinaryReader reader)
        {
            if (LazyLoadSource != null)
                return LazyReadFrom(reader);

            var table = new Table();
            table.ReadHeaderFrom(reader, _is64Bit);

            if (table.IsCompressed)
                ReadCompressed(reader, table);
            else
                ReadUncompressed(reader, table);

            return table;
        }

        private LazyTable LazyReadFrom(BinaryReader reader)
        {
            var table = new LazyTable(new TableReader(is64bit: _is64Bit));

            var tableStart = reader.BaseStream.Position;
            table.ReadHeaderFrom(reader);

            table.Source = LazyLoadSource.OffsetedSource(tableStart, table.Size + 11);

            reader.BaseStream.Seek(table.Size - 1, SeekOrigin.Current);

            return table;
        }

        private unsafe void ReadCompressed(BinaryReader reader, Table table)
        {
            if (!_recordCompressedReader.Initialize(reader, _is64Bit))
                ThrowHelper.ThrowException("Failed to initialize compressed record reader");

            var rowMemory = new RecordMemory();

            while (_recordCompressedReader.Read(reader, ref rowMemory))
            {
                var row = new Record
                {
                    Data = new byte[rowMemory.DataSize],
                    StringLookup = new StringLookup {IsPerTable = true, Data = new byte[rowMemory.StringBufferSize]}
                };

                Marshal.Copy((IntPtr) rowMemory.DataBegin, row.Data, 0, rowMemory.DataSize);
                Marshal.Copy((IntPtr) rowMemory.StringBufferBegin, row.StringLookup.Data, 0, rowMemory.StringBufferSize);

                table.Records.Add(row);
            }
        }

        private unsafe void ReadUncompressed(BinaryReader reader, Table table)
        {
            if (!_recordUncompressedReader.Initialize(reader,
                _is64Bit && !table.IsCompressed && table.ElementCount == 1))
                ThrowHelper.ThrowException("Failed to initialize uncompressed record reader");

            var rowMemory = new RecordMemory();
            var stringLookup = new StringLookup {IsPerTable = true};

            while (_recordUncompressedReader.Read(reader, ref rowMemory))
            {
                if (rowMemory.DataSize == 6)
                    continue;

                var row = new Record
                {
                    Data = new byte[rowMemory.DataSize],
                    StringLookup = stringLookup
                };

                Marshal.Copy((IntPtr) rowMemory.DataBegin, row.Data, 0, rowMemory.DataSize);

                table.Records.Add(row);
            }

            table.RecordCountOffset = _recordUncompressedReader.GetRecordCountOffset();

            if (rowMemory.StringBufferBegin != null)
            {
                stringLookup.Data = new byte[rowMemory.StringBufferSize];
                Marshal.Copy((IntPtr) rowMemory.StringBufferBegin, stringLookup.Data, 0, rowMemory.StringBufferSize);
            }

            _recordUncompressedReader.GetPadding(out var padding);
            if (padding.Length > 0)
                table.Padding = padding.ToArray();
        }
    }
}