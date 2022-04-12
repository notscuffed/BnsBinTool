using System;
using System.Runtime.InteropServices;

namespace BnsBinTool.Core.DataStructs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IColor
    {
        public byte R;
        public byte G;
        public byte B;

        public IColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
        
        public static IColor Parse(string input)
        {
            var items = input.Split(',');
            
            if (items.Length != 3)
                throw new ArgumentException("Invalid Color string input");
            
            return new IColor(
                byte.Parse(items[0]),
                byte.Parse(items[1]),
                byte.Parse(items[2])
            );
        }
        
        public static bool operator ==(IColor a, IColor b)
        {
            return
                a.R == b.R &&
                a.G == b.G &&
                a.B == b.B;
        }

        public static bool operator !=(IColor a, IColor b)
        {
            return !(a == b);
        }
        
        public bool Equals(IColor other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object obj)
        {
            return obj is IColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B);
        }
    }
}