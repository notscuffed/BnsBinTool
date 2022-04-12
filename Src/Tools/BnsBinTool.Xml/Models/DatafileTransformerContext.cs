using System.Collections.Generic;
using System.Linq;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Models;
using BnsBinTool.Xml.Helpers;

namespace BnsBinTool.Xml.Models
{
    public class DatafileTransformerContext
    {
        public readonly Dictionary<XmlPosition, int> AutoRecordIds = new();
        public readonly int[] LastRecordId;

        public DatafileTransformerContext(ICollection<Table> tables, DatafileDefinition datafileDef)
        {
            LastRecordId = new int[datafileDef.TableDefinitions.Max(x => x.Type) + 1];

            foreach (var table in tables)
            {
                LastRecordId[table.Type] = table.Records.LastOrDefault()?.RecordId ?? 0;
            }
        }
    }

}