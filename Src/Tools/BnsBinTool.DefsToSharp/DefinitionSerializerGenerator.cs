using System;
using System.Linq;
using BnsBinTool.Core.Abstractions;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;

namespace BnsBinTool.DefsToSharp
{
    public class DefinitionSerializerGenerator
    {
        public void GenerateSerializeMethod(CodeWriter writer, ITableDefinition tableDef)
        {
            var prefix = tableDef is SubtableDefinition ? "override " : "virtual ";
            writer.WriteIndentedLine($"public {prefix}unsafe ushort Serialize(byte* buffer, StreamWriter stringWriter)");
            writer.BeginBlock();
            {
                if (tableDef is SubtableDefinition)
                {
                    writer.WriteIndentedLine("base.Serialize(buffer, stringWriter);");
                }
                
                if (tableDef.Attributes.Any(x => x.Type == AttributeType.TString || x.Type == AttributeType.TNative))
                {
                    writer.WriteIndentedLine("stringWriter.Flush();");
                    writer.WriteIndentedLine("var position = (int) stringWriter.BaseStream.Position;");
                }

                foreach (var attrDef in tableDef.Attributes)
                {
                    SerializeAttribute(writer, attrDef);
                }

                writer.WriteIndentedLine("*(short*)(buffer) = 1;");
                writer.WriteIndentedLine($"*(short*)(buffer+2) = {tableDef.SubclassType};");
                writer.WriteIndentedLine($"*(short*)(buffer+4) = {tableDef.Size};");
                
                writer.WriteIndentedLine($"return {tableDef.Size};");
            }
            writer.EndBlock();
        }

        private static void SerializeAttribute(CodeWriter writer, AttributeDefinition attrDef)
        {
            if (attrDef.Repeat == 1)
            {
                SerializeAttributeAtIndex(writer, "", attrDef.Offset, attrDef);
            }
            else
            {
                for (var i = 0; i < attrDef.Repeat; i++)
                {
                    SerializeAttributeAtIndex(writer, $"[{i}]", attrDef.Offset + i * attrDef.Size, attrDef);
                }
            }
        }

        private static void SerializeAttributeAtIndex(CodeWriter writer, string indexer, int offset, AttributeDefinition attrDef)
        {
            switch (attrDef.Type)
            {
                case AttributeType.TInt8:
                    writer.WriteIndentedLine($"*(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TInt16:
                    writer.WriteIndentedLine($"*(short*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TInt32:
                    writer.WriteIndentedLine($"*(int*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TInt64:
                    writer.WriteIndentedLine($"*(long*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TFloat32:
                    writer.WriteIndentedLine($"*(float*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TBool:
                    writer.WriteIndentedLine($"*(bool*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TString:
                    writer.WriteIndentedLine($"*(int*)(buffer + {offset}) = position;");
                    writer.WriteIndentedLine($"stringWriter.Write({attrDef.Name}{indexer});");
                    writer.WriteIndentedLine("stringWriter.Write('\\0');");
                    writer.WriteIndentedLine($"position += ({attrDef.Name}{indexer}.Length + 1) * 2;");
                    break;
                case AttributeType.TNative:
                    writer.WriteIndentedLine($"*(int*)(buffer + {offset}) = ({attrDef.Name}{indexer}.Length + 1) * 2;");
                    writer.WriteIndentedLine($"*(int*)(buffer + {offset + 4}) = position;");
                    writer.WriteIndentedLine($"stringWriter.Write({attrDef.Name}{indexer});");
                    writer.WriteIndentedLine($"stringWriter.Write('\\0');");
                    writer.WriteIndentedLine($"position += ({attrDef.Name}{indexer}.Length + 1) * 2;");
                    break;
                case AttributeType.TSeq:
                case AttributeType.TSeq16:
                case AttributeType.TProp_seq:
                case AttributeType.TProp_field:
                    writer.WriteIndentedLine($"*({attrDef.SequenceDef.Name}*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TSub:
                    writer.WriteIndentedLine($"*(short*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TVector32:
                    writer.WriteIndentedLine($"*({nameof(Vector32)}*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TIColor:
                    writer.WriteIndentedLine($"*({nameof(IColor)}*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TBox:
                    writer.WriteIndentedLine($"*({nameof(Box)}*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TMsec:
                    writer.WriteIndentedLine($"*(int*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TDistance:
                    writer.WriteIndentedLine($"*(short*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TVelocity:
                    writer.WriteIndentedLine($"*(short*)(buffer + {offset}) = {attrDef.Name}{indexer};");
                    break;
                case AttributeType.TTime32:
                    writer.WriteIndentedLine($"*(int*)(buffer + {offset}) = (int)({attrDef.Name}{indexer} - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;");
                    break;
                case AttributeType.TTime64:
                    writer.WriteIndentedLine($"*(long*)(buffer + {offset}) = (long)({attrDef.Name}{indexer} - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;");
                    break;
                case AttributeType.TRef:
                    writer.WriteIndentedLine($"if ({attrDef.Name}{indexer} != null)");
                    writer.Indent();
                    writer.WriteIndentedLine($"*({nameof(Ref)}*)(buffer + {offset}) = {attrDef.Name}{indexer}.{nameof(IRecord.Ref)};");
                    writer.Unindent();
                    break;
                case AttributeType.TTRef:
                    writer.WriteIndentedLine($"if ({attrDef.Name}{indexer} != null)");
                    writer.Indent();
                    writer.WriteIndentedLine($"*({nameof(TRef)}*)(buffer + {offset}) = {attrDef.Name}{indexer}.{nameof(IRecord.TRef)};");
                    writer.Unindent();
                    break;
                case AttributeType.TIcon:
                    writer.WriteIndentedLine($"if ({attrDef.Name}{indexer} != null)");
                    writer.Indent();
                    writer.WriteIndentedLine($"*({nameof(IconRef)}*)(buffer + {offset}) = new {nameof(IconRef)}({attrDef.Name}{indexer}.{nameof(IRecord.Ref)}, {attrDef.Name}_IconIndex{indexer});");
                    writer.Unindent();
                    break;
                case AttributeType.TScript_obj:
                    // Ignore
                    break;
                case AttributeType.TNone when attrDef.Size == 4:
                    writer.WriteIndentedLine($"*(int*)(buffer + {offset}) = {attrDef.Name}{indexer}; // None 4");
                    break;
                case AttributeType.TNone when attrDef.Size == 8:
                case AttributeType.TXUnknown1 when attrDef.Size == 8:
                case AttributeType.TXUnknown2 when attrDef.Size == 8:
                    writer.WriteIndentedLine($"*(long*)(buffer + {offset}) = {attrDef.Name}{indexer}; // None 8");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(attrDef.Type), $"Value: {attrDef.Type}");
            }
        }
    }
}