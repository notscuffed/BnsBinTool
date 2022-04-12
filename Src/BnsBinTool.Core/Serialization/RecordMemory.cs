using System.Runtime.InteropServices;

namespace BnsBinTool.Core.Serialization
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RecordMemory
    {
        public byte* DataBegin;
        public byte* StringBufferBegin;
        public int DataSize;
        public int StringBufferSize;
        public short Type;
    }
}