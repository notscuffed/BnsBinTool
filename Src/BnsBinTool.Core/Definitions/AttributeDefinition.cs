using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BnsBinTool.Core.Definitions
{
    public class AttributeDefinition
    {
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string TypeName { get; set; }
        public string OriginalTypeName { get; set; }
        public AttributeType Type { get; set; }
        public string DefaultValue { get; set; }
        public ushort Repeat { get; set; }
        public short ReferedTable { get; set; }
        public ushort Offset { get; set; }
        public ushort Size { get; set; }
        public bool IsKey { get; set; }
        public bool IsRequired { get; set; }
        public bool IsHidden { get; set; }
        public AttributeDefaultValues AttributeDefaultValues { get; set; }

        public List<string> Sequence { get; private set; } = new List<string>();
        public SequenceDefinition SequenceDef { get; internal set; }

        public static AttributeDefinition LoadFrom(dynamic jattr)
        {
            if (jattr["deprecated"] == true)
                return null;

            var attribute = new AttributeDefinition();

            attribute.Name = jattr["name"];
            attribute.OriginalName = jattr["name"];
            attribute.TypeName = jattr["type"];
            attribute.OriginalTypeName = jattr["type"];

            if (jattr["type_id"] == null)
            {
                var index = Array.IndexOf(AttributeTypeNames, attribute.TypeName);

                if (index == -1)
                    throw new Exception("Failed to determine attribute type");

                attribute.Type = (AttributeType) index;
            }
            else
                attribute.Type = (AttributeType) jattr["type_id"];

            attribute.DefaultValue = jattr["defvalue"];

            attribute.Repeat = jattr["repeat"];
            attribute.ReferedTable = jattr["reftable"];
            attribute.Offset = jattr["offset"];
            attribute.Size = jattr["size"];

            attribute.IsKey = jattr["key"];
            attribute.IsRequired = jattr["required"];
            attribute.IsHidden = jattr["hidden"];

            if (jattr["seq"] != null)
            {
                foreach (JToken s in jattr["seq"])
                {
                    attribute.Sequence.Add(s.Value<string>());
                }
            }

            switch (attribute.Type)
            {
                case AttributeType.TSeq:
                case AttributeType.TProp_seq:
                    if (attribute.Size != 1)
                        Console.WriteLine("Fixing attribute size");
                    attribute.Size = 1;
                    break;
                
                case AttributeType.TSeq16:
                case AttributeType.TProp_field:
                    if (attribute.Size != 2)
                        Console.WriteLine("Fixing attribute size");
                    attribute.Size = 2;
                    break;
            }

            try
            {
                attribute.AttributeDefaultValues = AttributeDefaultValues.FromAttribute(attribute);
            }
            catch (Exception)
            {
                attribute.AttributeDefaultValues = new();
            }

            return attribute;
        }

        public AttributeDefinition DuplicateOffseted(int offset)
        {
            return new AttributeDefinition
            {
                Name = Name,
                OriginalName = OriginalName,
                TypeName = TypeName,
                Type = Type,
                DefaultValue = DefaultValue,
                Repeat = Repeat,
                ReferedTable = ReferedTable,
                Offset = (ushort) (Offset + offset),
                Size = Size,
                IsKey = IsKey,
                IsRequired = IsRequired,
                IsHidden = IsHidden,
                AttributeDefaultValues = AttributeDefaultValues,
                Sequence = Sequence
            };
        }

        public static string[] AttributeTypeNames =
        {
            "none",
            "int8",
            "int16",
            "int32",
            "int64",
            "float32",
            "bool",
            "string",
            "seq",
            "seq16",
            "ref",
            "tref",
            "sub",
            "su",
            "vector16",
            "vector32",
            "icolor",
            "fcolor",
            "box",
            "angle",
            "msec",
            "distance",
            "velocity",
            "prop_seq",
            "prop_field",
            "script_obj",
            "native",
            "version",
            "icon",
            "time32",
            "time64",
        };
    }
}