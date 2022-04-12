using System;
using System.Runtime.InteropServices;

namespace BnsBinTool.Core.DataStructs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Box
    {
        public short X1;
        public short Y1;
        public short Z1;
        public short X2;
        public short Y2;
        public short Z2;

        public Vector32 Begin => new(X1, Y1, Z1);
        public Vector32 End => new(X2, Y2, Z2);

        public override string ToString()
        {
            return $"({X1}, {Y1}, {Z1} - {X2}, {Y2}, {Z2})";
        }

        public Box(short x1, short y1, short z1, short x2, short y2, short z2)
        {
            X1 = x1;
            Y1 = y1;
            Z1 = z1;
            X2 = x2;
            Y2 = y2;
            Z2 = z2;
        }

        public static bool operator ==(Box a, Box b)
        {
            return
                a.X1 == b.X1 &&
                a.Y1 == b.Y1 &&
                a.Z1 == b.Z1 &&
                a.X2 == b.X2 &&
                a.Y2 == b.Y2 &&
                a.Z2 == b.Z2;
        }

        public static bool operator !=(Box a, Box b)
        {
            return !(a == b);
        }

        public static Box Parse(string input)
        {
            var items = input.Split(',');

            if (items.Length != 6)
                throw new ArgumentException("Invalid Box string input");

            return new Box(
                short.Parse(items[0]),
                short.Parse(items[1]),
                short.Parse(items[2]),
                short.Parse(items[3]),
                short.Parse(items[4]),
                short.Parse(items[5])
            );
        }

        public bool Equals(Box other)
        {
            return X1 == other.X1 && Y1 == other.Y1 && Z1 == other.Z1 && X2 == other.X2 && Y2 == other.Y2 && Z2 == other.Z2;
        }

        public override bool Equals(object obj)
        {
            return obj is Box other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X1, Y1, Z1, X2, Y2, Z2);
        }
    }
}