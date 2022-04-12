using System;
using System.Collections.Generic;
using System.IO;
using BnsBinTool.Core.Sources;
using BnsBinTool.Core.Serialization;

namespace BnsBinTool.Core.Models
{
    public class Datafile : DatafileHeader
    {
        public static Datafile ReadFromFile(string datafilePath, bool lazy = true, bool is64Bit = false)
        {
            if (!File.Exists(datafilePath))
                throw new FileNotFoundException("Failed to find specified datafile", nameof(datafilePath));

            var source = new FileSource(datafilePath);

            return lazy
                ? DatafileReader.Lazy(source, is64Bit).ReadFrom(source)
                : is64Bit
                    ? DatafileReader.Default64.ReadFrom(source)
                    : DatafileReader.Default.ReadFrom(source);
        }

        public static Datafile ReadFromBytes(byte[] bytes, bool lazy = true, bool is64Bit = false)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var source = new ByteArraySource(bytes);

            return lazy
                ? DatafileReader.Lazy(source, is64Bit).ReadFrom(source)
                : is64Bit
                    ? DatafileReader.Default64.ReadFrom(source)
                    : DatafileReader.Default.ReadFrom(source);
        }

        public byte[] ToArray()
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);

            DatafileWriter.Default.WriteTo(writer, this);

            return memoryStream.ToArray();
        }

        public void WriteToFile(string outputPath)
        {
            using var writer = new BinaryWriter(new MemoryStream());

            DatafileWriter.Default.WriteTo(writer, this);

            using var outputStream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);

            writer.BaseStream.Seek(0, SeekOrigin.Begin);
            writer.BaseStream.CopyTo(outputStream);
        }

        public NameTable NameTable { get; set; }
        public List<Table> Tables { get; } = new List<Table>();
        public bool Is64Bit { get; set; }
    }
}