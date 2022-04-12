using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BnsBinTool.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BnsBinTool.Core.Definitions
{
    public class TableDefinition : ITableDefinition
    {
        private Dictionary<string, AttributeDefinition> _attributesDictionary =
            new Dictionary<string, AttributeDefinition>();

        private Dictionary<string, SubtableDefinition> _subtablesDictionary =
            new Dictionary<string, SubtableDefinition>();

        private Dictionary<string, AttributeDefinition> _expandedAttributesDictionary =
            new Dictionary<string, AttributeDefinition>();

        private Record _defaultRecord;

        private readonly List<AttributeDefinition> _defaultStringAttrsWithValue
            = new List<AttributeDefinition>();

        public static TableDefinition LoadFrom(string path)
        {
            var table = new TableDefinition();

            dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(path));

            if (json == null)
                throw new Exception("Failed to load json");

            table.Name = json["name"];
            table.OriginalName = table.Name;
            table.Type = json["type"];
            table.MajorVersion = json["major_ver"];
            table.MinorVersion = json["minor_ver"];
            table.AutoKey = json["autokey"];
            table.MaxId = json["maxid"];
            JArray json_el = json["el"];

            if (json_el == null)
                throw new Exception("Failed to get el");

            foreach (dynamic el in json_el)
            {
                ElDefinition elDef = ElDefinition.LoadFrom(el);
                table.Els.Add(elDef);
            }

            var jrecord = json_el.FirstOrDefault(x => x["name"].Value<string>() == "record");

            // If we don't find record definition then we just return empty table
            if (jrecord == null)
            {
                table.IsEmpty = true;
                return table;
            }

            dynamic jbody = jrecord["body"];

            if (jbody == null)
                throw new Exception("Failed to get body");

            table.Size = jbody["size"];

            // Add auto key id
            if (table.AutoKey)
            {
                var autoIdAttr = new AttributeDefinition
                {
                    Name = "auto-id",
                    OriginalName = "auto-id",
                    Size = 8,
                    Offset = 8,
                    Type = AttributeType.TInt64,
                    OriginalTypeName = "int64",
                    TypeName = "int64",
                    AttributeDefaultValues = new AttributeDefaultValues(),
                    DefaultValue = "0",
                    IsKey = true,
                    IsRequired = true,
                    Repeat = 1
                };

                table.Attributes.Add(autoIdAttr);
                table.ExpandedAttributes.Add(autoIdAttr);
            }

            if (jbody["attr"] != null)
            {
                foreach (var attr in jbody["attr"])
                {
                    AttributeDefinition attrDef = AttributeDefinition.LoadFrom(attr);

                    if (attrDef == null)
                        continue;

                    table.Attributes.Add(attrDef);

                    // Expand repeated attributes if needed
                    if (attrDef.Repeat == 1)
                    {
                        table.ExpandedAttributes.Add(attrDef);
                        continue;
                    }

                    for (var i = 1; i <= attrDef.Repeat; i++)
                    {
                        var newAttrDef = attrDef.DuplicateOffseted((i - 1) * attrDef.Size);
                        newAttrDef.Name += $"-{i}";
                        newAttrDef.OriginalName = newAttrDef.Name;
                        newAttrDef.Repeat = 1;
                        table.ExpandedAttributes.Add(newAttrDef);
                    }
                }
            }

            short subIndex = 0;
            if (jbody["sub"] != null)
            {
                foreach (var sub in jbody["sub"])
                {
                    SubtableDefinition subtable = SubtableDefinition.LoadFrom(table, sub, subIndex++);

                    if (subtable != null)
                        table.Subtables.Add(subtable);
                }
            }

            table._attributesDictionary = table.Attributes.ToDictionary(x => x.Name);
            table._expandedAttributesDictionary = table.ExpandedAttributes.ToDictionary(x => x.Name);
            table._subtablesDictionary = table.Subtables.ToDictionary(x => x.Name);

            table.IsEmpty = table.Attributes.Count == 0
                            && (table.Subtables.Count == 0
                                || table.Subtables.All(x => x.Attributes.Count == 0));

            foreach (var attrDef in table.ExpandedAttributes)
            {
                if (attrDef.Type != AttributeType.TString || string.IsNullOrEmpty(attrDef.DefaultValue))
                    continue;

                table._defaultStringAttrsWithValue.Add(attrDef);
            }

            table._defaultRecord = table.CreateDefaultRecordInternal();

            return table;
        }

        public short Type { get; set; }
        public short SubclassType { get; set; } = -1; // always -1 on base table definition
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public ushort MajorVersion { get; set; }
        public ushort MinorVersion { get; set; }
        public bool AutoKey { get; set; }
        public long MaxId { get; set; }

        public ushort Size { get; set; }
        public bool IsEmpty { get; set; }
        public List<AttributeDefinition> Attributes { get; } = new List<AttributeDefinition>();
        public List<SubtableDefinition> Subtables { get; } = new List<SubtableDefinition>();
        public List<AttributeDefinition> ExpandedAttributes { get; } = new List<AttributeDefinition>();
        public List<ElDefinition> Els { get; } = new();

        public AttributeDefinition this[string name] => _attributesDictionary.GetValueOrDefault(name, null);
        public SubtableDefinition SubtableByName(string name) => _subtablesDictionary.GetValueOrDefault(name, null);
        public AttributeDefinition ExpandedAttributeByName(string name) => _expandedAttributesDictionary.GetValueOrDefault(name, null);


        public Record CreateDefaultRecord(StringLookup stringLookup, out List<AttributeDefinition> defaultStringAttrsWithValue)
        {
            var length = _defaultRecord.Data.Length;

            var record = new Record
            {
                Data = new byte[length],
                StringLookup = stringLookup
            };

            Array.Copy(_defaultRecord.Data, record.Data, length);

            if (_defaultStringAttrsWithValue.Count == 0)
            {
                defaultStringAttrsWithValue = null;
                return record;
            }

            defaultStringAttrsWithValue = _defaultStringAttrsWithValue;
            return record;
        }

        private Record CreateDefaultRecordInternal()
        {
            var record = new Record
            {
                Data = new byte[Size],
                DataSize = Size,
                SubclassType = SubclassType,
                XmlNodeType = 1
            };

            AttributeDefaultValues.SetRecordDefaults(record, this);

            return record;
        }
    }
}