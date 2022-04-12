using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using BnsBinTool.Core;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Xml.Helpers
{
    public static class XmlHelper
    {
        public static void HandleSubelements(this XmlFileReader reader, string rootName, Func<XmlFileReader, bool> handleElement)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (handleElement(reader))
                        continue;

                    reader.ThrowException($"Invalid element: '{reader.Name}'");
                    return;
                }

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == rootName)
                    break;

                if (reader.NodeType == XmlNodeType.Comment)
                    continue;

                reader.ThrowException($"Invalid node type: '{reader.NodeType}', name: '{reader.Name}'");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RequireAttribute(this XmlFileReader reader, string attributeName)
        {
            var result = reader.GetAttribute(attributeName);

            if (result != null)
                return result;

            reader.ThrowException($"Missing '{attributeName}' attribute");
            return null;
        }

        public static void GetInTable(this XmlFileReader reader, DatafileDefinition datafileDef, Dictionary<int, Table> tables, out TableDefinition tableDef, out Table table)
        {
            var tableName = reader.RequireAttribute("in-table");

            tableDef = datafileDef.TableDefinitions
                .FirstOrDefault(x => x.OriginalName == tableName);

            if (tableDef != null)
            {
                if (tables.TryGetValue(tableDef.Type, out table))
                    return;

                ThrowHelper.ThrowException($"Failed to get table from datafile: {tableDef.Name}:{tableDef.Type}");
            }

            table = null;
            reader.ThrowException($"Table definition doesn't exist: '{tableName}'");
        }

        public static XmlPosition GetPosition(this XmlFileReader reader, int fileId)
        {
            return new XmlPosition(reader.LineNumber, reader.LinePosition, fileId);
        }

        public static void Log(this XmlFileReader reader, string message)
        {
            Logger.LogTime($"{message} in {reader.FilePath} at line {reader.LineNumber}:{reader.LinePosition}");
        }

        public static bool ReadRefAttribute(XmlFileReader reader, Span<byte> refData, Dictionary<string, AttributeDefinition> keyAttrs)
        {
            if (!keyAttrs.TryGetValue(reader.Name, out var attrDef))
                return false;
            
            switch (attrDef.Type)
            {
                case AttributeType.TInt64:
                    refData.Set(attrDef.Offset - 8, long.Parse(reader.Value));
                    break;

                case AttributeType.TInt32:
                case AttributeType.TMsec:
                    refData.Set(attrDef.Offset - 8, int.Parse(reader.Value));
                    break;

                case AttributeType.TDistance:
                case AttributeType.TInt16:
                case AttributeType.TSub:
                    refData.Set(attrDef.Offset - 8, short.Parse(reader.Value));
                    break;

                case AttributeType.TInt8:
                    refData[attrDef.Offset - 8] = byte.Parse(reader.Value);
                    break;

                case AttributeType.TBool:
                    refData[attrDef.Offset - 8] =
                        Constants.PositiveXmlValues.Contains(reader.Value, StringComparer.OrdinalIgnoreCase)
                            ? (byte) 1
                            : (byte) 0;
                    break;

                case AttributeType.TSeq:
                case AttributeType.TProp_seq:
                    refData.Set(attrDef.Offset - 8, (sbyte) attrDef.Sequence.IndexOf(reader.Value));
                    break;
                    
                case AttributeType.TSeq16:
                case AttributeType.TProp_field:
                    refData.Set(attrDef.Offset - 8, (short) attrDef.Sequence.IndexOf(reader.Value));
                    break;

                default:
                    ThrowHelper.ThrowException("Unhandled type: " + attrDef.Type);
                    break;
            }

            return true;
        }

        public static bool ReadNonComment(this XmlFileReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Comment)
                    continue;

                return true;
            }

            return false;
        }
    }

    public readonly struct XmlPosition
    {
        public readonly int LineNumber;
        public readonly int LinePosition;
        public readonly int FileId;

        public XmlPosition(int lineNumber, int linePosition, int fileId)
        {
            LineNumber = lineNumber;
            LinePosition = linePosition;
            FileId = fileId;
        }
    }
}