using System;
using System.Collections.Generic;
using System.Linq;
using BnsBinTool.Core.Abstractions;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Models;
using BnsBinTool.Core.Serialization;

namespace BnsBinTool.DefsToSharp
{
    public class DefinitionTablesClassGenerator
    {
        public bool EnableAliasTable { get; init; }
        public bool EnableGetResolvedAliases { get; init; }

        public void GenerateTablesClass(CodeWriter writer, ICollection<TableDefinition> tableDefs, bool is64Bit)
        {
            writer.WriteIndentedLine("public class Tables");
            writer.BeginBlock();

            writer.WriteIndentedLine($"private readonly {nameof(RecordCompressedWriter)} _recordCompressedWriter = new();");
            writer.WriteIndentedLine($"private readonly {nameof(RecordUncompressedWriter)} _recordUncompressedWriter = new();");
            writer.WriteIndentedLine("private readonly MemoryStream _lookupBuffer = new();");
            writer.WriteIndentedLine("private readonly StreamWriter _lookupBufferWriter;");
            writer.WriteIndentedLine($"public byte[] TableFileIndex = new byte[{tableDefs.Count}];");
            writer.WriteIndentedLine($"public bool[] TableCompressed = new bool[{tableDefs.Count}];");
            writer.WriteIndentedLine($"public sbyte[] TableRecordOffset = new sbyte[{tableDefs.Count}];");
            writer.WriteIndentedLine($"public byte[][] TablePadding = new byte[{tableDefs.Count}][];");
            writer.WriteIndentedLine($"public TableHeader[] TableHeader = new TableHeader[{tableDefs.Count}];");
            writer.WriteIndentedLine($"public {nameof(DatafileHeader)}[] DatafileHeader;");
            writer.WriteIndentedLine($"public {nameof(NameTable)}[] NameTable;");
            writer.WriteIndentedLine($"public bool Is64Bit = {(is64Bit ? "true" : "false")};");
            writer.WriteIndentedLine("public List<short> TablesToRead = new List<short>();");

            writer.WriteIndentedLine("public Tables()");
            writer.BeginBlock();
            writer.WriteIndentedLine("_lookupBufferWriter = new StreamWriter(_lookupBuffer, Encoding.Unicode);");
            writer.EndBlock();

            foreach (var tableDef in tableDefs)
            {
                if (EnableAliasTable && tableDef.Attributes.Any(x => x.Name.Equals("alias", StringComparison.OrdinalIgnoreCase)))
                {
                    writer.WriteIndentedLine($"public AliasTable<{tableDef.Name}> {tableDef.Name} = new();");
                }
                else
                {
                    writer.WriteIndentedLine($"public SortedDictionary<{nameof(Ref)}, {tableDef.Name}> {tableDef.Name} = new({nameof(RefComparer)}.Instance);");
                }
            }

            writer.WriteIndentedLine();
            WriteToResolvedAliases(writer, tableDefs);
            WriteSaveMethod(writer, tableDefs);
            WriteSaveTable(writer);
            WriteLoadMethod(writer, tableDefs);

            foreach (var tableDef in tableDefs)
            {
                WriteReadTable(writer, tableDef);
            }

            WriteResolveMethod(writer, tableDefs);
            WriteResolveTRefMethod(writer, tableDefs);
            WriteRebuildAliasMap(writer, tableDefs);

            writer.EndBlock();
        }

        private static void WriteSaveMethod(CodeWriter writer, ICollection<TableDefinition> tableDefs)
        {
            var maximumRecordSize = Math.Max(
                tableDefs.Max(x => x.Size),
                tableDefs.SelectMany(x => x.Subtables).Max(x => x.Size));

            writer.WriteIndentedLine("public unsafe void Save(params string[] paths)");
            writer.BeginBlock();
            {
                writer.WriteIndentedLine($"var recordBuffer = stackalloc byte[{maximumRecordSize}];");
                writer.WriteIndentedLine($"System.Runtime.CompilerServices.Unsafe.InitBlock(recordBuffer, 0, {maximumRecordSize});");

                writer.WriteIndentedLine("for (var pathId = 0; pathId < paths.Length; pathId++)");
                writer.BeginBlock();
                {
                    writer.WriteIndentedLine("using var writer = new BinaryWriter(File.Open(paths[pathId], FileMode.Create, FileAccess.Write, FileShare.None));");

                    // Write name table
                    writer.WriteIndentedLine("if (NameTable[pathId] != null)");
                    writer.BeginBlock();
                    {
                        // Write header
                        writer.WriteIndentedLine($"var overwriteNametableSize = DatafileHeader[pathId].WriteHeaderTo(writer, DatafileHeader[pathId].{nameof(DatafileHeader.ReadTableCount)}, aliasCount:NameTable[0].Entries.Count, is64Bit:Is64Bit);");
                        writer.WriteIndentedLine("var oldPosition = writer.BaseStream.Position;");
                        writer.WriteIndentedLine($"new {nameof(NameTableWriter)}().WriteTo(writer, NameTable[pathId], is64Bit:Is64Bit);");
                        writer.WriteIndentedLine("overwriteNametableSize(writer.BaseStream.Position - oldPosition);");
                    }
                    writer.EndBlock();
                    writer.WriteIndentedLine("else");
                    writer.BeginBlock();
                    {
                        // Write header
                        writer.WriteIndentedLine($"_ = DatafileHeader[pathId].WriteHeaderTo(writer, DatafileHeader[pathId].{nameof(DatafileHeader.ReadTableCount)}, aliasCount:DatafileHeader[pathId].{nameof(DatafileHeader.AliasCount)}, is64Bit:Is64Bit);");
                    }
                    writer.EndBlock();

                    // Write all tables
                    foreach (var tableDef in tableDefs)
                    {
                        writer.WriteIndentedLine($"if (TableFileIndex[{tableDef.Type - 1}] == pathId)");
                        writer.Indent();
                        writer.WriteIndentedLine($"SaveTable(writer, recordBuffer, {tableDef.Type - 1}, {tableDef.Name}.Values, {tableDef.Name}.Count);");
                        writer.Unindent();
                    }
                }
                writer.EndBlock();
            }
            writer.EndBlock();
        }

        private static void WriteSaveTable(CodeWriter writer)
        {
            writer.WriteIndentedLine("private unsafe void SaveTable<T>(BinaryWriter writer, byte* recordBuffer, ushort tableIndex, IEnumerable<T> records, int recordCount) where T : ISerializableRecord");
            writer.BeginBlock();
            {
                writer.WriteIndentedLine("if (TableHeader[tableIndex] == null)");
                writer.WriteIndentedLine("    return;");

                writer.WriteIndentedLine($"TableHeader[tableIndex].{nameof(TableHeader.WriteHeaderTo)}(writer);");
                writer.WriteIndentedLine("if (TableCompressed[tableIndex])");
                writer.BeginBlock();
                {
                    writer.WriteIndentedLine("_recordCompressedWriter.BeginWrite(writer);");

                    writer.WriteIndentedLine("foreach (var record in records)");
                    writer.BeginBlock();
                    writer.WriteIndentedLine("_lookupBuffer.SetLength(0);");
                    writer.WriteIndentedLine("var size = record.Serialize(recordBuffer, _lookupBufferWriter);");
                    writer.WriteIndentedLine("_lookupBufferWriter.Flush();");
                    writer.WriteIndentedLine("_recordCompressedWriter.WriteRecord(writer,");
                    writer.WriteIndentedLine("    new ReadOnlySpan<byte>(recordBuffer, size),");
                    writer.WriteIndentedLine("    new ReadOnlySpan<byte>(_lookupBuffer.GetBuffer()[..(int)_lookupBuffer.Length]));");
                    writer.WriteIndentedLine("System.Runtime.CompilerServices.Unsafe.InitBlock(recordBuffer, 0, size);");
                    writer.EndBlock();

                    writer.WriteIndentedLine("_recordCompressedWriter.EndWrite(writer);");
                }
                writer.EndBlock();
                writer.WriteIndentedLine("else");
                writer.BeginBlock();
                {
                    writer.WriteIndentedLine("_lookupBuffer.SetLength(0);");
                    writer.WriteIndentedLine("_recordUncompressedWriter.BeginWrite(writer, is64Bit:Is64Bit && TableHeader[tableIndex].ElementCount == 1);");
                    writer.WriteIndentedLine("_recordUncompressedWriter.SetRecordCountOffset(TableRecordOffset[tableIndex]);");
                    writer.WriteIndentedLine("foreach (var record in records)");
                    writer.BeginBlock();
                    {
                        writer.WriteIndentedLine("var size = record.Serialize(recordBuffer, _lookupBufferWriter);");
                        writer.WriteIndentedLine("_recordUncompressedWriter.WriteRecord(writer, new ReadOnlySpan<byte>(recordBuffer, size));");
                        writer.WriteIndentedLine("System.Runtime.CompilerServices.Unsafe.InitBlock(recordBuffer, 0, size);");
                    }
                    writer.EndBlock();
                    writer.WriteIndentedLine("_lookupBufferWriter.Flush();");
                    writer.WriteIndentedLine("_recordUncompressedWriter.EndWrite(writer, TablePadding[tableIndex], _lookupBuffer);");
                }
                writer.EndBlock();
            }
            writer.EndBlock();
        }

        private static void WriteLoadMethod(CodeWriter writer, ICollection<TableDefinition> tableDefs)
        {
            writer.WriteIndentedLine("public void Load(params string[] paths)");
            writer.BeginBlock();
            {
                writer.WriteIndentedLine($"var compressedReader = new {nameof(RecordCompressedReader)}();");
                writer.WriteIndentedLine($"var uncompressedReader = new {nameof(RecordUncompressedReader)}();");
                writer.WriteIndentedLine($"DatafileHeader = new {nameof(DatafileHeader)}[paths.Length];");
                writer.WriteIndentedLine($"NameTable = new {nameof(NameTable)}[paths.Length];");

                writer.WriteIndentedLine("for (var pathId = 0; pathId < paths.Length; pathId++)");
                writer.BeginBlock();
                {
                    writer.WriteIndentedLine("using var reader = new BinaryReader(File.Open(paths[pathId], FileMode.Open, FileAccess.Read, FileShare.Read));");
                    writer.WriteIndentedLine($"var header = new {nameof(DatafileHeader)}();");
                    writer.WriteIndentedLine("header.ReadHeaderFrom(reader, is64Bit:Is64Bit);");
                    writer.WriteIndentedLine("DatafileHeader[pathId] = header;");

                    writer.WriteIndentedLine("if (header.ReadTableCount > 10)");
                    writer.BeginBlock();
                    {
                        writer.WriteIndentedLine($"var nameTableReader = new {nameof(NameTableReader)}(is64Bit:Is64Bit) {{LazyLoadSource = new FileSource(paths[pathId])}};");
                        writer.WriteIndentedLine("NameTable[pathId] = nameTableReader.ReadFrom(reader);");
                    }
                    writer.EndBlock();

                    writer.WriteIndentedLine("for (var i = 0; i < header.ReadTableCount; i++)");
                    writer.BeginBlock();
                    {
                        writer.WriteIndentedLine($"var tableHeader = new {nameof(TableHeader)}();");
                        writer.WriteIndentedLine("tableHeader.ReadHeaderFrom(reader, is64Bit:Is64Bit);");
                        writer.WriteIndentedLine("TableHeader[tableHeader.Type - 1] = tableHeader;");
                        writer.WriteIndentedLine("TableFileIndex[tableHeader.Type - 1] = (byte) pathId;");

                        writer.WriteIndentedLine("if (TablesToRead.Count > 0 && !TablesToRead.Contains(tableHeader.Type))");
                        writer.BeginBlock();
                        {
                            writer.WriteIndentedLine("reader.BaseStream.Seek(tableHeader.Size - 1, SeekOrigin.Current);");
                            writer.WriteIndentedLine("continue;");
                        }
                        writer.EndBlock();

                        WriteDecideCompressed(writer, tableDefs);
                    }
                    writer.EndBlock();
                }
                writer.EndBlock();
            }
            writer.EndBlock();
        }

        private static void WriteDecideCompressed(CodeWriter writer, ICollection<TableDefinition> tableDefs)
        {
            writer.WriteIndentedLine($"var recordReader = tableHeader.IsCompressed ? ({nameof(IRecordReader)})compressedReader : uncompressedReader;");
            writer.WriteIndentedLine("if (!recordReader.Initialize(reader, is64Bit:Is64Bit && !tableHeader.IsCompressed && tableHeader.ElementCount == 1))");
            writer.Indent();
            writer.WriteIndentedLine("throw new Exception(\"Failed to initialize record reader\");");
            writer.Unindent();

            writer.WriteIndentedLine("switch (tableHeader.Type)");
            writer.BeginBlock();
            {
                foreach (var tableDef in tableDefs)
                {
                    writer.BeginCase($"{tableDef.Type}");
                    writer.WriteIndentedLine($"Load{tableDef.Name}(reader, recordReader);");
                    writer.EndCase();
                }
            }
            writer.EndBlock();

            writer.WriteIndentedLine("if (!tableHeader.IsCompressed)");
            writer.BeginBlock();
            writer.WriteIndentedLine($"TableRecordOffset[tableHeader.Type - 1] = (sbyte) uncompressedReader.{nameof(RecordUncompressedReader.GetRecordCountOffset)}();");
            writer.WriteIndentedLine($"uncompressedReader.{nameof(RecordUncompressedReader.GetPadding)}(out var padding);");
            writer.WriteIndentedLine("if (padding.Length > 0)");
            writer.Indent();
            writer.WriteIndentedLine("TablePadding[tableHeader.Type - 1] = padding.ToArray();");
            writer.Unindent();
            writer.EndBlock();
            writer.WriteIndentedLine("else");
            writer.Indent();
            writer.WriteIndentedLine("TableCompressed[tableHeader.Type - 1] = true;");
            writer.Unindent();
        }

        private static void WriteReadTable(CodeWriter writer, TableDefinition tableDef)
        {
            writer.WriteIndentedLine($"private unsafe void Load{tableDef.Name}(BinaryReader reader, {nameof(IRecordReader)} recordReader)");
            writer.BeginBlock();
            {
                writer.WriteIndentedLine($"var recordMemory = new {nameof(RecordMemory)}();");
                writer.WriteIndentedLine("while (recordReader.Read(reader, ref recordMemory))");
                writer.BeginBlock();
                {
                    if (tableDef.Subtables.Count == 0)
                    {
                        writer.WriteIndentedLine($"var record = new {tableDef.Name}();");
                        writer.WriteIndentedLine("record.Deserialize(recordMemory.DataBegin, recordMemory.StringBufferBegin);");
                        writer.WriteIndentedLine($"{tableDef.Name}[record.Ref] = record;");
                    }
                    else
                    {
                        var ifelse = "if";
                        for (var subclassId = 0; subclassId < tableDef.Subtables.Count; subclassId++)
                        {
                            writer.WriteIndentedLine($"{ifelse} (recordMemory.Type == {subclassId})");
                            writer.BeginBlock();
                            {
                                writer.WriteIndentedLine($"var record = new {tableDef.Name}.{tableDef.Subtables[subclassId].Name}();");
                                writer.WriteIndentedLine("record.Deserialize(recordMemory.DataBegin, recordMemory.StringBufferBegin);");
                                writer.WriteIndentedLine($"{tableDef.Name}[record.Ref] = record;");
                            }
                            writer.EndBlock();

                            ifelse = "else if";
                        }

                        writer.WriteIndentedLine("else if (recordMemory.Type == -1)");
                        writer.BeginBlock();
                        {
                            writer.WriteIndentedLine($"Console.WriteLine($\"Invalid record {{recordMemory.Type}} subtype in {tableDef.Name}\");");
                        }
                        writer.EndBlock();
                    }
                }
                writer.EndBlock();
            }
            writer.EndBlock();
        }

        private static void WriteResolveMethod(CodeWriter writer, ICollection<TableDefinition> tableDefs)
        {
            writer.WriteIndentedLine("public void Resolve()");
            writer.BeginBlock();

            foreach (var tableDef in tableDefs)
            {
                writer.WriteIndentedLine($"foreach (var record in {tableDef.Name}.Values)");
                {
                    writer.Indent();

                    writer.WriteIndentedLine("record.Resolve(this);");

                    writer.Unindent();
                }
            }

            writer.EndBlock();
        }

        private static void WriteResolveTRefMethod(CodeWriter writer, ICollection<TableDefinition> tableDefs)
        {
            writer.WriteIndentedLine($"public {nameof(IRecord)} ResolveTRef({nameof(TRef)} tref)");
            writer.BeginBlock();
            {
                writer.WriteIndentedLine($"if (tref.Table == 0 || tref == new {nameof(Ref)}(0, 0))");
                writer.Indent();
                writer.WriteIndentedLine("return null;");
                writer.Unindent();

                writer.WriteIndentedLine($"switch (tref.{nameof(TRef.Table)})");
                writer.BeginBlock();
                {
                    foreach (var tableDef in tableDefs)
                    {
                        var recordVar = char.ToLower(tableDef.Name[0]) + tableDef.Name[1..];
                        writer.BeginCase(tableDef.Type.ToString());

                        writer.WriteIndentedLine($"if ({tableDef.Name}.TryGetValue(tref, out var {recordVar}))");
                        writer.Indent();
                        writer.WriteIndentedLine($"return {recordVar};");
                        writer.Unindent();
                        writer.WriteIndentedLine("else");
                        writer.Indent();
                        writer.WriteIndentedLine($"return new {nameof(FakeRecord)}(tref);");
                        writer.Unindent();

                        writer.Unindent();
                    }
                }
                writer.EndBlock();

                writer.WriteIndentedLine("return null;");
            }
            writer.EndBlock();
        }

        private void WriteToResolvedAliases(CodeWriter writer, ICollection<TableDefinition> tableDefs)
        {
            writer.WriteIndentedLine($"public {nameof(ResolvedAliases)} GetResolvedAliases()");

            if (!EnableGetResolvedAliases)
            {
                writer.BeginBlock();
                writer.WriteIndentedLine("return default;");
                writer.EndBlock();
                return;
            }

            writer.BeginBlock();
            {
                writer.WriteIndentedLine($"var resolvedAliases = new {nameof(ResolvedAliases)}();");

                writer.WriteIndentedLine("Dictionary<Ref, string> byRef;");
                writer.WriteIndentedLine("Dictionary<string, Ref> byAlias;");

                writer.WriteIndentedLine($"for (var i = 0; i < {tableDefs.Count}; i++)");
                writer.BeginBlock();
                {
                    writer.WriteIndentedLine("resolvedAliases.ByAlias[i] = new Dictionary<string, Ref>();");
                    writer.WriteIndentedLine("resolvedAliases.ByRef[i] = new Dictionary<Ref, string>();");
                }
                writer.EndBlock();

                foreach (var tableDef in tableDefs)
                {
                    if (tableDef.Attributes.All(x => x.OriginalName != "alias"))
                        continue;

                    writer.WriteIndentedLine($"byAlias = resolvedAliases.ByAlias[{tableDef.Type - 1}];");
                    writer.WriteIndentedLine($"byRef = resolvedAliases.ByRef[{tableDef.Type - 1}];");
                    writer.WriteIndentedLine($"foreach (var (key, record) in {tableDef.Name})");
                    writer.BeginBlock();
                    {
                        writer.WriteIndentedLine("byAlias[record.Alias] = key;");
                        writer.WriteIndentedLine("byRef[key] = record.Alias;");
                    }
                    writer.EndBlock();
                }

                writer.WriteIndentedLine("return resolvedAliases;");
            }
            writer.EndBlock();
        }

        private void WriteRebuildAliasMap(CodeWriter writer, ICollection<TableDefinition> tableDefs)
        {
            writer.WriteIndentedLine("public void RebuildAliasMap(int datafileIndex)");
            writer.BeginBlock();
            {
                writer.WriteIndentedLine("NameTable[datafileIndex]");
                writer.Indent();
                writer.WriteIndentedLine(".BeginRebuilding()");

                foreach (var tableDef in tableDefs.Where(x => x.Attributes.Any(x => x.OriginalName == "alias")))
                {
                    writer.WriteIndentedLine($".AddTable(\"{tableDef.OriginalName}\", {tableDef.Name}.Values)");
                }

                writer.WriteIndentedLine(".EndRebuilding();");
                writer.Unindent();
            }
            writer.EndBlock();
        }
    }
}