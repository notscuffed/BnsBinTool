using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BnsBinTool.Core;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Xml.AliasResolvers
{
    public static class DatafileAliasResolver
    {
        public static ResolvedAliases Resolve(
            IEnumerable<Table> tablesEnumerable,
            DatafileDefinition datafileDefinition,
            bool ignoreInvalidReferences = false)
        {
            var tables = tablesEnumerable.ToDictionary(x => (int) x.Type);

            var resolvedAliases = new ResolvedAliases();

            foreach (var tableDef in datafileDefinition.TableDefinitions)
            {
                var byRef = new Dictionary<Ref, string>();
                var byAlias = new Dictionary<string, Ref>();

                resolvedAliases.ByRef[tableDef.Type] = byRef;
                resolvedAliases.ByAlias[tableDef.Type] = byAlias;
            }

            Parallel.ForEach(datafileDefinition.TableDefinitions, tableDef =>
            {
                var byRef = resolvedAliases.ByRef[tableDef.Type];
                var byAlias = resolvedAliases.ByAlias[tableDef.Type];

                var aliasAttrDef = tableDef["alias"];

                if (aliasAttrDef == null)
                    return;

                if (!tables.TryGetValue(tableDef.Type, out var table))
                    return;

                foreach (var record in table.Records)
                {
                    var reference = new Ref(record.RecordId, record.RecordVariationId);
                    var alias = record.StringLookup.GetString(record.Get<int>(aliasAttrDef.Offset));

                    if (alias != null)
                    {
                        byRef[reference] = alias;
                        byAlias[alias] = reference;
                    }
                    else
                    {
                        if (!ignoreInvalidReferences)
                            ThrowHelper.ThrowException("Failed to read alias (This usually means ur using wrong table definition)");
                    }
                }
            });

            return resolvedAliases;
        }
    }
}