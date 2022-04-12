using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if ENABLE_ISA
using BnsBinTool.Core.Helpers;
#else
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
#endif

namespace BnsBinTool.Core.Serialization
{
    public unsafe class RecordCompressedWriter
    {
        private static readonly byte[] EmptyBlockHeader = new byte[4 * sizeof(int) + sizeof(ushort)];
        private readonly int _compressionBlockSize;
        private int _beginning = -1;
        private ushort _blockSize;
        private int _blockBegin = -1;
        private int _blockCount;
        private int _recordCount;
        private ulong _lowKey;
        private ulong _highKey;

        private readonly List<ushort> _recordOffsetInBlocks = new List<ushort>();

#if ENABLE_ISA
        private const ushort BufferSize = ushort.MaxValue;
        private readonly IntPtr _levelBufferHandle = Marshal.AllocHGlobal(331776);
        private readonly byte* _levelBuffer;
        private readonly IntPtr _dataBufferHandle = Marshal.AllocHGlobal(BufferSize);
        private readonly byte* _dataBuffer;
        private int _dataFilled = -1;
        private readonly IntPtr _compressedBufferHandle = Marshal.AllocHGlobal(BufferSize);
        private readonly byte* _compressedBuffer;
#else
        private DeflaterOutputStream _deflateStream;
#endif

        public RecordCompressedWriter(int compressionBlockSize = ushort.MaxValue)
        {
            _compressionBlockSize = compressionBlockSize;

#if ENABLE_ISA
            _levelBuffer = (byte*) _levelBufferHandle.ToPointer();
            _dataBuffer = (byte*) _dataBufferHandle.ToPointer();
            _compressedBuffer = (byte*) _compressedBufferHandle.ToPointer();
#endif
        }

#if ENABLE_ISA
        ~RecordCompressedWriter()
        {
            Marshal.FreeHGlobal(_levelBufferHandle);
            Marshal.FreeHGlobal(_dataBufferHandle);
            Marshal.FreeHGlobal(_compressedBufferHandle);
        }
#endif

        public void BeginWrite(BinaryWriter writer)
        {
            _beginning = (int) writer.BaseStream.Position;
            _blockCount = 0;
#if ENABLE_ISA
            _dataFilled = -1;
#endif
            writer.Write(0); // Table size
            writer.Write(true); // Is compressed
            writer.Write(0); // Compressed block count
            writer.Write((short) 8);
        }

        public void EndWrite(BinaryWriter writer)
        {
#if ENABLE_ISA
            if (_dataFilled >= 0)
#else
            if (_deflateStream != null)
#endif
                EndCompressedBlock(writer);

            var position = (int) writer.BaseStream.Position;
            writer.Seek(_beginning, SeekOrigin.Begin);
            writer.Write(position - _beginning - sizeof(int)); // Size
            writer.Write(true); // Is compressed
            writer.Write(_blockCount); // Compressed block count
            writer.Seek(position, SeekOrigin.Begin);
        }

        public void WriteRecord(BinaryWriter writer, ReadOnlySpan<byte> data, ReadOnlySpan<byte> lookupBuffer)
        {
            ref var byteRef = ref MemoryMarshal.GetReference(data);
            var key = *((ulong*) Unsafe.AsPointer(ref byteRef) + 1);

            // Do not add _blockSize to it as it gets reset in BeginCompressedBlock
            var size = (ushort) (data.Length + lookupBuffer.Length);

#if ENABLE_ISA
            if (_dataFilled == -1)
#else
            if (_deflateStream == null)
#endif
            {
                BeginCompressedBlock(writer);
                _lowKey = key;
            }

            if (size + _blockSize > _compressionBlockSize)
            {
                EndCompressedBlock(writer);
                BeginCompressedBlock(writer);

                _lowKey = key;
            }

            _highKey = key;
            _recordOffsetInBlocks.Add(_blockSize);
            _blockSize += size;
            _recordCount++;

#if ENABLE_ISA
            data.CopyTo(new Span<byte>(_dataBuffer + _dataFilled, _compressionBlockSize - _dataFilled));
            _dataFilled += data.Length;
            lookupBuffer.CopyTo(new Span<byte>(_dataBuffer + _dataFilled, _compressionBlockSize - _dataFilled));
            _dataFilled += lookupBuffer.Length;
#else
            _deflateStream.Write(data);
            _deflateStream.Write(lookupBuffer);
#endif
        }

        private void BeginCompressedBlock(BinaryWriter writer)
        {
            _lowKey = 0;
            _highKey = 0;
            _blockSize = 0;
            _recordCount = 0;
            _recordOffsetInBlocks.Clear();
            var baseStream = writer.BaseStream;
            _blockBegin = (int) baseStream.Position;
            writer.Write(EmptyBlockHeader);

#if ENABLE_ISA
            _dataFilled = 0;
#else
            _deflateStream = new DeflaterOutputStream(baseStream, new Deflater(6)) {IsStreamOwner = false};
#endif
        }

        private void EndCompressedBlock(BinaryWriter writer)
        {
            _blockCount++;

#if ENABLE_ISA
            var size = IsaLib.zlib_compress(_dataBuffer, _dataFilled, _compressedBuffer, BufferSize, _levelBuffer);
            if (size == 0)
                ThrowHelper.ThrowException("Failed to compress buffer");
            writer.Write(new ReadOnlySpan<byte>(_compressedBuffer, (int) size));
#else
            _deflateStream.Close();
            _deflateStream = null;
#endif

            var afterBlockPosition = (int) writer.BaseStream.Position;
            var compressedSize = (ushort) (afterBlockPosition - _blockBegin - EmptyBlockHeader.Length);
            writer.Seek(_blockBegin, SeekOrigin.Begin);
            writer.Write(_lowKey);
            writer.Write(_highKey);
            writer.Write(compressedSize);
            writer.Seek(afterBlockPosition, SeekOrigin.Begin);
            writer.Write(_blockSize);
            writer.Write(_recordCount);

            foreach (var recordOffsetInBlock in _recordOffsetInBlocks)
            {
                writer.Write(recordOffsetInBlock);
            }
        }
    }
}