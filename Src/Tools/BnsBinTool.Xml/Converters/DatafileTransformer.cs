using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using BnsBinTool.Core;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Exceptions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;
using BnsBinTool.Xml.AliasResolvers;
using BnsBinTool.Xml.Helpers;
using BnsBinTool.Xml.Models;

namespace BnsBinTool.Xml.Converters
{
    public class DatafileTransformer
    {
        // Init only - READ ONLY
        private readonly DatafileDefinition _datafileDef;
        private RecordBuilder _recordBuilder;
        private RecordBuilder _textRecordBuilder;

        // States
        private Dictionary<int, Table> _tables;
        private ResolvedAliases _resolvedAliases;
        private Table _textTable;
        private TableDefinition _textTableDef;
        private AttributeDefinition _textAttrDef;
        private AttributeDefinition _textAliasAttrDef;
        private Dictionary<int, Record> _translate;
        private readonly StringBuilder _translateStringBuilder = new StringBuilder(4096);
        private DatafileTransformerContext _context;
        private int _patchFileId;
        private int _textTableType;
        private readonly short _effectGroupTableTypeHack;

        public DatafileTransformer(DatafileDefinition datafileDef)
        {
            _datafileDef = datafileDef;
            _effectGroupTableTypeHack = datafileDef["effect-group"].Type;
        }

        public void Transform(string[] datafilePaths, string[] outputPaths, string patchesRootPath, bool is64Bit = false, string extractedXmlDatPath = null, string extractedLocalDatPath = null,
            bool rebuildAutoTables = false,
            bool importAutoIdsPatches = true)
        {
            patchesRootPath = Path.GetFullPath(patchesRootPath);

            Logger.StartTimer();

            var datafiles = datafilePaths
                .Select(x =>
                {
                    Logger.LogTime($"Loading datafile: '{x}'");
                    return Datafile.ReadFromFile(x, is64Bit: is64Bit);
                })
                .ToArray();

            // Concat tables
            _tables = datafiles
                .Select(x => x.Tables)
                .Aggregate((x, y) => x.Concat(y).ToList())
                .ToDictionary(x => (int) x.Type);

            // Init stuff
            _textTable = _tables[_datafileDef.TextTableId];
            _textTableDef = _datafileDef[_datafileDef.TextTableId];
            _textAttrDef = _textTableDef.Attributes.First(x => x.OriginalName == "text");
            _textAliasAttrDef = _textTableDef.Attributes.First(x => x.OriginalName == "alias");
            _translate = _textTable.Records.ToDictionary(x => x.RecordId);
            _textTableType = _textTableDef.Type;

            // Resolve aliases
            _resolvedAliases = DatafileAliasResolver.Resolve(_tables.Values, _datafileDef);

            ExtractedXmlDatAliasResolver.Resolve(_resolvedAliases, _datafileDef, extractedXmlDatPath, extractedLocalDatPath);

            _recordBuilder ??= new RecordBuilder(_datafileDef, _resolvedAliases);
            _textRecordBuilder ??= new RecordBuilder(_datafileDef, _resolvedAliases);

            Logger.LogTime("Resolved datafile aliases");

            _context = new DatafileTransformerContext(_tables.Values, _datafileDef);
            var patchAliasResolver = new PatchAliasResolver(_resolvedAliases, _datafileDef, _tables, _context);
            patchAliasResolver.Resolve(patchesRootPath);
            Logger.LogTime("Resolved patch aliases");

            _textRecordBuilder.InitializeTable(_textTable.IsCompressed);
            TransformFromPatches(patchesRootPath);
            _textRecordBuilder.FinalizeTable();

            Logger.LogTime("Tables transformed");

            if (rebuildAutoTables)
            {
                Logger.LogTime("Rebuilding auto tables");

                var autoTableRebuilder = new AutoTableRebuilder(_datafileDef);
                autoTableRebuilder.RebuildDatafileAutoTableIndex(_tables.Values.ToArray());
            }

            if (importAutoIdsPatches)
            {
                Logger.LogTime("Importing fixed auto id's");
                
                var autoIdPatcher = new AutoTableFixer(_datafileDef);
                autoIdPatcher.Fix(_tables.Values.ToArray(), patchesRootPath);
            }

            Logger.LogTime("Rebuilding alias map");
            var rebuilder = datafiles[0].NameTable
                .BeginRebuilding();

            foreach (var table in _tables.Values)
            {
                var tableDef = _datafileDef[table.Type];
                var aliasAttrDef = tableDef.Attributes.FirstOrDefault(x => x.Name == "alias");

                if (aliasAttrDef == null)
                    continue;

                var tablePrefix = tableDef.OriginalName.ToLowerInvariant() + ":";
                var aliasOffset = aliasAttrDef.Offset;

                foreach (var record in table.Records)
                {
                    var alias = record.StringLookup.GetString(record.Get<int>(aliasOffset));
                    rebuilder.AddAliasManually(
                        tablePrefix + alias.ToLowerInvariant(),
                        record.Get<Ref>(8));
                }
            }

            XmlRecordsHelper.LoadAliasesToRebuilder(rebuilder, extractedXmlDatPath, extractedLocalDatPath);

            rebuilder.EndRebuilding();
            Logger.LogTime("Rebuilt alias map");
            
            
            Logger.LogTime("Sorting records");
            foreach (var table in _tables.Values)
            {
                var result = table.Records.OrderBy(x => x.Get<int>(12))
                    .ThenBy(x => x.Get<int>(8)).ToArray();
                table.Records.Clear();
                table.Records.AddRange(result);
            }
            Logger.LogTime("Sorted records");

            var mainDatafile = datafiles.First(x => x.NameTable != null);

            for (var i = 0; i < datafiles.Length; i++)
            {
                Logger.LogTime($"Saving datafile: '{outputPaths[i]}'");
                datafiles[i].AliasCount = mainDatafile.AliasCount;
                datafiles[i].AliasMapSize = mainDatafile.AliasMapSize;
                datafiles[i].WriteToFile(outputPaths[i]);
            }

            Logger.LogTime("Done");
        }

        private void TransformFromPatches(string patchesRootPath)
        {
            _patchFileId = 0;
            foreach (var path in Directory.EnumerateFiles(patchesRootPath, "*.xml", SearchOption.AllDirectories))
            {
                TransformFromPatch(path, path[(patchesRootPath.Length + 1)..]);
                _patchFileId++;
            }
        }

        private void TransformFromPatch(string fullFilePath, string filePath)
        {
            if (!File.Exists(fullFilePath))
                return;

            Logger.LogTime($"Loading patch: '{filePath}'");

            using var reader = new XmlFileReader(fullFilePath, filePath);

            if (!reader.Read() || reader.NodeType != XmlNodeType.XmlDeclaration)
                reader.ThrowException("Failed to read xml declaration");

            if (!reader.Read() || reader.Name != "patch")
                reader.ThrowException("Failed to read patch element");

            try
            {
                while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "add":
                                HandleAdd(reader);
                                continue;

                            case "translate":
                                HandleTranslate(reader);
                                continue;

                            case "modify":
                                HandleModify(reader);
                                continue;

                            case "delete":
                                HandleDelete(reader);
                                continue;

                            case "replace":
                                HandleReplace(reader);
                                continue;

                            default:
                                reader.ThrowException($"Invalid patch action: '{reader.Name}'");
                                return;
                        }
                    }
                }
            }
            catch (BnsInvalidReferenceException e)
            {
                reader.ThrowException(e.Message);
            }
        }

        private void HandleAdd(XmlFileReader reader)
        {
            reader.GetInTable(_datafileDef, _tables, out var tableDef, out var table);
            InitializeRecordBuilderTable(table);

            reader.HandleSubelements("add", r =>
            {
                switch (r.Name)
                {
                    case "record":
                        AddRecord(r, tableDef, table);
                        return true;

                    case "auto-record":
                        AddRecord(r, tableDef, table, true);
                        return true;
                }

                return false;
            });

            _recordBuilder.FinalizeTable();
        }

        private void HandleTranslate(XmlFileReader reader)
        {
            reader.HandleSubelements("translate", r =>
            {
                switch (r.Name)
                {
                    case "by-alias":
                        TranslateByAlias(r);
                        return true;
                }

                return false;
            });
        }

        private void HandleModify(XmlFileReader reader)
        {
            reader.GetInTable(_datafileDef, _tables, out var tableDef, out var table);
            InitializeRecordBuilderTable(table);

            var keyAttributes = tableDef.Attributes
                .Where(x => x.IsKey && x.Offset is >= 8 and < 16)
                .ToDictionary(x => x.Name);

            reader.HandleSubelements("modify", r =>
            {
                switch (r.Name)
                {
                    case "by-alias":
                        ModifyByAlias(r, tableDef, table);
                        return true;

                    case "by-ref":
                        ModifyByRef(r, tableDef, table, keyAttributes);
                        return true;

                    default:
                        return false;
                }
            });

            _recordBuilder.FinalizeTable();
        }

        private static void HandleDelete(XmlFileReader reader)
        {
            // TODO: Deletion should happen here but it's actually hapenning in PatchAliasResolver currently
            while (reader.Read() && (reader.NodeType != XmlNodeType.EndElement || reader.Name != "delete"))
            {
            }
        }

        private void HandleReplace(XmlFileReader reader)
        {
            reader.GetInTable(_datafileDef, _tables, out var tableDef, out var table);
            InitializeRecordBuilderTable(table);

            table.Records.Clear();

            reader.HandleSubelements("replace", r =>
            {
                switch (r.Name)
                {
                    case "record":
                        AddRecord(r, tableDef, table);
                        return true;

                    default:
                        return false;
                }
            });

            _recordBuilder.FinalizeTable();
        }

        private void TranslateByAlias(XmlFileReader reader)
        {
            var alias = reader.RequireAttribute("alias");
            var text = reader.RequireAttribute("text");

            var id = _resolvedAliases.ByAlias[_textTable.Type][alias].Id;

            if (_translate.TryGetValue(id, out var record))
            {
                var builder = _translateStringBuilder;
                builder.Clear();
                builder.Append(alias);
                builder.Append('\0');
                var offset = builder.Length << 1;
                builder.Append(text);
                builder.Append('\0');
                record.Set(_textAliasAttrDef.Offset, 0);
                record.Set(_textAttrDef.Offset, new Native((text.Length << 1) + 2, offset));
                record.StringLookup.Data = Encoding.Unicode.GetBytes(builder.ToString());
            }
            else
            {
                reader.ThrowException($"Failed to find text record with alias: '{alias}' (resolved id: {id})");
            }
        }

        private void ModifyByRef(XmlFileReader reader, TableDefinition tableDef, Table table, Dictionary<string, AttributeDefinition> keyAttrs)
        {
            Span<byte> refData = stackalloc byte[8];

            if (!reader.MoveToFirstAttribute())
                reader.ThrowException("Failed to move to first attribute");

            var keyCount = 0;

            do
            {
                if (XmlHelper.ReadRefAttribute(reader, refData, keyAttrs))
                    keyCount++;
            } while (reader.MoveToNextAttribute());

            if (keyCount == 0)
                reader.ThrowException("Not a single key has been specified");

            var r = refData.Get<Ref>(0);
            var record = table.Records.FirstOrDefault(x => x.Get<Ref>(8) == r);

            if (record == null)
                reader.ThrowException($"Record with ref '{r.Id}:{r.Variant}' not found in table '{tableDef.Name}'");

            ModifyShared(reader, record, tableDef);
        }

        private void ModifyShared(XmlFileReader reader, Record record, TableDefinition tableDef)
        {
            var def = record.SubclassType == -1
                ? (ITableDefinition) tableDef
                : tableDef.Subtables[record.SubclassType];

            _recordBuilder.InitializeMutateRecord(record);

            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if (reader.Name == "alias")
                        continue;

                    var attrDef = def.ExpandedAttributeByName(reader.Name);

                    if (attrDef == null)
                        reader.ThrowException($"Invalid attribute: '{reader.Name}'");

                    if (attrDef.ReferedTable != _datafileDef.TextTableId)
                    {
                        _recordBuilder.SetAttribute(record, attrDef, reader.Value);
                    }
                    else
                    {
                        // TODO: PatchAliasResolver needs to be the one that creates Text records 
                        if (!string.IsNullOrEmpty(reader.Value) && reader.Value[0] == '`')
                        {
                            Record textRecord;

                            var referedTextRefId = record.Get<Ref>(attrDef.Offset).Id;

                            if (referedTextRefId > 0)
                            {
                                textRecord = _translate[referedTextRefId];

                                var text = reader.Value[1..];
                                var builder = _translateStringBuilder;
                                builder.Clear();
                                builder.Append(textRecord.StringLookup.GetString(textRecord.Get<int>(_textAliasAttrDef.Offset)));
                                builder.Append('\0');
                                var offset = builder.Length << 1;
                                builder.Append(text);
                                builder.Append('\0');

                                textRecord.Set(_textAliasAttrDef.Offset, 0);
                                textRecord.Set(_textAttrDef.Offset, new Native((text.Length << 1) + 2, offset));
                                textRecord.StringLookup.Data = Encoding.Unicode.GetBytes(builder.ToString());
                            }
                            else
                            {
                                _textRecordBuilder.InitializeRecord();
                                textRecord = _textTableDef.CreateDefaultRecord(_textRecordBuilder.StringLookup, out _);
                                var newTextRecordId = ++_context.LastRecordId[_textTableType];
                                textRecord.Set(8, newTextRecordId);
                                _textTable.Records.Add(textRecord);
                                _textRecordBuilder.SetAttribute(textRecord, _textAliasAttrDef, $"TXTGen.{newTextRecordId}");
                                _textRecordBuilder.SetAttribute(textRecord, _textAttrDef, reader.Value[1..]);
                                _textRecordBuilder.FinalizeRecord();

                                record.Set(attrDef.Offset, new Ref(newTextRecordId));
                            }
                        }
                        else
                        {
                            if (reader.Value.Length != 0 || attrDef.Type is not (AttributeType.TRef or AttributeType.TTRef))
                            {
                                _recordBuilder.SetAttribute(record, attrDef, reader.Value);
                            }
                            else
                            {
                                record.Set(attrDef.Offset, attrDef.Type is AttributeType.TRef ? new Ref() : new TRef());
                            }
                        }
                    }
                } while (reader.MoveToNextAttribute());
            }
            else
            {
                reader.ThrowException("Failed to move to first attribute");
            }

            _recordBuilder.FinalizeMutateRecord();
        }

        private void ModifyByAlias(XmlFileReader reader, TableDefinition tableDef, Table table)
        {
            var alias = reader.RequireAttribute("alias");

            var byAlias = _resolvedAliases.ByAlias[tableDef.Type];

            if (byAlias.TryGetValue(alias, out var r))
            {
                var record = table.Records.FirstOrDefault(x => x.Get<Ref>(8) == r);

                if (record != null)
                {
                    ModifyShared(reader, record, tableDef);
                    return;
                }
            }

            reader.ThrowException($"Record with alias '{alias}' not found in table '{tableDef.Name}'");
        }

        private void AddRecord(XmlFileReader reader, TableDefinition tableDef, Table table, bool isAutoRecord = false)
        {
            _recordBuilder.InitializeRecord();

            // Create record
            var subtableName = reader.GetAttribute("type");
            var def = subtableName == null
                ? (ITableDefinition) tableDef
                : tableDef.SubtableByName(subtableName);

            if (def == null)
            {
                if (subtableName != null)
                    reader.ThrowException($"Invalid record type: {subtableName}");
                reader.ThrowException("TableDef was null, this should never happen");
            }

            var record = def.CreateDefaultRecord(_recordBuilder.StringLookup, out var defaultStringAttrsWithValue);
            table.Records.Add(record);

            foreach (var attrDef in def.Attributes)
            {
                if (attrDef.Type == AttributeType.TString)
                {
                    _recordBuilder.SetAttribute(record, attrDef, "");
                }
            }

            // Set default strings if needed
            if (defaultStringAttrsWithValue != null)
                _recordBuilder.SetDefaultStrings(record, defaultStringAttrsWithValue);

            if (isAutoRecord)
                record.RecordId = _context.AutoRecordIds[reader.GetPosition(_patchFileId)];

            // Go through each attribute
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    var attrDef = def.ExpandedAttributeByName(reader.Name);

                    if (attrDef != null)
                    {
                        if (attrDef.ReferedTable != _datafileDef.TextTableId)
                        {
                            try
                            {
                                _recordBuilder.SetAttribute(record, attrDef, reader.Value);
                            }
                            catch (BnsInvalidReferenceException e)
                            {
                                if (attrDef.ReferedTable != _effectGroupTableTypeHack)
                                    reader.ThrowException(e.Message);
                            }
                            catch (Exception e)
                            {
                                reader.ThrowException(e.Message);
                            }
                        }
                        else
                        {
                            // TODO: PatchAliasResolver needs to be the one that creates Text records 
                            if (!string.IsNullOrEmpty(reader.Value) && reader.Value[0] == '`')
                            {
                                _textRecordBuilder.InitializeRecord();
                                var textRecord = _textTableDef.CreateDefaultRecord(_textRecordBuilder.StringLookup, out _);
                                var newTextRecordId = ++_context.LastRecordId[_textTableType];
                                textRecord.Set(8, newTextRecordId);
                                _textTable.Records.Add(textRecord);
                                _textRecordBuilder.SetAttribute(textRecord, _textAliasAttrDef, $"TXTGen.{newTextRecordId}");
                                _textRecordBuilder.SetAttribute(textRecord, _textAttrDef, reader.Value[1..]);
                                _textRecordBuilder.FinalizeRecord();

                                record.Set(attrDef.Offset, new Ref(newTextRecordId));
                            }
                            else
                            {
                                _recordBuilder.SetAttribute(record, attrDef, reader.Value);
                            }
                        }
                    }
                    else if (reader.Name != "type")
                        reader.Log($"Failed to find attrDef for: {reader.Name}, value: {reader.Value}");
                } while (reader.MoveToNextAttribute());
            }

            _recordBuilder.FinalizeRecord();
        }

        private void InitializeRecordBuilderTable(Table table)
        {
            if (!table.IsCompressed && table.Records.Count > 0)
                _recordBuilder.InitializeTable(table.IsCompressed, table.Records.First().StringLookup);
            else
                _recordBuilder.InitializeTable(table.IsCompressed);
        }
    }
}