using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BnsBinTool.Core.Definitions
{
    public class DatafileDefinition
    {
        public List<TableDefinition> TableDefinitions { get; }
        public short IconTextureTableId { get; }
        public short TextTableId { get; }
        public bool Is64Bit { get; init; }
        public List<SequenceDefinition> SequenceDefinitions { get; } = new List<SequenceDefinition>();

        private readonly Dictionary<string, TableDefinition> _definitionsByName;
        private readonly Dictionary<int, TableDefinition> _definitionsByType;

        private DatafileDefinition(List<TableDefinition> definitions)
        {
            TableDefinitions = definitions;

            var iconTexture = definitions.FirstOrDefault(x => x.Name == "icontexture" || x.Name == "icon-texture");
            if (iconTexture != null)
                IconTextureTableId = iconTexture.Type;

            var text = definitions.FirstOrDefault(x => x.Name == "text");
            if (text != null)
                TextTableId = text.Type;

            _definitionsByName = definitions.ToDictionary(x => x.Name);
            _definitionsByType = definitions.ToDictionary(x => (int) x.Type);
        }

        public TableDefinition this[int index] => _definitionsByType[index];
        public TableDefinition this[string index] => _definitionsByName[index];

        public bool TryGetValue(string index, out TableDefinition tableDef)
        {
            return _definitionsByName.TryGetValue(index, out tableDef);
        }

        public bool TryGetValue(int index, out TableDefinition tableDef)
        {
            return _definitionsByType.TryGetValue(index, out tableDef);
        }

        public static DatafileDefinition Load(string directory, bool mergeDuplicatedSequences = false, bool is64Bit = false)
        {
            var definitions = Directory.EnumerateFiles(directory, "*.json")
                .Select(TableDefinition.LoadFrom)
                .OrderBy(x => x.Type)
                .ToList();

            var datafileDeinition = new DatafileDefinition(definitions) {Is64Bit = is64Bit};
            datafileDeinition.SequenceDefinitions.AddRange(
                new SequenceDefinitionLoader().LoadFor(definitions, mergeDuplicatedSequences));
            return datafileDeinition;
        }
    }
}