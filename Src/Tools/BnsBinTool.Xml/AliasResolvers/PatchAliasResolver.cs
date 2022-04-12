using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using BnsBinTool.Core;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;
using BnsBinTool.Xml.Helpers;
using BnsBinTool.Xml.Models;

namespace BnsBinTool.Xml.AliasResolvers
{
    /// <summary>
    /// Note: this currently mutates the passed in tables
    /// </summary>
    // TODO: stop mutating table in here
    public class PatchAliasResolver
    {
        private readonly ResolvedAliases _resolvedAliases;
        private readonly DatafileDefinition _datafileDef;
        private readonly Dictionary<short, Dictionary<string, AttributeDefinition>> _keyAttributesPerTable;
        private readonly Dictionary<int, Table> _tables;
        private readonly DatafileTransformerContext _context;
        private int _fileId;

        public PatchAliasResolver(ResolvedAliases resolvedAliases, DatafileDefinition datafileDef, Dictionary<int, Table> tables,
            DatafileTransformerContext context)
        {
            _resolvedAliases = resolvedAliases;
            _datafileDef = datafileDef;
            _tables = tables;
            _context = context;

            _keyAttributesPerTable = datafileDef
                .TableDefinitions
                .ToDictionary(
                    x => x.Type,
                    x => x.Attributes
                        .Where(y => y.IsKey && y.Offset is >= 8 and < 16)
                        .ToDictionary(y => y.Name)
                );
        }

        public void Resolve(string patchesRootPath)
        {
            _fileId = 0;
            foreach (var path in Directory.EnumerateFiles(patchesRootPath, "*.xml", SearchOption.AllDirectories))
            {
                ResolveForPatchFile(path, path[(patchesRootPath.Length + 1)..]);
                _fileId++;
            }
        }

        private void ResolveForPatchFile(string fullFilePath, string filePath)
        {
            if (!File.Exists(fullFilePath))
                return;

            // Get all aliases
            using var reader = new XmlFileReader(fullFilePath, filePath)
            {
                WhitespaceHandling = WhitespaceHandling.None
            };

            if (!reader.Read() || reader.NodeType != XmlNodeType.XmlDeclaration)
                reader.ThrowException("Failed to read xml declaration");

            if (!reader.Read() || reader.Name != "patch")
                reader.ThrowException("Failed to read patch element");

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
                            HandleTranslation(reader);
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

        private void HandleAdd(XmlFileReader reader)
        {
            reader.GetInTable(_datafileDef, _tables, out var tableDef, out _);

            var byAlias = _resolvedAliases.ByAlias[tableDef.Type];
            var byRef = _resolvedAliases.ByRef[tableDef.Type];

            Span<byte> refData = stackalloc byte[8];

            var keyAttributes = _keyAttributesPerTable[tableDef.Type];

            var isAutoTable = tableDef.AutoKey;
            var tableType = tableDef.Type;

            var aliasAttrDef = tableDef["alias"];

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "record":
                        {
                            if (aliasAttrDef != null)
                            {
                                var alias = reader.GetAttribute("alias");

                                if (string.IsNullOrWhiteSpace(alias))
                                {
                                    if (tableDef.Name == "zonepathway")
                                        continue;
                                    
                                    reader.ThrowException("Missing alias");
                                }
                                
                                if (byAlias.ContainsKey(alias))
                                    reader.ThrowException($"Alias already exists: \"{alias}\"");

                                var reference = GetRef(reader, refData, keyAttributes);
                                byRef[reference] = alias;
                                byAlias[alias] = reference;
                            }

                            continue;
                        }

                        case "auto-record":
                        {
                            if (!isAutoTable)
                                reader.ThrowException($"Table '{tableDef.Name}' does not support auto records");

                            var autoId = ++_context.LastRecordId[tableType];
                            _context.AutoRecordIds[reader.GetPosition(_fileId)] = autoId;

                            if (aliasAttrDef != null)
                            {
                                var alias = reader.GetAttribute("alias");

                                if (string.IsNullOrWhiteSpace(alias))
                                    reader.ThrowException("Missing alias");
                                
                                if (byAlias.ContainsKey(alias))
                                    reader.ThrowException($"Alias already exists: \"{alias}\"");

                                var reference = new Ref(autoId);
                                byRef[reference] = alias;
                                byAlias[alias] = reference;
                            }

                            continue;
                        }

                        default:
                            reader.ThrowException($"Invalid element: '{reader.Name}'");
                            return;
                    }
                }

                if (reader.NodeType == XmlNodeType.EndElement)
                    break;

                if (reader.NodeType == XmlNodeType.Comment)
                    continue;

                reader.ThrowException($"Invalid node type: '{reader.NodeType}'");
            }
        }

        private static Ref GetRef(
            XmlFileReader reader,
            Span<byte> refData,
            Dictionary<string, AttributeDefinition> keyAttributes)
        {
            if (!reader.MoveToFirstAttribute())
                reader.ThrowException("Failed to get attributes");

            refData.Clear();

            var keyCount = 0;
            do
            {
                if (XmlHelper.ReadRefAttribute(reader, refData, keyAttributes))
                    keyCount++;
            } while (reader.MoveToNextAttribute());

            if (keyCount == 0)
                reader.ThrowException("Not a single key has been specified");

            return refData.Get<Ref>(0);
        }

        private static void HandleTranslation(XmlFileReader reader)
        {
            while (reader.Read() && (reader.NodeType != XmlNodeType.EndElement || reader.Name != "translate"))
            {
            }
        }

        private static void HandleModify(XmlFileReader reader)
        {
            // TODO: actually need to get injected text's aliases and add it to alias map
            while (reader.Read() && (reader.NodeType != XmlNodeType.EndElement || reader.Name != "modify"))
            {
            }
        }

        private void HandleDelete(XmlFileReader reader)
        {
            reader.GetInTable(_datafileDef, _tables, out var tableDef, out var table);

            var byAlias = _resolvedAliases.ByAlias[tableDef.Type];
            var byRef = _resolvedAliases.ByRef[tableDef.Type];

            Span<byte> refData = stackalloc byte[8];

            var keyAttributes = _keyAttributesPerTable[tableDef.Type];

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "by-alias":
                        {
                            var alias = reader.GetAttribute("alias");

                            if (string.IsNullOrWhiteSpace(alias))
                                reader.ThrowException("Missing alias");

                            if (!byAlias.TryGetValue(alias, out var reference))
                                reader.ThrowException($"Failed to find record with alias: '{alias}'");

                            var record = table.Records.FirstOrDefault(x => x.Get<Ref>(8) == reference);
                            if (record != null)
                            {
                                // TODO: Record should be actually removed in DatafileTransformer not here
                                table.Records.Remove(record);
                            }
                            else reader.ThrowException($"Failed to find record using reference from alias: '{alias}'");

                            byAlias.Remove(alias);
                            byRef.Remove(reference);

                            continue;
                        }

                        case "by-ref":
                        {
                            var reference = GetRef(reader, refData, keyAttributes);

                            var record = table.Records.FirstOrDefault(x => x.Get<Ref>(8) == reference);
                            if (record != null)
                            {
                                // TODO: Record should be actually removed in DatafileTransformer not here
                                table.Records.Remove(record);
                            }
                            else reader.ThrowException($"Failed to find record using reference: '{reference}'");

                            if (byRef.TryGetValue(reference, out var alias))
                                byAlias.Remove(alias);
                            byRef.Remove(reference);

                            continue;
                        }

                        default:
                            reader.ThrowException($"Invalid element: '{reader.Name}'");
                            break;
                    }
                }

                if (reader.NodeType == XmlNodeType.EndElement)
                    break;

                if (reader.NodeType == XmlNodeType.Comment)
                    continue;

                reader.ThrowException($"Invalid node type: '{reader.NodeType}'");
            }
        }

        private void HandleReplace(XmlFileReader reader)
        {
            reader.GetInTable(_datafileDef, _tables, out var tableDef, out _);

            // If the table definition doesn't have an alias then we just skip it
            if (tableDef.Attributes.All(x => x.OriginalName != "alias"))
            {
                while (reader.Read() && (reader.NodeType != XmlNodeType.EndElement || reader.Name != "replace"))
                {
                }

                return;
            }

            var keyAttributes = _keyAttributesPerTable[tableDef.Type];
            var byAlias = _resolvedAliases.ByAlias[tableDef.Type];
            var byRef = _resolvedAliases.ByRef[tableDef.Type];

            byAlias.Clear();
            byRef.Clear();

            Span<byte> refData = stackalloc byte[8];
            
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name != "record")
                        reader.ThrowException($"Invalid element: '{reader.Name}'");

                    var alias = reader.GetAttribute("alias");

                    if (string.IsNullOrWhiteSpace(alias))
                        reader.ThrowException("Missing alias");

                    var reference = GetRef(reader, refData, keyAttributes);

                    byAlias[alias] = reference;
                    byRef[reference] = alias;
                    continue;
                }

                if (reader.NodeType == XmlNodeType.EndElement)
                    break;

                if (reader.NodeType == XmlNodeType.Comment)
                    continue;

                reader.ThrowException($"Invalid node type: '{reader.NodeType}'");
            }
        }
    }
}