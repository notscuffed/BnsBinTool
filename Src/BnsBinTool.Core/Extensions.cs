using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Core
{
    public static class Extensions
    {
        public static string ReadNString([NotNull] this BinaryReader reader)
        {
            var builder = new StringBuilder();
            var c = reader.ReadByte();

            while (c != 0)
            {
                builder.Append((char) c);
                c = reader.ReadByte();
            }

            return builder.ToString();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe string GetNStringUTF16(this byte[] array, int offset)
        {
            fixed (byte* memory = array)
                return new string((char*) (memory + offset));
        }

        public static Ref ToRef(this string str)
        {
            var split = str.Split(':', 2);

            if (split.Length == 2)
            {
                var id = int.Parse(split[0]);
                var variant = int.Parse(split[1]);
                return new Ref(id, variant);
            }

            ThrowHelper.ThrowInvalidRefException(str);
            return new Ref();
        }

        // Getters
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe T Get<T>(this Record record, int offset) where T : unmanaged
        {
            fixed (byte* ptr = record.Data)
            {
                return *(T*) (ptr + offset);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe T Get<T>(this byte[] data, int offset) where T : unmanaged
        {
            fixed (byte* ptr = data)
            {
                return *(T*) (ptr + offset);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe T Get<T>(this Span<byte> data, int offset) where T : unmanaged
        {
            fixed (byte* ptr = data)
            {
                return *(T*) (ptr + offset);
            }
        }

        // Setters
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe void Set<T>(this Record record, int offset, T value) where T : unmanaged
        {
            fixed (byte* ptr = record.Data)
            {
                *(T*) (ptr + offset) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe void Set<T>(this byte[] data, int offset, T value) where T : unmanaged
        {
            fixed (byte* ptr = data)
            {
                *(T*) (ptr + offset) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe void Set<T>(this Span<byte> data, int offset, T value) where T : unmanaged
        {
            fixed (byte* ptr = data)
            {
                *(T*) (ptr + offset) = value;
            }
        }
    }
}