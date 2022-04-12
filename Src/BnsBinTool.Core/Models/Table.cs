using System.Collections.Generic;
using System.IO;
using BnsBinTool.Core.Serialization;

namespace BnsBinTool.Core.Models
{
    public class Table : TableHeader
    {
        public static Table ReadFromBytes(byte[] bytes)
        {
            var bnsTableReader = new TableReader();
            
            using var reader = new BinaryReader(new MemoryStream(bytes));

            return bnsTableReader.ReadFrom(reader);
        }
        
        public byte[] ToArray(bool is64Bit)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);

            var bnsTableWriter = new TableWriter();
            bnsTableWriter.WriteTo(writer, this, is64Bit:false);

            return memoryStream.ToArray();
        }
        
        public virtual List<Record> Records { get; } = new List<Record>();

        /// <summary>
        /// TODO: Hack because the table seems to offset it randomly?
        /// </summary>
        public int RecordCountOffset { get; set; }

        /// <summary>
        /// TODO: Hack because idk where this padding is coming from
        /// </summary>
        public byte[] Padding { get; set; }
    }
}