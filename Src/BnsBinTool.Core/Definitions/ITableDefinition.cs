using System.Collections.Generic;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Core.Definitions
{
    public interface ITableDefinition
    {
        string Name { get; set; }
        ushort Size { get; set; }
        short SubclassType { get; set; }
        List<AttributeDefinition> ExpandedAttributes { get; }
        List<AttributeDefinition> Attributes { get; }
        AttributeDefinition ExpandedAttributeByName(string name);
        Record CreateDefaultRecord(StringLookup stringLookup, out List<AttributeDefinition> defaultStringAttrsWithValue);
    }
}