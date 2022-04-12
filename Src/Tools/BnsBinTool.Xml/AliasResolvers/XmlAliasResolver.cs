using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using BnsBinTool.Core;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Xml.Helpers;

namespace BnsBinTool.Xml.AliasResolvers
{
    public static class XmlAliasResolver
    {
        public static ResolvedAliases Resolve(
            string tablesDirectory,
            HashSet<TableDefinition> tableDefs,
            Func<TableDefinition, List<string>> getPathsForTableDef)
        {
            var resolvedAliases = new ResolvedAliases();

            foreach (var tableDef in tableDefs.Where(tableDef => !tableDef.IsEmpty))
            {
                resolvedAliases.ByRef[tableDef.Type] = new Dictionary<Ref, string>();
                resolvedAliases.ByAlias[tableDef.Type] = new Dictionary<string, Ref>();
            }

            Parallel.ForEach(tableDefs, tableDef =>
            {
                if (tableDef.IsEmpty)
                    return;

                var byRef = resolvedAliases.ByRef[tableDef.Type];
                var byAlias = resolvedAliases.ByAlias[tableDef.Type];

                var aliasAttr = tableDef["alias"];

                if (aliasAttr == null)
                    return;


                foreach (var relativePath in getPathsForTableDef(tableDef))
                {
                    ResolveForXml(tablesDirectory + "\\" + relativePath + ".xml", relativePath + ".xml", tableDef, byRef, byAlias);
                }
            });

            return resolvedAliases;
        }

        private static void ResolveForXml(string fullFilePath, string filePath, TableDefinition tableDef, Dictionary<Ref, string> byRef, Dictionary<string, Ref> byAlias)
        {
            if (!File.Exists(fullFilePath))
                return;

            // Get all aliases
            using var xmlReader = new XmlFileReader(fullFilePath, filePath)
            {
                WhitespaceHandling = WhitespaceHandling.None
            };

            if (!xmlReader.ReadNonComment() || xmlReader.NodeType != XmlNodeType.XmlDeclaration)
                xmlReader.ThrowException("Failed to read xml declaration");

            if (!xmlReader.ReadNonComment() || xmlReader.Name != "table")
                xmlReader.ThrowException("Failed to read table element");

            var keyAttributes = tableDef.Attributes
                .Where(x => x.IsKey && x.Offset is >= 8 and < 16)
                .ToDictionary(x => x.Name);

            Span<byte> refData = stackalloc byte[8];

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    string alias = null;

                    if (xmlReader.MoveToFirstAttribute())
                    {
                        do
                        {
                            if (xmlReader.Name == "alias")
                            {
                                alias = xmlReader.Value;
                                continue;
                            }

                            XmlHelper.ReadRefAttribute(xmlReader, refData, keyAttributes);
                        } while (xmlReader.MoveToNextAttribute());
                    }

                    if (alias != null)
                    {
                        var @ref = refData.Get<Ref>(0);
                        byRef[@ref] = alias;
                        byAlias[alias] = @ref;
                    }

                    continue;
                }

                if (xmlReader.NodeType == XmlNodeType.Comment)
                    continue;

                break;
            }
        }
    }
}