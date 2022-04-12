using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BnsBinTool.Core.Helpers
{
    public static class EnumHelper<T> where T : struct
    {
        internal static readonly Store<T> Data = new Store<T>();

        internal class Store<TStore>
        {
            internal class Comparer : IEqualityComparer<KeyValuePair<T, string>>
            {
                internal static Comparer Instance = new Comparer();
                
                public bool Equals(KeyValuePair<T, string> x, KeyValuePair<T, string> y)
                {
                    return GetHashCode(x) == GetHashCode(y);
                }

                public int GetHashCode(KeyValuePair<T, string> obj)
                {
                    var value = obj.Key;
                    return Unsafe.As<T, int>(ref value);
                }
            }
            
            public readonly ImmutableDictionary<string, T> KeyToValueMap;
            public readonly string[] EnumValueNames;
            public readonly string[] EnumOriginalValueNames;

            public Store()
            {
                KeyToValueMap = Enum.GetNames(typeof(T))
                    .ToImmutableDictionary(x => x, x => (T) Enum.Parse(typeof(T), x));

                EnumValueNames = typeof(T)
                    .GetFields()
                    .Where(x => x.FieldType == typeof(T))
                    .Select(x => new KeyValuePair<T, string>(KeyToValueMap[x.Name], x.Name))
                    .Distinct(Comparer.Instance)
                    .Select(x => x.Value)
                    .ToArray();

                EnumOriginalValueNames = typeof(T)
                    .GetFields()
                    .Where(x => x.FieldType == typeof(T) && x.GetCustomAttribute(typeof(OriginalEnumNameAttribute)) is OriginalEnumNameAttribute)
                    .Select(x => new KeyValuePair<T, string>(KeyToValueMap[x.Name], ((OriginalEnumNameAttribute) x.GetCustomAttribute(typeof(OriginalEnumNameAttribute))).OriginalName))
                    .Distinct(Comparer.Instance)
                    .Select(x => x.Value)
                    .ToArray();
            }
        }
    }

    public static class EnumHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static TTo Convert<TFrom, TTo>(TFrom value)
            where TFrom : struct
            where TTo : struct
        {
            return Parse<TTo>(
                Stringify(value)
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static TTo ConvertTo<TFrom, TTo>(this TFrom value)
            where TFrom : struct
            where TTo : struct
        {
            return Parse<TTo>(
                Stringify(value)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static T Parse<T>(string key) where T : struct => EnumHelper<T>.Data.KeyToValueMap[key];

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static string Stringify<T>(T value) where T : struct
        {
            return EnumHelper<T>.Data.EnumValueNames[Unsafe.As<T, int>(ref value)];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static string StringifyOriginal<T>(T value) where T : struct
        {
            return EnumHelper<T>.Data.EnumOriginalValueNames[Unsafe.As<T, int>(ref value)];
        }
    }

    public class OriginalEnumNameAttribute : Attribute
    {
        public string OriginalName { get; }

        public OriginalEnumNameAttribute(string name)
        {
            OriginalName = name;
        }
    }
}