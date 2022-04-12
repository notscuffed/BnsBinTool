using System;
using System.Linq;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;

namespace BnsBinTool.DefsToSharp
{
    public class DefinitionDeserializerGenerator
    {
        public void GenerateDeserializeMethod(CodeWriter writer, ITableDefinition tableDef)
        {
            if (tableDef is SubtableDefinition && !tableDef.Attributes.Any())
                return;
            
            var prefix = tableDef is SubtableDefinition ? "new " : "";
            writer.WriteIndentedLine($"public {prefix}unsafe void Deserialize(byte* buffer, byte* stringBuffer)");
            writer.BeginBlock();
            {
                foreach (var attrDef in tableDef.Attributes)
                {
                    DeserializeAttribute(writer, attrDef);
                }

                if (tableDef is SubtableDefinition)
                {
                    writer.WriteIndentedLine("base.Deserialize(buffer, stringBuffer);");
                }
            }
            writer.EndBlock();
        }

        private static void DeserializeAttribute(CodeWriter writer, AttributeDefinition attrDef)
        {
            if (attrDef.Repeat == 1)
            {
                DeserializeAttributeAtIndex(writer, "", attrDef.Offset, attrDef);
            }
            else
            {
                for (var i = 0; i < attrDef.Repeat; i++)
                {
                    DeserializeAttributeAtIndex(writer, $"[{i}]", attrDef.Offset + i * attrDef.Size, attrDef);
                }
            }
        }

        private static void DeserializeAttributeAtIndex(CodeWriter writer, string indexer, int offset, AttributeDefinition attrDef)
        {
            switch (attrDef.Type)
            {
                case AttributeType.TInt8:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(buffer + {offset});");
                    break;
                case AttributeType.TInt16:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(short*)(buffer + {offset});");
                    break;
                case AttributeType.TInt32:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(int*)(buffer + {offset});");
                    break;
                case AttributeType.TInt64:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(long*)(buffer + {offset});");
                    break;
                case AttributeType.TFloat32:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(float*)(buffer + {offset});");
                    break;
                case AttributeType.TBool:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(bool*)(buffer + {offset});");
                    break;
                case AttributeType.TString:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = new string((char*)(stringBuffer + *(int*)(buffer + {offset})));");
                    break;
                case AttributeType.TNative:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = new string((char*)(stringBuffer + *(int*)(buffer + {offset + 4})));");
                    break;
                case AttributeType.TSeq:
                case AttributeType.TSeq16:
                case AttributeType.TProp_seq:
                case AttributeType.TProp_field:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *({attrDef.SequenceDef.Name}*)(buffer + {offset});");
                    break;
                case AttributeType.TSub:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(short*)(buffer + {offset});");
                    break;
                case AttributeType.TVector32:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *({nameof(Vector32)}*)(buffer + {offset});");
                    break;
                case AttributeType.TIColor:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *({nameof(IColor)}*)(buffer + {offset});");
                    break;
                case AttributeType.TBox:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *({nameof(Box)}*)(buffer + {offset});");
                    break;
                case AttributeType.TMsec:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(int*)(buffer + {offset});");
                    break;
                case AttributeType.TDistance:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(short*)(buffer + {offset});");
                    break;
                case AttributeType.TVelocity:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(short*)(buffer + {offset});");
                    break;
                case AttributeType.TTime32:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = new DateTime(1970,1,1).AddSeconds(*(int*)(buffer + {offset}));");
                    break;
                case AttributeType.TTime64:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = new DateTime(1970,1,1).AddSeconds(*(long*)(buffer + {offset}));");
                    break;
                case AttributeType.TRef:
                    writer.WriteIndentedLine($"_{char.ToLower(attrDef.Name[0])}{attrDef.Name[1..]}{indexer} = *({nameof(Ref)}*)(buffer + {offset});");
                    break;
                case AttributeType.TTRef:
                    writer.WriteIndentedLine($"_{char.ToLower(attrDef.Name[0])}{attrDef.Name[1..]}{indexer} = *({nameof(TRef)}*)(buffer + {offset});");
                    break;
                case AttributeType.TIcon:
                    writer.WriteIndentedLine($"_{char.ToLower(attrDef.Name[0])}{attrDef.Name[1..]}{indexer} = *({nameof(IconRef)}*)(buffer + {offset});");
                    writer.WriteIndentedLine($"{attrDef.Name}_IconIndex{indexer} = _{char.ToLower(attrDef.Name[0])}{attrDef.Name[1..]}{indexer}.{nameof(IconRef._unk_i32_0)};");
                    break;
                case AttributeType.TScript_obj:
                    // Ignore
                    break;
                case AttributeType.TNone when attrDef.Size == 4:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(int*)(buffer + {offset}); // None 4");
                    break;
                case AttributeType.TNone when attrDef.Size == 8:
                case AttributeType.TXUnknown1 when attrDef.Size == 8:
                case AttributeType.TXUnknown2 when attrDef.Size == 8:
                    writer.WriteIndentedLine($"{attrDef.Name}{indexer} = *(long*)(buffer + {offset}); // None 8");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(attrDef.Type), $"Value: {attrDef.Type}");
            }
        }
    }
}