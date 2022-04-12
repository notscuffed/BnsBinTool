using System.Collections.Generic;
using BnsBinTool.Core.Serialization;
using BnsBinTool.Core.Sources;

namespace BnsBinTool.Core.Models
{
    public class LazyNameTable : NameTable
    {
        private readonly INameTableReader _nameTableReader;
        private List<NameTableEntry> _entries;

        public LazyNameTable(INameTableReader nameTableReader)
        {
            _nameTableReader = nameTableReader;
        }
        public ISource Source { get; internal set; }
        public bool EntriesLoaded => _entries != null;

        public override List<NameTableEntry> Entries
        {
            get
            {
                if (_entries != null)
                    return _entries;

                using var reader = Source.CreateReader();
                var nameTable = _nameTableReader.ReadFrom(reader);
                _entries = nameTable.Entries;

                return _entries;
            }
        }

        public override void Clear()
        {
            _entries = new List<NameTableEntry>();
        }
    }
}