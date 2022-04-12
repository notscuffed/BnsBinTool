using System;
using System.Runtime.InteropServices;

namespace BnsBinTool.Core.DataStructs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TRef
    {
        public int Table;
        public int Id;
        public int Variant;

        public TRef(int table, int id, int variant = 0)
        {
            Table = table;
            Id = id;
            Variant = variant;
        }

        public override string ToString()
        {
            return $"({Id}:{Variant}, table: {Table})";
        }

        public static implicit operator int(TRef r) => r.Id;

        public static bool operator ==(TRef a, TRef b)
        {
            return
                a.Table == b.Table &&
                a.Id == b.Id &&
                a.Variant == b.Variant;
        }

        public static bool operator !=(TRef a, TRef b)
        {
            return !(a == b);
        }

        public bool Equals(TRef other)
        {
            return Table == other.Table && Id == other.Id && Variant == other.Variant;
        }

        public override bool Equals(object obj)
        {
            return obj is TRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Table, Id, Variant);
        }
    }
}