using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BnsBinTool.Core.Definitions;

namespace BnsBinTool.DefsToSharp
{
    public class Program
    {
        public static string FixName(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                return string.Concat(
                    input.Split(new[] {'-', '_', '.', ' '}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => char.ToUpper(x[0]) + x[1..])
                );
            }

            return input;
        }

        private static int PrintUsage()
        {
            var processName = Process.GetCurrentProcess().ProcessName;

            Console.WriteLine("Converts definitions to C# code");
            Console.WriteLine($"Usage: {processName} [definitions path] [namespace] (output path)");
            Console.WriteLine("Flags:");
            Console.WriteLine("--64           - 64-bit definition");
            Console.WriteLine("--alias-table  - Use alias table instead of dictionary");

            return 1;
        }

        public static int Main(string[] args)
        {
            var flags = args.Where(x => x[0] == '-').Select(x => x[1] == '-' ? x[2..].ToLower() : x[1..].ToLower()).ToArray();
            args = args.Where(x => x[0] != '-').ToArray();

            if (args.Length < 2 || args.Length > 3)
                return PrintUsage();

            var outputPath = ".\\Output";
            var defsPath = args[0];
            var @namespace = args[1];

            if (args.Length == 3)
                outputPath = args[2];

            if (!Directory.Exists(defsPath))
            {
                Console.WriteLine($"Defiinitions path '{defsPath}' doesn't exist");
                return 1;
            }

            Directory.CreateDirectory(outputPath);

            var is64Bit = flags.Contains("64");
            
            var datafileDef = DatafileDefinition.Load(defsPath, is64Bit:is64Bit);

            var definitionNameFixer = new DefinitionNameFixer(
                x => FixName(x) + "Record",
                (p, s) => FixName(p) + FixName(s) + "Record",
                FixName,
                x => "E" + FixName(x),
                x =>
                {
                    if (char.IsDigit(x[0]))
                        x = "N" + x;

                    return FixName(x);
                });

            definitionNameFixer.Fix(datafileDef);

            var definitionGenerator = new DefinitionGenerator(
                datafileDef,
                outputPath,
                new DefinitionDeserializerGenerator(),
                new DefinitionSerializerGenerator(),
                new DefinitionTablesClassGenerator {EnableAliasTable = flags.Contains("alias-table")},
                new DefinitionResolverGenerator(datafileDef),
                new SequenceTranslator());

            definitionGenerator.Generate(@namespace);

            Console.WriteLine($"Processed {datafileDef.TableDefinitions.Count} table definitions to {outputPath} in namespace {@namespace}.");

            return 0;
        }
    }
}