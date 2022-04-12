using System;
using System.IO;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Core.Serialization
{
    public class DatafileWriter
    {
        public static DatafileWriter Default => new DatafileWriter(
            new NameTableWriter(),
            new TableWriter());

        private readonly INameTableWriter _nameTableWriter;
        private readonly ITableWriter _tableWriter;

        public DatafileWriter(
            INameTableWriter nameTableWriter,
            ITableWriter tableWriter)
        {
            _nameTableWriter = nameTableWriter;
            _tableWriter = tableWriter;
        }

        public void WriteTo(BinaryWriter writer, Datafile datafile)
        {
            var overwriteNameTableSize = datafile.WriteHeaderTo(writer,
                datafile.Tables.Count,
                datafile.NameTable?.Entries.Count ?? datafile.AliasCount, datafile.Is64Bit);

            if (datafile.NameTable == null)
                overwriteNameTableSize(datafile.AliasMapSize);

            if (datafile.Tables.Count > 10)
            {
                if (datafile.NameTable == null)
                    throw new NullReferenceException("NameTable was null on main datafile");
                var oldPosition = writer.BaseStream.Position;
                _nameTableWriter.WriteTo(writer, datafile.NameTable, datafile.Is64Bit);
                var nameTableSize = writer.BaseStream.Position - oldPosition;
                datafile.AliasMapSize = nameTableSize;
                datafile.AliasCount = datafile.NameTable.Entries.Count;
                overwriteNameTableSize(nameTableSize);
            }

            foreach (var table in datafile.Tables)
            {
                _tableWriter.WriteTo(writer, table, datafile.Is64Bit);
            }

            writer.Flush();
        }
    }
}