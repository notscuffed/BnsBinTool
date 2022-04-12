using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BnsBinTool.Core.Models;

namespace BnsBinTool.BinDumper
{
    internal class Program
    {
        private static string _outputPath;

        public static int Main(string[] args)
        {
            var flags = args.Where(x => x[0] == '-').Select(x => x[1..].ToLower()).ToArray();
            args = args.Where(x => x[0] != '-').ToArray();

            var tablesOnly = flags.Contains("tables")
                             || flags.Contains("table")
                             || flags.Contains("tableonly")
                             || flags.Contains("tablesonly");

            if (args.Length < 2 || args.Length > 4)
            {
                Console.WriteLine("Dump all records and string lookups to output directory from specified datafila\r\n" +
                                  "Usage: bnsdb [datafile path] [output directory] (begin table type) (inclusive end table type)\r\n" +
                                  "\r\n" +
                                  "Switches:\r\n" +
                                  "    -tables        Dump only raw tables\r\n");

                return 1;
            }

            _outputPath = args[1];

            var datafile = Datafile.ReadFromFile(args[0]);

            Directory.CreateDirectory(Path.Combine(_outputPath, "Tables"));

            if (args.Length == 2)
            {
                Parallel.ForEach(datafile.Tables, tablesOnly ? (Action<Table>) DumpTable : DumpTableWithRecords);
                Console.WriteLine("Done");
                return 0;
            }

            if (!int.TryParse(args[2], out var minTableType))
            {
                Console.WriteLine("Invalid min table type integer");
                return 0;
            }

            var maxTableType = minTableType;

            if (args.Length == 4 && !int.TryParse(args[3], out maxTableType))
            {
                Console.WriteLine("Invalid max table type integer");
                return 0;
            }
            
            Parallel.ForEach(datafile.Tables.Where(x => x.Type >= minTableType && x.Type <= maxTableType),
                tablesOnly ? (Action<Table>) DumpTable : DumpTableWithRecords);
            
            Console.WriteLine("Done");

            return 0;
        }

        private static void DumpTable(Table table)
        {
            if (!(table is LazyTable lazyTable))
                throw new Exception("Expected lazy table");

            var outputPath = Path.Combine(_outputPath, "Tables", $"{table.Type}.bin");

            using var outputStream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var lazyTableStream = lazyTable.Source.CreateStream();

            lazyTableStream.CopyTo(outputStream);

            Console.WriteLine($"Written table {table.Type}");
        }

        private static void DumpTableWithRecords(Table table)
        {
            var path = Path.Combine(_outputPath, "Tables", $"{table.Type}");

            Directory.CreateDirectory(path);

            if (table.Records.Count == 0)
                return;

            var firstRecord = table.Records.First();
            var lastRecord = table.Records.Last();

            var areSame = firstRecord != lastRecord
                          && firstRecord.StringLookup != null
                          && firstRecord.StringLookup == lastRecord.StringLookup;

            if (areSame && firstRecord.StringLookup.Data != null)
                File.WriteAllBytes(Path.Combine(path, "local.strings"), firstRecord.StringLookup.Data);

            foreach (var record in table.Records)
            {
                var variationId = record.RecordVariationId;

                var nameFormat = variationId == 0 ? "{0}.{1}" : "{0}_{2}.{1}";

                File.WriteAllBytes(Path.Combine(path, string.Format(nameFormat, record.RecordId, "bin", variationId)),
                    record.Data);

                if (!areSame && record.StringLookup?.Data != null)
                    File.WriteAllBytes(
                        Path.Combine(path, string.Format(nameFormat, record.RecordId, "strings", variationId)),
                        record.StringLookup.Data);
            }

            Console.WriteLine($"Written table {table.Type}");
        }
    }
}