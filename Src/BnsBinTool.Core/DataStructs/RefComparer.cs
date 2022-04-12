using System;
using System.Collections.Generic;

namespace BnsBinTool.Core.DataStructs
{
    /// <summary>
    /// Might not work correctly in collections with duplicate keys
    /// </summary>
    public class RefComparer : IComparer<Ref>
    {
        public static readonly RefComparer Instance = new RefComparer();
        
        public int Compare(Ref x, Ref y)
        {
            if ((long) x == (long) y)
                return 0;
            if ((long) x > (long) y)
                return 1;
            return -1;
        }
    }
    
    public class RefEqualityComparer : IEqualityComparer<Ref>
    {
        public static readonly RefEqualityComparer Instance = new RefEqualityComparer();
        
        public bool Equals(Ref x, Ref y)
        {
            return (long) x == (long) y;
        }

        public int GetHashCode(Ref obj)
        {
            return HashCode.Combine(obj.Id, obj.Variant);
        }
    }
}