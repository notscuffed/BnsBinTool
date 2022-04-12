using System;
using System.Collections.Generic;

namespace BnsBinTool.Core.Definitions
{
    public class SequenceDefinitionLoader
    {
        private readonly Dictionary<string, List<SequenceDefinition>> _duplicateSequences
            = new Dictionary<string, List<SequenceDefinition>>();

        public List<SequenceDefinition> LoadFor(IEnumerable<TableDefinition> tableDefs, bool mergeDuplicated)
        {
            var allSequenceDefinitions = new List<SequenceDefinition>();

            foreach (var tableDef in tableDefs)
            {
                LoadForTable(tableDef, allSequenceDefinitions, mergeDuplicated);

                foreach (var subtableDef in tableDef.Subtables)
                {
                    LoadForTable(subtableDef, allSequenceDefinitions, mergeDuplicated);
                }
            }

            return allSequenceDefinitions;
        }

        private void LoadForTable(ITableDefinition tableDef, List<SequenceDefinition> allSequenceDefinitions, bool mergeDuplicated)
        {
            List<SequenceDefinition> sequenceDefList = null;
            
            foreach (var attrDef in tableDef.Attributes)
            {
                if (attrDef.Sequence.Count == 0)
                    continue;

                var first = attrDef.Sequence[0];

                if (mergeDuplicated)
                {
                    if (!_duplicateSequences.TryGetValue(first, out sequenceDefList))
                    {
                        sequenceDefList = new List<SequenceDefinition>();
                        _duplicateSequences[first] = sequenceDefList;
                    }

                    foreach (var sequenceDef in sequenceDefList)
                    {
                        if (sequenceDef.Size != attrDef.Size)
                            continue;

                        if (!IsSequenceEqual(sequenceDef.Sequence, attrDef.Sequence, StringComparer.OrdinalIgnoreCase))
                            continue;

                        if (attrDef.Sequence.Count > sequenceDef.Sequence.Count)
                        {
                            for (var i = sequenceDef.Sequence.Count; i < attrDef.Sequence.Count; i++)
                            {
                                sequenceDef.Sequence.Add(attrDef.Sequence[i]);
                                sequenceDef.OriginalSequence.Add(attrDef.Sequence[i]);
                            }
                        }

                        // Assign sequence definition
                        attrDef.SequenceDef = sequenceDef;

                        goto FOUND_SEQUENCE;
                    }
                }

                // Create new one if we didn't find existing one
                var newSequenceDef = new SequenceDefinition(attrDef.Name, attrDef.Size); // Use attribute name as sequence name
                newSequenceDef.Sequence.AddRange(attrDef.Sequence);
                newSequenceDef.OriginalSequence.AddRange(attrDef.Sequence);
                
                if (mergeDuplicated)
                    sequenceDefList.Add(newSequenceDef);
                allSequenceDefinitions.Add(newSequenceDef);
                
                // Assign sequence definition
                attrDef.SequenceDef = newSequenceDef;
                
                FOUND_SEQUENCE: ;
            }
        }

        private static bool IsSequenceEqual(IEnumerable<string> a, IEnumerable<string> b, StringComparer comparer)
        {
            using var enumeratorA = a.GetEnumerator();
            using var enumeratorB = b.GetEnumerator();

            while (enumeratorA.MoveNext() & enumeratorB.MoveNext())
            {
                if (comparer.Equals(enumeratorA.Current, enumeratorB.Current))
                    continue;

                return false;
            }

            return true;
        }
    }
}