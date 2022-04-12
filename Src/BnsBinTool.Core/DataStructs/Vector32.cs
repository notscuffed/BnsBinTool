using System;
using System.Runtime.InteropServices;

namespace BnsBinTool.Core.DataStructs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector32
    {
        public int X;
        public int Y;
        public int Z;

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public Vector32(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static bool operator ==(Vector32 a, Vector32 b)
        {
            return
                a.X == b.X &&
                a.Y == b.Y &&
                a.Z == b.Z;
        }

        public static bool operator !=(Vector32 a, Vector32 b)
        {
            return !(a == b);
        }

        public static Vector32 Parse(string input)
        {
            var items = input.Split(',');

            if (items.Length != 3)
                throw new ArgumentException("Invalid Vector32 string input");

            return new Vector32(
                int.Parse(items[0]),
                int.Parse(items[1]),
                int.Parse(items[2])
            );
        }

        public bool Equals(Vector32 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector32 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }
}