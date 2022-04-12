using System.Collections.Generic;
using System.IO;
using System.Linq;
using BnsBinTool.Core.Abstractions;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;

namespace BnsBinTool.DefsToSharp
{
    public class DefinitionGenerator
    {
        private readonly DefinitionDeserializerGenerator _definitionDeserializerGenerator;
        private readonly DefinitionSerializerGenerator _definitionSerializerGenerator;
        private readonly DefinitionTablesClassGenerator _definitionTablesClassGenerator;
        private readonly DefinitionResolverGenerator _definitionResolverGenerator;
        private readonly DatafileDefinition _datafileDef;
        private readonly SequenceTranslator _sequenceTranslator;
        private readonly string _outputPath;

        private readonly Dictionary<short, TableDefinition> _tables;

        public DefinitionGenerator(
            DatafileDefinition datafileDef,
            string outputPath,
            DefinitionDeserializerGenerator definitionDeserializerGenerator,
            DefinitionSerializerGenerator definitionSerializerGenerator,
            DefinitionTablesClassGenerator definitionTablesClassGenerator,
            DefinitionResolverGenerator definitionResolverGenerator,
            SequenceTranslator sequenceTranslator)
        {
            _datafileDef = datafileDef;
            _definitionDeserializerGenerator = definitionDeserializerGenerator;
            _definitionSerializerGenerator = definitionSerializerGenerator;
            _definitionTablesClassGenerator = definitionTablesClassGenerator;
            _definitionResolverGenerator = definitionResolverGenerator;
            _sequenceTranslator = sequenceTranslator;
            _outputPath = Path.GetFullPath(outputPath);

            _tables = datafileDef.TableDefinitions.ToDictionary(x => x.Type);
        }

        public void Generate(string @namespace)
        {
            Directory.CreateDirectory(_outputPath);

            foreach (var tableDefinition in _datafileDef.TableDefinitions)
            {
                WriteTableDefinition(tableDefinition, @namespace);
            }

            GenerateTablesClass(@namespace);
        }

        private void GenerateTablesClass(string @namespace)
        {
            using var writer = new CodeWriter(new StreamWriter(File.Open(
                _outputPath + "\\Tables.cs",
                FileMode.Create, FileAccess.Write, FileShare.Read)));

            writer.WriteIndentedLine("using System;");
            writer.WriteIndentedLine("using System.IO;");
            writer.WriteIndentedLine("using System.Linq;");
            writer.WriteIndentedLine("using System.Text;");
            writer.WriteIndentedLine("using System.Collections.Generic;");
            writer.WriteIndentedLine("using BnsBinTool.Core;");
            writer.WriteIndentedLine("using BnsBinTool.Core.Models;");
            writer.WriteIndentedLine("using BnsBinTool.Core.Sources;");
            writer.WriteIndentedLine("using BnsBinTool.Core.Definitions;");
            writer.WriteIndentedLine("using BnsBinTool.Core.DataStructs;");
            writer.WriteIndentedLine("using BnsBinTool.Core.Abstractions;");
            writer.WriteIndentedLine("using BnsBinTool.Core.Serialization;");
            writer.WriteIndentedLine();
            writer.WriteIndentedLine($"namespace {@namespace}");
            writer.BeginBlock();
            _definitionTablesClassGenerator.GenerateTablesClass(writer, _datafileDef.TableDefinitions, _datafileDef.Is64Bit);
            writer.EndBlock();
        }

        private void WriteSequenceDefinition(CodeWriter writer, SequenceDefinition sequenceDef)
        {
            var type = sequenceDef.Size switch
            {
                8 => "long",
                4 => "int",
                2 => "short",
                1 => "byte",
                _ => "int"
            };

            writer.WriteIndentedLine($"public enum {sequenceDef.Name} : {type}");
            writer.BeginBlock();

            var id = 0;


            for (var index = 0; index < sequenceDef.Sequence.Count; index++)
            {
                var value = sequenceDef.Sequence[index];
                var originalName = sequenceDef.OriginalSequence[index];
                writer.WriteIndentedLine($"[OriginalEnumName(\"{originalName}\")] {value} = {id},");

                if (_sequenceTranslator.Translate(value, out var translatedValue))
                    writer.WriteIndentedLine($"[OriginalEnumName(\"{originalName}\")] {translatedValue} = {id},");

                id++;
            }

            writer.EndBlock();
        }

        private void WriteTableDefinition(TableDefinition tableDef, string @namespace)
        {
            using var writer = new CodeWriter(new StreamWriter(File.Open(
                _outputPath + "\\" + tableDef.Name + ".cs",
                FileMode.Create, FileAccess.Write, FileShare.Read)));

            writer.WriteIndentedLine("using System;");
            writer.WriteIndentedLine("using System.IO;");
            writer.WriteIndentedLine("using BnsBinTool.Core.Helpers;");
            writer.WriteIndentedLine("using BnsBinTool.Core.DataStructs;");
            writer.WriteIndentedLine("using BnsBinTool.Core.Abstractions;");
            writer.WriteIndentedLine();
            writer.WriteIndentedLine($"namespace {@namespace}");
            writer.BeginBlock();
            WriteTable(writer, tableDef, tableDef.Name);
            writer.EndBlock();
        }

        private void WriteTable(CodeWriter writer, TableDefinition tableDef, string name, string extends = "")
        {
            writer.WriteIndent();
            writer.Write($"public partial class {name} : {nameof(ISerializableRecord)}");

            if (tableDef.Attributes.Any(x => x.OriginalName == "alias"))
                extends += ", IHaveAlias";

            writer.Write($"{extends}");
            writer.WriteLine();
            writer.BeginBlock();
            {
                writer.WriteIndentedLine($"public short {nameof(IRecord.TableType)} => {tableDef.Type};");
                writer.WriteIndentedLine($"public static short {nameof(IRecord.TableType)}Static => {tableDef.Type};");
                WriteAttributes(writer, tableDef.Attributes);

                foreach (var subtableDef in tableDef.Subtables)
                {
                    writer.WriteIndentedLine();
                    WriteSubtable(writer, tableDef, subtableDef, subtableDef.Name, tableDef.Name);
                }

                foreach (var sequenceDef in tableDef.Attributes
                    .Select(x => x.SequenceDef)
                    .Where(x => x != null))
                {
                    WriteSequenceDefinition(writer, sequenceDef);
                }

                WriteGetRef(writer, tableDef.Attributes);
                _definitionSerializerGenerator.GenerateSerializeMethod(writer, tableDef);
                _definitionDeserializerGenerator.GenerateDeserializeMethod(writer, tableDef);
                _definitionResolverGenerator.GenerateResolveMethod(writer, tableDef, tableDef);
            }
            writer.EndBlock();
        }

        private void WriteSubtable(CodeWriter writer, TableDefinition baseDef, ITableDefinition tableDef, string name, string baseName, string extends = "")
        {
            writer.WriteIndent();
            writer.Write($"public partial class {name}");

            if (tableDef.Attributes.Any(x => x.OriginalName == "alias"))
                extends += ", IHaveAlias";

            writer.Write($" : {baseName}{extends}");

            writer.WriteLine();
            writer.BeginBlock();
            {
                WriteAttributes(writer, tableDef.Attributes);

                foreach (var sequenceDef in tableDef.Attributes
                    .Select(x => x.SequenceDef)
                    .Where(x => x != null))
                {
                    WriteSequenceDefinition(writer, sequenceDef);
                }

                _definitionSerializerGenerator.GenerateSerializeMethod(writer, tableDef);
                _definitionDeserializerGenerator.GenerateDeserializeMethod(writer, tableDef);
                _definitionResolverGenerator.GenerateResolveMethod(writer, baseDef, tableDef);
            }
            writer.EndBlock();
        }

        private void WriteGetRef(CodeWriter writer, IEnumerable<AttributeDefinition> attrDefs)
        {
            var array = attrDefs.Where(x => x.Offset >= 8 && x.Offset < 16).ToArray();
            var lastIndex = array.Length - 1;

            if (array.Length == 0)
            {
                writer.WriteIndentedLine($"public {nameof(Ref)} Ref {{ get; set; }} = new {nameof(Ref)}(0, 0);");
                return;
            }

            if (array.Any(x => x.Type == AttributeType.TBool))
            {
                writer.WriteIndentedLine("private byte BoolToByte(bool b) => System.Runtime.CompilerServices.Unsafe.As<bool, byte>(ref b);");
                writer.WriteIndentedLine("private bool ByteToBool(byte b) => System.Runtime.CompilerServices.Unsafe.As<byte, bool>(ref b);");
            }

            writer.WriteIndentedLine($"public {nameof(Ref)} Ref");
            writer.BeginBlock();
            {
                // Getter
                writer.WriteIndent();
                writer.Write($"get => {nameof(Ref)}.From(");

                for (var i = 0; i < array.Length; i++)
                {
                    var attrDef = array[i];

                    var bits = (attrDef.Offset - 8) * 8;

                    if (attrDef.Type == AttributeType.TBool)
                    {
                        if (bits > 0)
                            writer.Write($"(((long)BoolToByte({attrDef.Name})) << {bits})");
                        else
                            writer.Write($"((long)BoolToByte({attrDef.Name}))");
                    }
                    else
                    {
                        if (bits > 0)
                            writer.Write($"(((long){attrDef.Name}) << {bits})");
                        else
                            writer.Write($"((long){attrDef.Name})");
                    }

                    if (i != lastIndex)
                        writer.Write(" | ");
                }

                writer.WriteLine(");");

                // Setter
                writer.WriteIndentedLine("set");
                writer.BeginBlock();
                {
                    writer.WriteIndentedLine("var l = (long) value;");

                    foreach (var attrDef in array)
                    {
                        var offset = attrDef.Offset - 8;
                        var bits = offset * 8;
                        var mask = new string('F', attrDef.Size * 2);


                        if (attrDef.Size == 8)
                        {
                            if (attrDef.Type == AttributeType.TInt64)
                                writer.WriteIndentedLine($"{attrDef.Name} = l;");
                            else
                                writer.WriteIndentedLine($"{attrDef.Name} = ({ToCSharpType(attrDef)}) l;");
                        }
                        else if (attrDef.Type == AttributeType.TBool)
                        {
                            if (bits > 0)
                                writer.WriteIndentedLine($"{attrDef.Name} = ByteToBool((byte) (l >> {bits} & 0x{mask}));");
                            else
                                writer.WriteIndentedLine($"{attrDef.Name} = ByteToBool((byte) (l & 0x{mask}));");
                        }
                        else
                        {
                            if (bits > 0)
                                writer.WriteIndentedLine($"{attrDef.Name} = ({ToCSharpType(attrDef)}) (l >> {bits} & 0x{mask});");
                            else
                                writer.WriteIndentedLine($"{attrDef.Name} = ({ToCSharpType(attrDef)}) (l & 0x{mask});");
                        }
                    }
                }
                writer.EndBlock();
            }
            writer.EndBlock();
        }

        private void WriteAttributes(CodeWriter writer, List<AttributeDefinition> attributeDefs)
        {
            foreach (var attributeDef in attributeDefs)
            {
                WriteAttribute(writer, attributeDef);
            }
        }

        private void WriteAttribute(CodeWriter writer, AttributeDefinition attrDef)
        {
            var array = "";
            var arrayConstructor = "";

            var typeName = ToCSharpType(attrDef);

            if (attrDef.Repeat > 1)
            {
                array = "[]";
                arrayConstructor = $" = new {typeName}[{attrDef.Repeat}];";
            }

            WriteResolvingHelper(writer, attrDef);

            writer.WriteIndentedLine($"public {typeName}{array} {attrDef.Name} {{ get; set; }}{arrayConstructor}");

            if (attrDef.Type == AttributeType.TIcon)
            {
                if (attrDef.Repeat > 1)
                    writer.WriteIndentedLine($"public int[] {attrDef.Name}_IconIndex {{ get; set; }} = new int[{attrDef.Repeat}];");
                else
                    writer.WriteIndentedLine($"public int {attrDef.Name}_IconIndex {{ get; set; }}");
            }
        }

        private static void WriteResolvingHelper(CodeWriter writer, AttributeDefinition attrDef)
        {
            var type = attrDef.Type switch
            {
                AttributeType.TRef => nameof(Ref),
                AttributeType.TTRef => nameof(TRef),
                AttributeType.TIcon => nameof(IconRef),
                _ => null
            };

            if (type == null)
                return;

            if (attrDef.Repeat > 1)
                writer.WriteIndentedLine($"private readonly {type}[] _{char.ToLower(attrDef.Name[0])}{attrDef.Name[1..]} = new {type}[{attrDef.Repeat}];");
            else
                writer.WriteIndentedLine($"private {type} _{char.ToLower(attrDef.Name[0])}{attrDef.Name[1..]};");
        }

        private string ToCSharpType(AttributeDefinition attrDef)
        {
            switch (attrDef.Type)
            {
                case AttributeType.TInt8:
                    return "byte";
                case AttributeType.TInt16:
                    return "short";
                case AttributeType.TInt32:
                    return "int";
                case AttributeType.TInt64:
                    return "long";
                case AttributeType.TFloat32:
                    return "float";
                case AttributeType.TBool:
                    return "bool";
                case AttributeType.TString:
                    return "string";
                case AttributeType.TSeq:
                case AttributeType.TSeq16:
                case AttributeType.TProp_seq:
                case AttributeType.TProp_field:
                    return attrDef.SequenceDef.Name;
                case AttributeType.TRef:
                    return _tables[attrDef.ReferedTable].Name;
                case AttributeType.TTRef:
                    return nameof(IRecord);
                case AttributeType.TSub:
                    return "short";
                case AttributeType.TVector32:
                    return nameof(Vector32);
                case AttributeType.TIColor:
                    return nameof(IColor);
                case AttributeType.TBox:
                    return nameof(Box);
                case AttributeType.TMsec:
                    return "int";
                case AttributeType.TDistance:
                    return "short";
                case AttributeType.TVelocity:
                    return "short";
                case AttributeType.TScript_obj:
                    return "object";
                case AttributeType.TNative:
                    return "string";
                case AttributeType.TIcon:
                    return _tables[_datafileDef.IconTextureTableId].Name;
                case AttributeType.TTime32:
                    return "DateTime";
                case AttributeType.TTime64:
                    return "DateTime";
                case AttributeType.TNone when attrDef.Size == 4:
                    return "int";
                case AttributeType.TNone when attrDef.Size == 8:
                case AttributeType.TXUnknown1 when attrDef.Size == 8:
                case AttributeType.TXUnknown2 when attrDef.Size == 8:
                    return "long";
                default:
                    ThrowHelper.ThrowOutOfRangeException(nameof(attrDef.Type));
                    break;
            }

            return "int";
        }
    }
}