using System.Runtime.InteropServices;

namespace BnsBinTool.Core.Helpers
{
    public static unsafe class IsaLib
    {
        [DllImport("isalib", CallingConvention = CallingConvention.Cdecl)]
        [SuppressGCTransition]
        public static extern bool zlib_decompress(byte* data, nint data_size, byte* buffer, nint buffer_size);
        
        [DllImport("isalib", CallingConvention = CallingConvention.Cdecl)]
        [SuppressGCTransition]
        public static extern nint zlib_compress(byte* data, nint data_size, byte* buffer, nint buffer_size, byte* level_buffer);
    }
}