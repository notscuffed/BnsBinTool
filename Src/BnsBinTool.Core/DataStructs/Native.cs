using System;
using System.Runtime.InteropServices;

namespace BnsBinTool.Core.DataStructs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Native
    {
        public override int GetHashCode()
        {
            return HashCode.Combine(StringSize, Offset);
        }

        public int StringSize;
        public int Offset;

        public Native(int stringSize = 0, int offset = 0)
        {
            StringSize = stringSize;
            Offset = offset;
        }
        
        public static bool operator ==(Native a, Native b)
        {
            return
                a.StringSize == b.StringSize &&
                a.Offset == b.Offset;
        }

        public static bool operator !=(Native a, Native b)
        {
            return !(a == b);
        }
        
        public bool Equals(Native other)
        {
            return StringSize == other.StringSize && Offset == other.Offset;
        }

        public override bool Equals(object obj)
        {
            return obj is Native other && Equals(other);
        }
    }
}