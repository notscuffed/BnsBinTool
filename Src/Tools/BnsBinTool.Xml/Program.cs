using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BnsBinTool.Core;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Exceptions;
using BnsBinTool.Core.Models;
using BnsBinTool.Xml.Converters;
using BnsBinTool.Xml.Helpers;

namespace BnsBinTool.Xml
{
    public static class Program
    {
        private static readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(true);
        private static Dictionary<string, string> _flagValue;
        private static List<string> _flags;

        private static void ShowHelp(bool dontExit = false)
        {
            var stream = typeof(Program).Assembly.GetManifestResourceStream("BnsBinTool.Xml.help.txt");

            if (stream == null)
                throw new Exception("Failed to find help resource");

            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                Console.WriteLine(reader.ReadLine());
            }

            if (!dontExit)
                Environment.Exit(1);
        }

        private const int DUMP_DATAFILE_PATH_ID = 0;
        private const int DUMP_LOCALFILE_PATH_ID = 1;
        private const int DUMP_DEFINITIONS_PATH_ID = 2;
        private const int DUMP_ARG_COUNT = 3;

        private const int COMPILE_PROJECT_PATH_ID = 0;
        private const int COMPILE_DEFINITIONS_PATH_ID = 1;
        private const int COMPILE_ARG_COUNT = 2;

        private const int TRANSFORM_PATCHES_PATH_ID = 0;
        private const int TRANSFORM_DEFINITIONS_PATH_ID = 1;

        private const int VALIDATE_XML_PATH_ID = 0;
        private const int VALIDATE_DEFINITIONS_PATH_ID = 1;

        public static int Main(string[] args)
        {
            args = InitializeArgs(args);

            if (args.Length < 1)
                ShowHelp();

            Logger.Minimal = _flags.Contains("nospam");

            var detailedExceptions = _flags.Contains("exceptions");

            try
            {
                switch (args[0])
                {
                    case "dump":
                        return Dump(args[1..]);

                    case "compile":
                        return Compile(args[1..]);

                    case "transform":
                        return Transform(args[1..]);

                    case "validate":
                        Logger.Minimal = true;
                        return Validate(args[1..]);


                    default:
                        ShowHelp();
                        return 1;
                }
            }
            catch (BnsException e)
            {
                if (detailedExceptions)
                    throw;

                Logger.LogTime($"\r\nException has occured:\r\n{e.Message}");
                return 1;
            }
            catch (Exception e)
            {
                if (detailedExceptions)
                    throw;

                Logger.LogTime($"\r\nException has occured:\r\n{e}");
                return 1;
            }
        }

        public static int Compile(string[] args)
        {
            if (args.Length < COMPILE_ARG_COUNT)
                ShowHelp();

            var projectRoot = Path.GetFullPath(args[COMPILE_PROJECT_PATH_ID]);

            if (!Directory.Exists(projectRoot))
            {
                Logger.Log($"Project root folder not found: '{projectRoot}'");
                return 1;
            }

            var definitionsPath = args[COMPILE_DEFINITIONS_PATH_ID];
            if (!Directory.Exists(definitionsPath))
            {
                Logger.Log($"Definitions folder not found: '{definitionsPath}'");
                return 1;
            }

            Logger.Log($"Project root: '{projectRoot}'");

            var compileOnce = _flags.Contains("once");
            var compileOnAllExtensions = _flags.Contains("watchall");

            if (!_flagValue.TryGetValue("delay", out var delayString) || !int.TryParse(delayString, out var delay))
                delay = 200;

            delay = Math.Max(0, delay);

            _debounced = Debouncer.Debounce((FileSystemEventArgs e) =>
            {
                _autoResetEvent.Set();
                Logger.Log($"Project file change detected: {e.ChangeType} {e.Name}");
            }, TimeSpan.FromMilliseconds(delay));

            _flagValue.TryGetValue("xml", out var extractedXmlDatPath);
            _flagValue.TryGetValue("local", out var extractedLocalDatPath);

            var definitions = DatafileDefinition.Load(definitionsPath);
            var xmlToDatafile = new XmlToDatafileConverter(projectRoot, definitions, extractedXmlDatPath, extractedLocalDatPath);

            var watcher = new FileSystemWatcher(projectRoot + "\\")
            {
                IncludeSubdirectories = true,
                Filter = compileOnAllExtensions ? "*.*" : "*.xml",
                EnableRaisingEvents = !compileOnce
            };

            watcher.Changed += WatcherOnChanged;
            watcher.Created += WatcherOnChanged;
            watcher.Deleted += WatcherOnChanged;
            watcher.Renamed += WatcherOnChanged;

            while (_autoResetEvent.WaitOne())
            {
                if (Debugger.IsAttached)
                {
                    xmlToDatafile.ConvertXmlsToDatafile();
                    if (compileOnce)
                        return 0;
                }
                else
                {
                    try
                    {
                        xmlToDatafile.ConvertXmlsToDatafile();
                        if (compileOnce)
                            return 0;
                    }
                    catch (Exception exception)
                    {
                        Logger.Log(exception.ToString());
                        if (compileOnce)
                            return 1;
                    }
                }
            }

            return 0;
        }

        public static int Dump(string[] args)
        {
            if (args.Length < DUMP_ARG_COUNT)
                ShowHelp();

            var datafilePath = args[DUMP_DATAFILE_PATH_ID];
            var localfilePath = args[DUMP_LOCALFILE_PATH_ID];

            if (!File.Exists(datafilePath))
            {
                Console.WriteLine($"datafile.bin not found: '{datafilePath}'");
                return 1;
            }

            if (!File.Exists(localfilePath))
            {
                Console.WriteLine($"localfile.bin not found: '{localfilePath}'");
                return 1;
            }

            var definitionsPath = args[DUMP_DEFINITIONS_PATH_ID];
            if (!Directory.Exists(definitionsPath))
            {
                Console.WriteLine($"Definitions folder not found: '{definitionsPath}'");
                return 1;
            }

            if (!_flagValue.TryGetValue("output", out var outputPath)
                && !_flagValue.TryGetValue("out", out outputPath)
                && !_flagValue.TryGetValue("o", out outputPath))
                outputPath = ".\\Output";

            _flagValue.TryGetValue("xml", out var extractedXmlDatPath);
            _flagValue.TryGetValue("local", out var extractedLocalDatPath);
            var prependDefComment = !_flags.Contains("nodef");
            
            var noValidate = _flags.Contains("novalidate");

            var definitions = DatafileDefinition.Load(definitionsPath);
            var datafileToXml = new DatafileToXmlConverter(definitions, extractedXmlDatPath, extractedLocalDatPath, noValidate);

            var is64Bit = _flags.Contains("64");

            var data = Datafile.ReadFromFile(datafilePath, is64Bit: is64Bit);
            var local = Datafile.ReadFromFile(localfilePath, is64Bit: is64Bit);

            if (_flagValue.TryGetValue("only", out var onlyTablesString))
            {
                var onlyTables = onlyTablesString.Split(',');
                datafileToXml.ConvertDatafilesToXml(data, local, outputPath, onlyTables, writeDefsComment: prependDefComment);
            }
            else
            {
                datafileToXml.ConvertDatafilesToXml(data, local, outputPath, writeDefsComment: prependDefComment);
            }

            return 0;
        }

        public static int Transform(string[] args)
        {
            if (args.Length < 4)
                ShowHelp();

            if (args.Length % 2 > 0)
            {
                ShowHelp(true);
                Console.WriteLine();
                Console.WriteLine("For every datafile.bin input there has to be an output path");
                return 1;
            }

            var datafileCount = (args.Length - 2) / 2;

            var definitionsPath = args[TRANSFORM_DEFINITIONS_PATH_ID];
            if (!Directory.Exists(definitionsPath))
            {
                Console.WriteLine($"Definitions folder not found: '{definitionsPath}'");
                return 1;
            }

            var is64Bit = _flags.Contains("64");

            var datafilePaths = args[2..(2 + datafileCount)];
            foreach (var datafilePath in datafilePaths)
            {
                if (!File.Exists(datafilePath))
                {
                    Console.WriteLine($"Datafile not found: '{datafilePath}'");
                    return 1;
                }
            }

            _flagValue.TryGetValue("xml", out var extractedXmlDatPath);
            _flagValue.TryGetValue("local", out var extractedLocalDatPath);
            var rebuildAutoTables = _flags.Contains("rebuildautotables");

            var definitions = DatafileDefinition.Load(definitionsPath);

            var datafileTransformer = new DatafileTransformer(definitions);

            datafileTransformer.Transform(
                datafilePaths,
                args[(2 + datafileCount)..].ToArray(),
                args[TRANSFORM_PATCHES_PATH_ID],
                is64Bit,
                extractedXmlDatPath,
                extractedLocalDatPath,
                rebuildAutoTables);

            return 0;
        }

        public static int Validate(string[] args)
        {
            if (args.Length != 2)
            {
                ShowHelp();
                return 1;
            }

            var xmlPath = args[VALIDATE_XML_PATH_ID];
            if (!File.Exists(xmlPath))
            {
                Console.WriteLine($"Xml file doesn't exist: '{xmlPath}'");
                return 1;
            }

            var definitionsPath = args[VALIDATE_DEFINITIONS_PATH_ID];
            if (!Directory.Exists(definitionsPath))
            {
                Console.WriteLine($"Definitions folder not found: '{definitionsPath}'");
                return 1;
            }

            var definitions = DatafileDefinition.Load(definitionsPath);

            var xmlValidator = new XmlValidator(definitions);
            xmlValidator.ValidateXmlFile(Path.GetFullPath(xmlPath), Path.GetFileName(xmlPath));

            return 0;
        }

        private static void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            _debounced(e);
        }

        private static string[] InitializeArgs(string[] args)
        {
            _flagValue = args
                .Where(x => x[0] == '-' && x.IndexOf('=') > 0)
                .ToDictionary(
                    x => x[1..x.IndexOf('=')].ToLower(),
                    x => x[(x.IndexOf('=') + 1)..]);

            _flags = args
                .Where(x => x[0] == '-' && x.IndexOf('=') == -1)
                .Select(x => x[1..].ToLowerInvariant())
                .ToList();

            return args.Where(x => x[0] != '-').ToArray();
        }

        private static Action<FileSystemEventArgs> _debounced;
    }
}