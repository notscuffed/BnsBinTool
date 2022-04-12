using System;
using System.Runtime.InteropServices;

namespace BnsBinTool.Core.DataStructs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IconRef
    {
        public int IconTextureRecordId;
        public int IconTextureVariantId;
        public int _unk_i32_0;

        public IconRef(int iconTextureRecordId, int iconTextureVariantId = 0, int unk_i32_0 = 1) : this()
        {
            IconTextureRecordId = iconTextureRecordId;
            IconTextureVariantId = iconTextureVariantId;
            _unk_i32_0 = unk_i32_0;
        }
        
        public IconRef(Ref @ref, int unk_i32_0 = 1) : this()
        {
            IconTextureRecordId = @ref.Id;
            IconTextureVariantId = @ref.Variant;
            _unk_i32_0 = unk_i32_0;
        }

        public override string ToString()
        {
            return $"(Id: {IconTextureRecordId}, Variant: {IconTextureVariantId}, Unk.: {_unk_i32_0})";
        }
        
        public static implicit operator int(IconRef r) => r.IconTextureRecordId;
        
        public static bool operator ==(IconRef a, IconRef b)
        {
            return
                a.IconTextureRecordId == b.IconTextureRecordId &&
                a.IconTextureVariantId == b.IconTextureVariantId &&
                a._unk_i32_0 == b._unk_i32_0;
        }

        public static bool operator !=(IconRef a, IconRef b)
        {
            return !(a == b);
        }
        
        public bool Equals(IconRef other)
        {
            return IconTextureRecordId == other.IconTextureRecordId && IconTextureVariantId == other.IconTextureVariantId && _unk_i32_0 == other._unk_i32_0;
        }

        public override bool Equals(object obj)
        {
            return obj is IconRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IconTextureRecordId, IconTextureVariantId, _unk_i32_0);
        }
    }
}