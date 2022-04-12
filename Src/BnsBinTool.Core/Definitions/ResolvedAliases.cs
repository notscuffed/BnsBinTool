using System.Collections.Generic;
using BnsBinTool.Core.DataStructs;

namespace BnsBinTool.Core.Definitions
{
    public class ResolvedAliases
    {
        public Dictionary<int, Dictionary<Ref, string>> ByRef = new Dictionary<int, Dictionary<Ref, string>>();
        public Dictionary<int, Dictionary<string, Ref>> ByAlias = new Dictionary<int, Dictionary<string, Ref>>();
    }
}