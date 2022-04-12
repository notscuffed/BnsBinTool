using System;
using System.Linq;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Info
{
    internal class Program
    {
        private static readonly string[] _trueValues = {"true", "t", "y", "yes", "1"};

        internal static int Main(string[] args)
        {
            var flags = args.Where(x => x[0] == '-').Select(x => x[1] == '-' ? x[2..].ToLower() : x[1..].ToLower()).ToArray();
            args = args.Where(x => x[0] != '-').ToArray();
            
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Prints stats for datafile");
                Console.WriteLine("Usage: bnsinfo [datafile path] (print table infos=False)");
                Console.WriteLine("");
                Console.WriteLine("Flags:\r\n" +
                                  "--64  - 64-bit version");
                return 1;
            }

            var printTableInfos = false;

            if (args.Length == 2)
                printTableInfos = _trueValues.Contains(args[1], StringComparer.OrdinalIgnoreCase);
            
            var datafile = Datafile.ReadFromFile(args[0], is64Bit:flags.Contains("64"));

            Console.WriteLine($"Datafile{(datafile.Is64Bit ? "64" : "32")}: {args[0]}\r\n" +
                              $"    Magic: {datafile.Magic}\r\n" +
                              $"    CreatedAt: {datafile.CreatedAt:u}\r\n" +
                              $"    Table count: {datafile.Tables.Count}\r\n" +
                              $"    Datafile version: {datafile.DatafileVersion}\r\n" +
                              $"    Version: {string.Join('.', datafile.ClientVersion)}\r\n" +
                              $"    TotalTablSize: {datafile.TotalTableSize / 1024 / 1024} MiB\r\n" +
                              $"    AliasMapSize: {datafile.AliasMapSize / 1024 / 1024} MiB\r\n" +
                              $"    AliasCount: {datafile.AliasCount}\r\n" +
                              $"    MaxBufferSize: {datafile.MaxBufferSize} bytes ({datafile.MaxBufferSize / 1024 / 1024} MiB)\r\n");

            if (!printTableInfos)
                return 0;

            foreach (var table in datafile.Tables)
            {
                Console.WriteLine($"Table {table.Type}\r\n" +
                                  $"    IsCompressed: {table.IsCompressed}\r\n" +
                                  $"    Version: {table.MajorVersion}.{table.MinorVersion}\r\n" +
                                  $"    El. Count: {table.ElementCount}\r\n" +
                                  $"    Size: 0x{table.Size:X} ({table.Size})");

                var recordTypes = table.Records.GroupBy(x => x.SubclassType)
                    .Select(x => x.Key)
                    .OrderBy(x => x)
                    .ToArray();
                var recordSizes = table.Records.GroupBy(x => x.DataSize)
                    .Select(x => x.Key)
                    .OrderBy(x => x)
                    .ToArray();

                Console.WriteLine($"    Types: {string.Join(", ", recordTypes)}");
                Console.WriteLine($"    Type sizes: {string.Join(", ", recordSizes)}");
                
                Console.WriteLine();
            }

            return 0;
        }
    }
}