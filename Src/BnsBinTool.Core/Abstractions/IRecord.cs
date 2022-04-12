using BnsBinTool.Core.DataStructs;

namespace BnsBinTool.Core.Abstractions
{
    public interface IRecord
    {
        Ref Ref { get; set; }
        TRef TRef => new TRef(TableType, Ref.Id, Ref.Variant);
        short TableType { get; }
    }
}