using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using BnsBinTool.Core;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Exceptions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Xml.Helpers;

namespace BnsBinTool.Xml
{
    public class XmlValidator
    {
        private static readonly Regex _tableNameInFileNameRegex = new Regex(@"([\w-]+?)data.*?\.xml",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly DatafileDefinition _datafileDef;

        public XmlValidator(DatafileDefinition datafileDef)
        {
            _datafileDef = datafileDef;
        }

        public void ValidateXmlFile(string fullFilePath, string filePath)
        {
            if (!File.Exists(fullFilePath))
                return;

            var tableName = GetTableNameFromFilePath(filePath).ToLowerInvariant();
            if (tableName == null)
                throw new Exception($"Failed to get table name from file path: '{filePath}'");

            var tableDefs = _datafileDef.TableDefinitions.ToDictionary(x => x.OriginalName.Replace("-", ""));

            if (!tableDefs.TryGetValue(tableName.Replace("-", ""), out var tableDef))
                throw new Exception($"Failed to get table definition for table name: '{tableName}'");

            Logger.LogTime($"Validating xml: '{filePath}'");

            using var reader = new XmlFileReader(fullFilePath, filePath);

            if (!reader.Read() || reader.NodeType != XmlNodeType.XmlDeclaration)
                reader.ThrowException("Failed to read xml declaration");

            if (!reader.Read() || reader.Name != "table")
                reader.ThrowException("Failed to read table element");

            var errorCount = 0;

            try
            {
                reader.HandleSubelements("table", r =>
                {
                    if (r.Name != "record")
                        return false;

                    // ReSharper disable once AccessToDisposedClosure
                    CheckAttributes(reader, tableDef, ref errorCount);

                    return true;
                });
            }
            catch (BnsInvalidReferenceException e)
            {
                Logger.LogTime($"Error count: {errorCount}");
                reader.ThrowException(e.Message);
            }

            Logger.LogTime($"Error count: {errorCount}");
        }

        private static string GetTableNameFromFilePath(string filePath)
        {
            var match = _tableNameInFileNameRegex.Match(Path.GetFileName(filePath));

            return !match.Success ? null : match.Groups[1].Value;
        }

        private void CheckAttributes(XmlFileReader reader, TableDefinition tableDef, ref int errorCount)
        {
            var subtableName = reader.GetAttribute("type");

            subtableName = subtableName?.ToLowerInvariant();

            var def = subtableName == null
                ? (ITableDefinition) tableDef
                : tableDef.SubtableByName(subtableName);

            if (def == null)
            {
                if (subtableName != null)
                    reader.ThrowException($"Invalid record type: '{subtableName}'");
                reader.ThrowException("TableDef was null, this should never happen");
            }

            // Go through each attribute
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    var attrDef = def.ExpandedAttributeByName(reader.Name);
                    if (attrDef != null)
                    {
                        if (attrDef.Sequence.Count > 0 && !attrDef.Sequence.Contains(reader.Value.ToLowerInvariant()))
                            reader.Log($"Invalid sequence value: '{reader.Value}' in attribute: '{reader.Name}'");
                    }
                    else if (reader.Name != "type")
                    {
                        reader.Log($"Failed to find attrDef for: '{reader.Name}', value: '{reader.Value}'");
                        errorCount++;
                    }
                } while (reader.MoveToNextAttribute());
            }
        }
    }
}