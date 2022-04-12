using System.Collections.Generic;

namespace BnsBinTool.Core.Definitions
{
    public class SequenceDefinition
    {
        public SequenceDefinition(string name, int size)
        {
            Name = name;
            Size = size;
        }

        public List<string> Sequence { get; } = new List<string>();
        public List<string> OriginalSequence { get; } = new List<string>();
        public string Name { get; set; }
        public int Size { get; set; }
    }
}