using System.Collections.Generic;
using System.Linq;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;

namespace BnsBinTool.DefsToSharp
{
    public class DefinitionResolverGenerator
    {
        private readonly AttributeType[] RefTypes = {AttributeType.TRef, AttributeType.TTRef, AttributeType.TIcon};
        private readonly DatafileDefinition _datafileDefinition;
        private readonly Dictionary<short, TableDefinition> _tables;

        public DefinitionResolverGenerator(DatafileDefinition datafileDefinition)
        {
            _datafileDefinition = datafileDefinition;
            _tables = datafileDefinition.TableDefinitions.ToDictionary(x => x.Type);
        }

        public void GenerateResolveMethod(CodeWriter writer, TableDefinition baseDef, ITableDefinition tableDef)
        {
            if (tableDef is SubtableDefinition && !tableDef.Attributes.Any(x => RefTypes.Contains(x.Type)))
                return;

            var prefix = tableDef is SubtableDefinition ? "override " : "virtual ";

            if (tableDef is TableDefinition && !baseDef.Subtables
                .SelectMany(x => x.Attributes)
                .Any(x => RefTypes.Contains(x.Type)))
            {
                prefix = "";
            }


            writer.WriteIndentedLine($"public {prefix}void Resolve(Tables tables)");
            writer.BeginBlock();

            foreach (var attrDef in tableDef.Attributes)
            {
                ResolveAttribute(writer, attrDef, tableDef);
            }

            if (tableDef is SubtableDefinition)
            {
                writer.WriteIndentedLine("base.Resolve(tables);");
            }

            writer.EndBlock();
        }

        private void ResolveAttribute(CodeWriter writer, AttributeDefinition attrDef, ITableDefinition tableDef)
        {
            TableDefinition referedTable;

            switch (attrDef.Type)
            {
                case AttributeType.TRef:
                    referedTable = _tables[attrDef.ReferedTable];
                    break;
                case AttributeType.TTRef:
                    if (attrDef.Repeat == 1)
                    {
                        ResolveTRefAtIndex(writer, "", attrDef);
                    }
                    else
                    {
                        for (var i = 0; i < attrDef.Repeat; i++)
                        {
                            ResolveTRefAtIndex(writer, $"[{i}]", attrDef);
                        }
                    }

                    return;
                case AttributeType.TIcon:
                    referedTable = _tables[_datafileDefinition.IconTextureTableId];
                    break;
                default:
                    return;
            }

            if (attrDef.Repeat == 1)
            {
                ResolveAttributeAtIndex(writer, "", "", attrDef, referedTable);
            }
            else
            {
                for (var i = 0; i < attrDef.Repeat; i++)
                {
                    ResolveAttributeAtIndex(writer, $"[{i}]", $"_{i}", attrDef, referedTable);
                }
            }
        }

        private static void ResolveTRefAtIndex(CodeWriter writer, string indexer, AttributeDefinition attrDef)
        {
            var refVar = "_" + char.ToLower(attrDef.Name[0]) + attrDef.Name[1..] + indexer;
            writer.WriteIndentedLine($"{attrDef.Name}{indexer} = tables.ResolveTRef({refVar});");
        }

        private static void ResolveAttributeAtIndex(CodeWriter writer, string indexer, string suffix, AttributeDefinition attrDef, TableDefinition referedTable)
        {
            var refVar = "_" + char.ToLower(attrDef.Name[0]) + attrDef.Name[1..] + indexer;
            var recordVar = char.ToLower(attrDef.Name[0]) + attrDef.Name[1..] + suffix + "Record";

            writer.WriteIndentedLine($"if ({refVar} != new {nameof(Ref)}(0,0))");
            {
                writer.BeginBlock();

                writer.WriteIndentedLine($"if (tables.{referedTable.Name}.TryGetValue({refVar}, out var {recordVar}))");
                {
                    writer.Indent();
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = {recordVar};");
                    writer.Unindent();
                }
                writer.WriteIndentedLine("else");
                {
                    writer.Indent();
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = new {referedTable.Name} {{Ref = {refVar}}};");
                    writer.Unindent();
                }

                writer.EndBlock();
            }
        }
    }
}