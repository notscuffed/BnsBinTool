using BnsBinTool.Core.Models;
using BnsBinTool.Core.Sources;

namespace BnsBinTool.Core.Serialization
{
    public class DatafileReader
    {
        public static DatafileReader Default => new DatafileReader(
            new NameTableReader(is64Bit:false),
            new TableReader(is64bit:false),
            is64Bit:false);
        
        public static DatafileReader Default64 => new DatafileReader(
            new NameTableReader(is64Bit:true),
            new TableReader(is64bit:true),
            is64Bit:true);

        public static DatafileReader Lazy(ISource source, bool is64Bit)
        {
            var tableReader = new TableReader(is64bit: is64Bit) {LazyLoadSource = source};
            return new DatafileReader(new NameTableReader(is64Bit:is64Bit) {LazyLoadSource = source}, tableReader, is64Bit);
        }

        private readonly INameTableReader _nameTableReader;
        private readonly ITableReader _tableReader;
        private readonly bool _is64bit;

        public DatafileReader(
            INameTableReader nameTableReader,
            ITableReader tableReader,
            bool is64Bit)
        {
            _nameTableReader = nameTableReader;
            _tableReader = tableReader;
            _is64bit = is64Bit;
        }

        public Datafile ReadFrom(ISource source)
        {
            using var reader = source.CreateReader();

            var bin = new Datafile {Is64Bit = _is64bit};
            bin.ReadHeaderFrom(reader, _is64bit);

            if (bin.ReadTableCount > 10)
                bin.NameTable = _nameTableReader.ReadFrom(reader);

            for (var tableId = 0; tableId < bin.ReadTableCount; tableId++)
            {
                bin.Tables.Add(_tableReader.ReadFrom(reader));
            }

            return bin;
        }
    }
}