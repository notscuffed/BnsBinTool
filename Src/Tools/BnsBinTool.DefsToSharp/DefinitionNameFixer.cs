using System;
using System.Collections.Generic;
using System.Linq;
using BnsBinTool.Core.Definitions;

namespace BnsBinTool.DefsToSharp
{
    public class DefinitionNameFixer
    {
        private readonly Func<string, string> _tableDefNameFixer;
        private readonly Func<string, string, string> _subtableDefNameFixer;
        private readonly Func<string, string> _attrDefNameFixer;
        private readonly Func<string, string> _sequenceDefNameFixer;
        private readonly Func<string, string> _sequenceValueNameFixer;

        public DefinitionNameFixer(
            Func<string, string> tableDefNameFixer,
            Func<string, string, string> subtableDefNameFixer,
            Func<string, string> attrDefNameFixer,
            Func<string, string> sequenceDefNameFixer,
            Func<string, string> sequenceValueNameFixer)
        {
            _tableDefNameFixer = tableDefNameFixer;
            _subtableDefNameFixer = subtableDefNameFixer;
            _attrDefNameFixer = attrDefNameFixer;
            _sequenceDefNameFixer = sequenceDefNameFixer;
            _sequenceValueNameFixer = sequenceValueNameFixer;
        }

        public void Fix(DatafileDefinition definitions)
        {
            foreach (var tableDef in definitions.TableDefinitions)
            {
                foreach (var subtableDef in tableDef.Subtables)
                {
                    subtableDef.Name = _subtableDefNameFixer(tableDef.Name, subtableDef.Name);
                    
                    foreach (var attrDef in subtableDef.Attributes)
                    {
                        attrDef.Name = _attrDefNameFixer(attrDef.Name);
                    }
                }
                
                tableDef.Name = _tableDefNameFixer(tableDef.Name);

                foreach (var attrDef in tableDef.Attributes)
                {
                    attrDef.Name = _attrDefNameFixer(attrDef.Name);
                }
            }

            var fixedNames = new List<string>();
            
            foreach (var sequenceDef in definitions.SequenceDefinitions)
            {
                sequenceDef.Name = _sequenceDefNameFixer(sequenceDef.Name);
                
                fixedNames.Clear();
                fixedNames.AddRange(sequenceDef.Sequence.Select(value => _sequenceValueNameFixer(value)));
                sequenceDef.Sequence.Clear();
                sequenceDef.Sequence.AddRange(fixedNames);
            }
        }
    }
}