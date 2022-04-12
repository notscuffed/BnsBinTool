using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BnsBinTool.Core;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;
using BnsBinTool.Xml.AliasResolvers;
using K4os.Hash.xxHash;

namespace BnsBinTool.Xml.Converters
{
    public class DatafileToXmlConverter
    {
        private readonly DatafileDefinition _datafileDef;
        private readonly string _extractedXmlDatPath;
        private readonly string _extractedLocalDatPath;
        private readonly Dictionary<string, string> _fileHashes = new Dictionary<string, string>();
        private readonly object _fileHashesLocker = new object();
        private readonly bool _noValidate;
        private string _outputPath;
        private bool _writeDefsComment;
        private ResolvedAliases _tablesAliases;

        public DatafileToXmlConverter(DatafileDefinition datafileDef, string extractedXmlDatPath, string extractedLocalDatPath, bool noValidate)
        {
            _datafileDef = datafileDef;
            _extractedXmlDatPath = extractedXmlDatPath;
            _extractedLocalDatPath = extractedLocalDatPath;
            _noValidate = noValidate;
        }

        public void ConvertDatafilesToXml(Datafile data, Datafile local, string outputPath, string[] onlyTables = null,
            bool writeDefsComment = false)
        {
            _outputPath = Path.GetFullPath(outputPath);
            _writeDefsComment = writeDefsComment;

            Directory.CreateDirectory(_outputPath + "\\tables");

            data.WriteToFile(_outputPath + "\\datafile.bin");
            local.WriteToFile(_outputPath + "\\localfile.bin");

            var tables = data.Tables.Concat(local.Tables).ToArray();

            _tablesAliases = DatafileAliasResolver.Resolve(tables, _datafileDef);

            ExtractedXmlDatAliasResolver.Resolve(_tablesAliases, _datafileDef, _extractedXmlDatPath, _extractedLocalDatPath);

            if (onlyTables != null)
            {
                tables = tables
                    .Where(x => onlyTables.Contains(_datafileDef[x.Type].OriginalName, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            Parallel.ForEach(tables, ProcessTable);

            SaveHashes();
        }

        private void SaveHashes()
        {
            lock (_fileHashesLocker)
            {
                File.WriteAllLines(_outputPath + "\\hashes.txt",
                    _fileHashes.Select(x => $"{x.Key}={x.Value}"));
            }
        }

        private void ProcessTable(Table table)
        {
            var tableDef = _datafileDef[table.Type];

            if (tableDef.IsEmpty)
                return;

            var typeDictionary = new Dictionary<short, List<Record>>();

            // Split table by subtypes
            foreach (var record in table.Records)
            {
                if (!typeDictionary.TryGetValue(record.SubclassType, out var typeRecords))
                {
                    typeRecords = new List<Record>();
                    typeDictionary[record.SubclassType] = typeRecords;
                }

                typeRecords.Add(record);
            }

            var hasMany = typeDictionary.Count > 1;

            foreach (var (type, records) in typeDictionary)
            {
                var memory = new MemoryStream();

                using var writer = new XmlTextWriter(memory, Encoding.Unicode)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4
                };

                ITableDefinition subtableDef;

                if (_noValidate && type >= tableDef.Subtables.Count)
                    continue;

                if (type == -1)
                    subtableDef = tableDef;
                else
                    subtableDef = tableDef.Subtables[type];

                ConvertSubTable(writer, records, tableDef, subtableDef);

                writer.Flush();

                Span<byte> data = memory.GetBuffer()[..(int) memory.Length];

                var name = tableDef.Name;

                if (hasMany)
                {
                    Directory.CreateDirectory($"{_outputPath}\\tables\\{name}");
                    name = $"{name}\\{(type >= 0 ? tableDef.Subtables[type].Name : $"base_{name}")}";
                }

                using (var fstream = File.Open(
                           _outputPath + $"\\tables\\{name}.xml",
                           FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    fstream.Write(data);
                    fstream.Close();
                }

                lock (_fileHashesLocker)
                    _fileHashes[name] = XXH64.DigestOf(data).ToString();
            }
        }

        private void ConvertSubTable(
            XmlTextWriter writer,
            List<Record> records,
            TableDefinition tableDef,
            ITableDefinition subtableDef)
        {
            writer.WriteStartDocument();
            if (_writeDefsComment)
                WriteDefsComment(writer, tableDef, subtableDef);
            writer.WriteStartElement("table");

            if (subtableDef.SubclassType >= 0)
            {
                foreach (var record in records)
                {
                    writer.WriteStartElement("record");
                    writer.WriteAttributeString("type", subtableDef.Name);
                    ConvertRecord(writer, record, subtableDef.ExpandedAttributes);
                    writer.WriteEndElement();
                }
            }
            else
            {
                foreach (var record in records)
                {
                    writer.WriteStartElement("record");
                    ConvertRecord(writer, record, subtableDef.ExpandedAttributes);
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private void ConvertRecord(XmlTextWriter writer, Record record, List<AttributeDefinition> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (_noValidate && attribute.Offset >= record.DataSize)
                    continue;
                
                string value;

                switch (attribute.Type)
                {
                    case AttributeType.TRef:
                        var r = record.Get<Ref>(attribute.Offset);

                        if (r == attribute.AttributeDefaultValues.DRef)
                            continue;

                        if (!_tablesAliases.ByRef.TryGetValue(attribute.ReferedTable, out var tableAliases)
                            || !tableAliases.TryGetValue(r, out value)
                            || string.IsNullOrWhiteSpace(value))
                            value = $"{r.Id}:{r.Variant}";

                        break;

                    case AttributeType.TIcon:
                        var ir = record.Get<IconRef>(attribute.Offset);

                        if (ir == attribute.AttributeDefaultValues.DIconRef)
                            continue;

                        if (!_tablesAliases.ByRef.TryGetValue(_datafileDef.IconTextureTableId, out tableAliases)
                            || !tableAliases.TryGetValue(ir, out value))
                            value = $"{ir.IconTextureRecordId}:{ir.IconTextureVariantId}:{ir._unk_i32_0}";
                        else
                            value += $":{ir._unk_i32_0}";

                        break;

                    case AttributeType.TTRef:
                        var tr = record.Get<TRef>(attribute.Offset);

                        if (tr == attribute.AttributeDefaultValues.DTRef)
                            continue;

                        var tableName = _datafileDef[tr.Table].Name;

                        if (!_tablesAliases.ByRef.TryGetValue(tr.Table, out tableAliases)
                            || !tableAliases.TryGetValue(tr, out value))
                            value = $"{tableName}:{tr.Id}:{tr.Variant}";
                        else
                            value = $"{tableName}:" + value;

                        break;

                    case AttributeType.TNative:
                        var n = record.Get<Native>(attribute.Offset);

                        if (n == attribute.AttributeDefaultValues.DNative)
                            continue;

                        value = record.StringLookup.GetString(n.Offset);
                        break;

                    case AttributeType.TVector32:
                        var v = record.Get<Vector32>(attribute.Offset);

                        if (v == attribute.AttributeDefaultValues.DVector32)
                            continue;

                        value = $"{v.X},{v.Y},{v.Z}";
                        break;

                    case AttributeType.TVelocity:
                        var velocity = record.Get<ushort>(attribute.Offset);

                        if (velocity == attribute.AttributeDefaultValues.DVelocity)
                            continue;

                        value = velocity.ToString();
                        break;

                    case AttributeType.TDistance:
                    case AttributeType.TInt16:
                    case AttributeType.TSub:
                        var s = record.Get<short>(attribute.Offset);

                        if (s == attribute.AttributeDefaultValues.DShort)
                            continue;

                        value = s.ToString();

                        break;

                    case AttributeType.TInt64:
                    case AttributeType.TTime64:
                        // TODO: implement time
                        var l = record.Get<long>(attribute.Offset);

                        if (l == attribute.AttributeDefaultValues.DLong)
                            continue;

                        value = l.ToString();
                        break;

                    case AttributeType.TInt32:
                    case AttributeType.TMsec:
                        var integer = record.Get<int>(attribute.Offset);

                        if (integer == attribute.AttributeDefaultValues.DInt)
                            continue;

                        value = integer.ToString();
                        break;

                    case AttributeType.TInt8:
                        var b = (sbyte) record.Data[attribute.Offset];

                        if (b == attribute.AttributeDefaultValues.DByte)
                            continue;

                        value = b.ToString();
                        break;

                    case AttributeType.TFloat32:
                        var f = record.Get<float>(attribute.Offset);

                        if (Math.Abs(f - attribute.AttributeDefaultValues.DFloat) < 0.001)
                            continue;

                        value = f.ToString(CultureInfo.InvariantCulture);
                        break;

                    case AttributeType.TString:
                        var str = record.StringLookup.GetString(record.Get<int>(attribute.Offset));

                        if (str == attribute.AttributeDefaultValues.DString)
                            continue;

                        value = str;
                        break;

                    case AttributeType.TBool:
                        var bol = record.Data[attribute.Offset] == 1;

                        if (bol == attribute.AttributeDefaultValues.DBool)
                            continue;

                        value = bol ? "y" : "n";
                        break;

                    case AttributeType.TSeq:
                    case AttributeType.TProp_seq:
                    {
                        var idx = record.Get<sbyte>(attribute.Offset);
                        if (idx < attribute.Sequence.Count)
                        {
                            var seq = attribute.Sequence[idx];

                            if (seq == attribute.AttributeDefaultValues.DSeq)
                                continue;

                            value = seq;
                        }
                        else
                        {
                            value = "";
                            if (!_noValidate) ThrowHelper.ThrowException("Invalid sequence index");
                        }

                        break;
                    }

                    case AttributeType.TSeq16:
                    case AttributeType.TProp_field:
                    {
                        var idx = record.Get<short>(attribute.Offset);
                        if (idx < attribute.Sequence.Count)
                        {
                            var seq = attribute.Sequence[idx];

                            if (seq == attribute.AttributeDefaultValues.DSeq)
                                continue;

                            value = seq;
                        }
                        else
                        {
                            value = "";
                            if (!_noValidate) ThrowHelper.ThrowException("Invalid sequence index");
                        }

                        break;
                    }

                    case AttributeType.TScript_obj:
                        var scriptObjBytes = record.Data[
                            attribute.Offset..(attribute.Offset + attribute.Size)
                        ];

                        if (scriptObjBytes.All(x => x == 0))
                            continue;

                        value = Convert.ToBase64String(scriptObjBytes);
                        break;

                    case AttributeType.TIColor:
                        var c = record.Get<IColor>(attribute.Offset);

                        if (c == attribute.AttributeDefaultValues.DIColor)
                            continue;

                        value = $"{c.R},{c.G},{c.B}";
                        break;

                    case AttributeType.TBox:
                        var box = record.Get<Box>(attribute.Offset);

                        value = $"{box.X1},{box.Y1},{box.Z1},{box.X2},{box.Y2},{box.Z2}";
                        break;

                    case AttributeType.TXUnknown1:
                    case AttributeType.TXUnknown2:
                        l = record.Get<long>(attribute.Offset);

                        if (l == attribute.AttributeDefaultValues.DLong)
                            continue;

                        value = l.ToString();
                        break;

                    case AttributeType.TNone:
                        value = "";
                        break;

                    default:
                        ThrowHelper.ThrowException($"Unhandled type name: '{attribute.TypeName}'");
                        value = null;
                        break;
                }

                writer.WriteAttributeString(attribute.Name, value);
            }
        }

        private void WriteDefsComment(XmlTextWriter writer, ITableDefinition mainTableDef, ITableDefinition subtableDef)
        {
            // Write comment with the table definition
            var cmt = new StringBuilder();

            cmt.AppendLine("");
            cmt.AppendLine($"# Table {mainTableDef.Name} definition");

            foreach (var tableDef in new[] {mainTableDef, subtableDef})
            {
                foreach (var attrDef in tableDef.Attributes)
                {
                    var typeName = attrDef.OriginalTypeName;
                    if (attrDef.ReferedTable != 0)
                    {
                        var referedTableDef = _datafileDef[attrDef.ReferedTable];
                        typeName = $"ref({referedTableDef.Name}:{referedTableDef.Type})";
                    }

                    cmt.Append($"{typeName} {attrDef.Name}{(attrDef.Repeat > 1 ? $"-(1-{attrDef.Repeat})" : "")} = \"{attrDef.DefaultValue}\"");

                    if (attrDef.Sequence.Count > 0)
                    {
                        cmt.Append($" [{string.Join(", ", attrDef.Sequence)}]");
                    }

                    cmt.AppendLine();
                }

                if (tableDef == subtableDef)
                    break;
            }

            writer.WriteComment(cmt.ToString());
        }
    }
}