using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BnsBinTool.Core;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Xml.Helpers
{
    public class AutoTableFixer
    {
        private readonly DatafileDefinition _datafileDef;
        private readonly AutoTableRebuilder _rebuilder;

        public AutoTableFixer(DatafileDefinition datafileDef)
        {
            _datafileDef = datafileDef;
            _rebuilder = new AutoTableRebuilder(datafileDef);
        }

        public void Fix(Table[] tables, string rootPath)
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.autoids.txt", SearchOption.AllDirectories))
            {
                if (_datafileDef.TryGetValue(Path.GetFileNameWithoutExtension(file).Replace("data.", ".").Replace(".autoids", ""), out var tableDef))
                {
                    var table = tables.FirstOrDefault(x => x.Type == tableDef.Type);

                    if (table == null)
                        ThrowHelper.ThrowException($"Failed to get table for tabledef: {tableDef.Name}");

                    FixFile(file, tables, table, tableDef);
                }
                else
                {
                    ThrowHelper.ThrowException($"Failed to find table definition for table: {tableDef.Name}");
                }
            }
        }

        private void FixFile(string path, Table[] tables, Table table, TableDefinition tableDef)
        {
            Logger.LogTime($"Fixing auto table {tableDef.Name}");

            if (!tableDef.AutoKey)
                ThrowHelper.ThrowException($"Table {tableDef.Name} is not autotable");

            if (tableDef.Attributes.All(x => x.Name != "alias"))
                ThrowHelper.ThrowException($"Table {tableDef.Name} does not have an alias attribute");

            using var reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

            var aliasMap = new Dictionary<string, Record>();

            foreach (var record in table.Records)
            {
                var alias = record.StringLookup.GetString(record.Get<int>(0x10));

                if (aliasMap.ContainsKey(alias))
                {
                    Logger.LogTime($"Duplicate alias '{alias}' in table: '{tableDef.Name}'");
                    continue;
                }

                aliasMap[alias] = record;
            }

            _rebuilder.CustomClearMapping();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var split = line.Split(' ', 2);

                var newAutoId = ulong.Parse(split[0]);
                var alias = split[1];

                if (!aliasMap.TryGetValue(alias, out var record))
                {
                    Logger.LogTime($"Failed to find record with alias '{alias}' in table: '{tableDef.Name}'");
                    continue;
                }

                _rebuilder.CustomAddMapping(record.Get<ulong>(8), newAutoId);
                record.Set(8, newAutoId);
            }

            _rebuilder.CustomRebuildForTable(tables, table);

            /*
            var dictionary = new Dictionary<ulong, Record>();
            var highest = 0ul;
            foreach (var record in table.Records)
            {
                var autoId = record.Get<ulong>(8);
                dictionary[autoId] = record;
                highest = Math.Max(autoId, highest);
            }

            for (ulong i = 1; i <= highest; i++)
            {
                if (!dictionary.ContainsKey(i))
                {
                    var record = new Record
                    {
                        Data = new byte[6],
                        DataSize = 20,
                        SubclassType = -1
                    };

                    record.Set(8, i);
                    dictionary[i] = record;
                }
            }
        */
        }
    }
}