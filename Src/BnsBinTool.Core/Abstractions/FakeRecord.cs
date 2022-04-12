using BnsBinTool.Core.DataStructs;

namespace BnsBinTool.Core.Abstractions
{
    public class FakeRecord : IRecord
    {
        public FakeRecord(Ref @ref, short tableType)
        {
            Ref = @ref;
            TableType = tableType;
        }
        
        public FakeRecord(TRef tref)
        {
            Ref = tref;
            TableType = (short) tref.Table;
        }

        public Ref Ref { get; set; }
        public short TableType { get; }
    }
}