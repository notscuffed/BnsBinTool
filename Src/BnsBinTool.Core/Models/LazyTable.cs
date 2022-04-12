using System.Collections.Generic;
using BnsBinTool.Core.Serialization;
using BnsBinTool.Core.Sources;

namespace BnsBinTool.Core.Models
{
    public class LazyTable : Table
    {
        private List<Record> _records;
        private readonly ITableReader _reader;

        public LazyTable(ITableReader reader)
        {
            _reader = reader;
        }
        
        public ISource Source { get; set; }
        public bool LoadedRecords => _records != null;

        public override List<Record> Records
        {
            get
            {
                if (_records != null)
                    return _records;

                _records = new List<Record>();

                using var reader = Source.CreateReader();
                var table = _reader.ReadFrom(reader);

                _records = table.Records;
                Padding = table.Padding;
                RecordCountOffset = table.RecordCountOffset;

                return _records;
            }
        }
    }
}