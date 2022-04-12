using System;
using System.IO;
using System.Text;

namespace BnsBinTool.StringDumper
{
    public class Program
    {
        // Dumps all *.strings in specified directory to stdout
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Dump strings from *.strings to console");
                Console.WriteLine("Usage: bnsds [input directory]");
                return;
            }
            
            var directory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

            foreach (var file in Directory.EnumerateFiles(directory, "*.strings",
                SearchOption.TopDirectoryOnly))
            {
                var bytes = File.ReadAllBytes(file);

                var strings = Encoding.Unicode.GetString(bytes).Split('\0', StringSplitOptions.RemoveEmptyEntries);

                if (strings.Length == 0)
                    continue;


                Console.WriteLine("# " + file);


                foreach (var str in strings)
                {
                    Console.WriteLine(str);
                }

                Console.WriteLine();
            }
        }
    }
}