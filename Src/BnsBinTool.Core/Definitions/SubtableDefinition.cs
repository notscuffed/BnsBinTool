using System;
using System.Collections.Generic;
using System.Linq;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Core.Definitions
{
    public class SubtableDefinition : ITableDefinition
    {
        private Dictionary<string, AttributeDefinition> _attributesDictionary =
            new Dictionary<string, AttributeDefinition>();
        private Dictionary<string, AttributeDefinition> _expandedAttributesDictionary =
            new Dictionary<string, AttributeDefinition>();
        private Record _defaultRecord;
        private readonly List<AttributeDefinition> _defaultStringAttrsWithValue
            = new List<AttributeDefinition>();
        
        public static SubtableDefinition LoadFrom(ITableDefinition parent, dynamic jsub, short type)
        {
            var subtable = new SubtableDefinition();

            subtable.Name = jsub["name"];
            subtable.Size = jsub["size"];
            subtable.SubclassType = type;

            // Add parent expanded attributes
            subtable.ExpandedAttributes.AddRange(parent.ExpandedAttributes);

            if (jsub["attr"] != null)
            {
                foreach (var attr in jsub["attr"])
                {
                    AttributeDefinition attrDef = AttributeDefinition.LoadFrom(attr);

                    if (attrDef == null)
                        continue;

                    // HACK: Handle case when there's name conflict in subtable
                    if (parent.Attributes.Any(x => x.Name == attrDef.Name))
                    {
                        attrDef.Name += "-rep";
                        attrDef.OriginalName = attrDef.Name;
                    }

                    subtable.Attributes.Add(attrDef);

                    // Expand repeated attributes if needed
                    if (attrDef.Repeat == 1)
                    {
                        subtable.ExpandedAttributes.Add(attrDef);
                        subtable.ExpandedAttributesSubOnly.Add(attrDef);
                        continue;
                    }

                    for (var i = 1; i <= attrDef.Repeat; i++)
                    {
                        var newAttrDef = attrDef.DuplicateOffseted((i - 1) * attrDef.Size);
                        newAttrDef.Name += $"-{i}";
                        newAttrDef.OriginalName = newAttrDef.Name;
                        newAttrDef.Repeat = 1;
                        subtable.ExpandedAttributes.Add(newAttrDef);
                        subtable.ExpandedAttributesSubOnly.Add(newAttrDef);
                    }
                }
            }

            subtable._attributesDictionary = subtable.Attributes.ToDictionary(x => x.Name);
            subtable._expandedAttributesDictionary = subtable.ExpandedAttributes.ToDictionary(x => x.Name);

            foreach (var attrDef in subtable.ExpandedAttributes)
            {
                if (attrDef.Type != AttributeType.TString || string.IsNullOrEmpty(attrDef.DefaultValue))
                    continue;
                
                subtable._defaultStringAttrsWithValue.Add(attrDef);
            }
            
            subtable._defaultRecord = subtable.CreateDefaultRecordInternal();
            
            return subtable;
        }
        
        public string Name { get; set; }
        public ushort Size { get; set; }
        public short SubclassType { get; set; }

        public List<AttributeDefinition> Attributes { get; } = new List<AttributeDefinition>();
        public List<AttributeDefinition> ExpandedAttributes { get; } = new List<AttributeDefinition>();
        public List<AttributeDefinition> ExpandedAttributesSubOnly { get; } = new List<AttributeDefinition>();

        public AttributeDefinition this[string name] => _attributesDictionary.GetValueOrDefault(name, null);
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