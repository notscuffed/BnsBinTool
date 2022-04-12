using System;
using System.IO;
using System.Runtime.InteropServices;
using BnsBinTool.Core.Abstractions;
using BnsBinTool.Core.Helpers;

namespace BnsBinTool.Core.Serialization
{
    /// <summary>
    /// Reads uncompressed table record by record
    /// </summary>
    public unsafe class RecordUncompressedReader : IRecordReader
    {
        private int _stringLookupBufferSize = 0x4000;
        private IntPtr _stringLookupBufferHandle = Marshal.AllocHGlobal(0x4000);

        private int _recordBufferSize = 0x1000;
        private IntPtr _recordBufferHandle = Marshal.AllocHGlobal(0x1000);

        private byte* _stringLookupBuffer;
        private byte* _recordBuffer;

        private long _recordCount = -1;
        private int _recordsSize;
        private int _currentRecord;

        private int _stringLookupSize;
        private long _stringLookupStart;

        private int _paddingSize;

        public RecordUncompressedReader()
        {
            _stringLookupBuffer = (byte*) _stringLookupBufferHandle.ToPointer();
            _recordBuffer = (byte*) _recordBufferHandle.ToPointer();
        }

        ~RecordUncompressedReader()
        {
            Marshal.FreeHGlobal(_stringLookupBufferHandle);
            Marshal.FreeHGlobal(_recordBufferHandle);
        }

        public bool Initialize(BinaryReader reader, bool is64Bit)
        {
            _currentRecord = 0;
            _recordCount = reader.ReadInt32();

            if (is64Bit)
            {
                if (reader.ReadInt32() != 0)
                    ThrowHelper.ThrowException("Unexpected integer");
            }
            
            _recordsSize = reader.ReadInt32();
            _stringLookupSize = reader.ReadInt32();
            _paddingSize = 0;

            EnsureBufferSize(ref _stringLookupBufferHandle, ref _stringLookupBuffer, ref _stringLookupBufferSize, _stringLookupSize);

            if (reader.ReadByte() != 1)
                ThrowHelper.ThrowException("Unexpected byte");

            var position = reader.BaseStream.Position;
            _stringLookupStart = position + _recordsSize;

            reader.BaseStream.Seek(_recordsSize, SeekOrigin.Current);

            var read = reader.Read(new Span<byte>(_stringLookupBuffer, _stringLookupSize));

            if (read != _stringLookupSize)
                ThrowHelper.ThrowException("Failed to read string lookup");

            reader.BaseStream.Seek(position, SeekOrigin.Begin);

            return true;
        }

        public bool Read(BinaryReader reader, ref RecordMemory recordMemory)
        {
            if (_recordCount == -1)
                ThrowHelper.ThrowException("Uninitialized");

            if (reader.BaseStream.Position > _stringLookupStart)
                ThrowHelper.ThrowException("Read past string lookup while reading records");

            if (_currentRecord != _recordCount && reader.BaseStream.Position != _stringLookupStart)
            {
                reader.Read(new Span<byte>(_recordBuffer, 6));
                var size = *(ushort*) (_recordBuffer + 4);
                
                EnsureBufferSize(ref _recordBufferHandle, ref _recordBuffer, ref _recordBufferSize, size);

                if (reader.Read(new Span<byte>(_recordBuffer + 6, size - 6)) != size - 6)
                    ThrowHelper.ThrowException("Failed to read record");
                
                recordMemory.Type = *(short*)(_recordBuffer + 2);
                recordMemory.DataBegin = _recordBuffer;
                recordMemory.DataSize = size;
                recordMemory.StringBufferBegin = _stringLookupBuffer;
                recordMemory.StringBufferSize = _stringLookupSize;

                _currentRecord++;
                return true;
            }

            if (reader.BaseStream.Position > _stringLookupStart)
                ThrowHelper.ThrowException("Read past string lookup while reading records");

            if (reader.BaseStream.Position < _stringLookupStart)
            {
                var paddingSize = (int) (_stringLookupStart - reader.BaseStream.Position);
                _paddingSize = paddingSize;
                EnsureBufferSize(ref _recordBufferHandle, ref _recordBuffer, ref _recordBufferSize, paddingSize);
                
                if (reader.Read(new Span<byte>(_recordBuffer, paddingSize)) != paddingSize)
                    ThrowHelper.ThrowException("Failed to padding");
            }

            reader.BaseStream.Seek(_stringLookupStart + _stringLookupSize, SeekOrigin.Begin);

            return false;
        }

        public void GetPadding(out Span<byte> outPadding)
        {
            outPadding = new Span<byte>(_recordBuffer, _paddingSize);
        }

        public int GetRecordCountOffset()
        {
            return (int) (_recordCount - _currentRecord);
        }

        private static void EnsureBufferSize(ref IntPtr intPtr, ref byte* bytePtr, ref int currentSize, int size)
        {
            if (size <= currentSize)
                return;

            size = size + 0x1000 - (size % 0x1000);

            intPtr = Marshal.ReAllocHGlobal(intPtr, (IntPtr) size);
            bytePtr = (byte*) intPtr.ToPointer();

            currentSize = size;
        }
    }
}