using System;
using System.IO;
using System.Runtime.InteropServices;
using BnsBinTool.Core.Abstractions;
using BnsBinTool.Core.Helpers;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace BnsBinTool.Core.Serialization
{
    public unsafe class RecordCompressedReader : IRecordReader
    {
        private const ushort BufferSize = ushort.MaxValue;
        private readonly IntPtr _decompressedBufferHandle = Marshal.AllocHGlobal(BufferSize);
        private readonly byte* _decompressedBuffer;
#if ENABLE_ISA
        private readonly IntPtr _compressedBufferHandle = Marshal.AllocHGlobal(BufferSize);
        private readonly byte* _compressedBuffer;
#endif

        private int _compressedBlockCount = -1;
        private int _currentBlock = -1;
        private int _currentRecord;
        private int _recordCount;
        private int _sizeDecompressed;
        private int _position;

        public RecordCompressedReader()
        {
            _decompressedBuffer = (byte*) _decompressedBufferHandle.ToPointer();
#if ENABLE_ISA
            _compressedBuffer = (byte*) _compressedBufferHandle.ToPointer();
#endif
        }

        ~RecordCompressedReader()
        {
            Marshal.FreeHGlobal(_decompressedBufferHandle);
#if ENABLE_ISA
            Marshal.FreeHGlobal(_compressedBufferHandle);
#endif
        }

        public bool Initialize(BinaryReader reader, bool is64Bit)
        {
            _compressedBlockCount = -1;
            _currentBlock = -1;
            _currentRecord = 0;
            _recordCount = 0;
            _sizeDecompressed = 0;
            _position = 0;
            _compressedBlockCount = reader.ReadInt32();

            if (reader.ReadUInt16() != 8)
                return false;

            return BeginReadBlock(reader);
        }

        public bool Read(BinaryReader reader, ref RecordMemory recordMemory)
        {
            if (_compressedBlockCount == -1)
                ThrowHelper.ThrowException("Uninitialized");

            if (_currentRecord == _recordCount)
            {
                if (!BeginReadBlock(reader))
                    return false;
            }

            recordMemory.DataBegin = _decompressedBuffer + _position;
            recordMemory.Type = *(short*) (recordMemory.DataBegin + 2);
            recordMemory.DataSize = *(ushort*) (recordMemory.DataBegin + 4);

            _position = _currentRecord < _recordCount - 1
                ? reader.ReadUInt16()
                : _sizeDecompressed;

            recordMemory.StringBufferBegin = recordMemory.DataBegin + recordMemory.DataSize;
            recordMemory.StringBufferSize = (ushort) (_decompressedBuffer + _position - recordMemory.StringBufferBegin);

            _currentRecord++;

            return true;
        }

        private bool BeginReadBlock(BinaryReader reader)
        {
            if (++_currentBlock >= _compressedBlockCount)
                return false;

            // Skip startRecord - endRecord & sizeCompressed
            reader.BaseStream.Seek(4 * sizeof(int), SeekOrigin.Current);

            var sizeCompressed = reader.ReadUInt16();
            var beforeDecompressingPosition = reader.BaseStream.Position;

#if ENABLE_ISA
            if (reader.Read(new Span<byte>(_compressedBuffer, sizeCompressed)) != sizeCompressed)
                ThrowHelper.ThrowException("Failed to read compressed buffer");
            if (!IsaLib.zlib_decompress(_compressedBuffer, sizeCompressed, _decompressedBuffer, BufferSize))
                ThrowHelper.ThrowException("Failed to decompress");
#else
            using (var inputStream = new InflaterInputStream(reader.BaseStream) {IsStreamOwner = false})
            {
                using var memoryStream = new UnmanagedMemoryStream(_decompressedBuffer, BufferSize, BufferSize, FileAccess.ReadWrite);
                inputStream.CopyTo(memoryStream);
            }
#endif

            reader.BaseStream.Seek(beforeDecompressingPosition + sizeCompressed, SeekOrigin.Begin);

            _sizeDecompressed = reader.ReadUInt16();

            // Read decompressed records and lookup tables
            _recordCount = reader.ReadInt32();
            _position = reader.ReadUInt16();

            _currentRecord = 0;

            return true;
        }
    }
}