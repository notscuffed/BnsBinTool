using System.Collections.Generic;
using System.Linq;
using BnsBinTool.Core;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Xml.Helpers
{
    public class AutoTableRebuilder
    {
        private readonly DatafileDefinition _datafileDef;
        private readonly Dictionary<ulong, ulong> _mapping = new(100000);

        public AutoTableRebuilder(DatafileDefinition datafileDef)
        {
            _datafileDef = datafileDef;
        }

        public void RebuildDatafileAutoTableIndex(Table[] tables)
        {
            foreach (var table in tables)
            {
                if (!_datafileDef.TryGetValue(table.Type, out var tableDef))
                    ThrowHelper.ThrowException($"Failed to get table definition for table type: {table.Type}");

                if (!DoesTableNeedRebuilding(table, tableDef))
                    continue;

                Logger.LogTime($"Rebuilding auto table {tableDef.Name}");
                InitializeMappingAndRebuild(tables, table);
            }
        }

        public static bool DoesTableNeedRebuilding(Table table, TableDefinition tableDef)
        {
            if (!tableDef.AutoKey)
                return false;

            ulong expectedAutoId = 1;

            foreach (var record in table.Records)
            {
                var autoId = record.Get<ulong>(8);

                if (autoId != expectedAutoId)
                    return true;

                expectedAutoId++;
            }

            return false;
        }

        public void CustomClearMapping()
        {
            _mapping.Clear();
        }

        public void CustomAddMapping(ulong from, ulong to)
        {
            _mapping[from] = to;
        }

        public void CustomRebuildForTable(Table[] tables, Table currentTable)
        {
            RebuildForTable(tables, currentTable);
        }

        private void InitializeMappingAndRebuild(Table[] tables, Table currentTable)
        {
            _mapping.Clear();

            ulong expectedAutoId = 1;

            // Rebuild auto id's
            foreach (var record in currentTable.Records)
            {
                var autoId = record.Get<ulong>(8);

                // Map old auto id to new auto id
                _mapping[autoId] = expectedAutoId;

                // Set new auto id on record
                record.Set(8, expectedAutoId);

                expectedAutoId++;
            }

            RebuildForTable(tables, currentTable);
        }

        /// <summary>
        /// Expects autotable in currentTable
        /// </summary>
        private void RebuildForTable(Table[] tables, Table currentTable)
        {
            var totalMaxSubTables = _datafileDef.TableDefinitions.Max(x => x.Subtables.Count + 1);

            // Each list contains fully expanded attributes for each subtable
            // (Subtable ones contain base ones as well)
            // Index is offseted by 1
            // -1 => 0
            // 0 => 1
            var attrRefs = new List<AttributeDefinition>[totalMaxSubTables];
            var attrTRefs = new List<AttributeDefinition>[totalMaxSubTables];

            for (var i = 0; i < totalMaxSubTables; i++)
            {
                attrRefs[i] = new List<AttributeDefinition>();
                attrTRefs[i] = new List<AttributeDefinition>();
            }

            foreach (var table in tables)
            {
                if (!_datafileDef.TryGetValue(table.Type, out var tableDef))
                    ThrowHelper.ThrowException($"Failed to get table definition for table type: {table.Type}");

                var currentTableType = currentTable.Type;

                var maxSubTables = tableDef.Subtables.Count + 1;

                for (var i = 0; i < maxSubTables; i++)
                {
                    attrRefs[i].Clear();
                    attrTRefs[i].Clear();
                }

                // Get ref && tref attributes
                foreach (var attrDef in tableDef.ExpandedAttributes)
                    GetRefsAndTRefsAttributes(attrDef, currentTableType, attrRefs[0], attrTRefs[0]);

                // Get ref && tref attributes from subtables
                foreach (var subtableDef in tableDef.Subtables)
                {
                    foreach (var attrDef in subtableDef.ExpandedAttributes)
                        GetRefsAndTRefsAttributes(attrDef, currentTableType, attrRefs[subtableDef.SubclassType + 1], attrTRefs[subtableDef.SubclassType + 1]);
                }

                foreach (var record in table.Records)
                {
                    var subclassType = record.SubclassType + 1;

                    FixRecordReferences(record, currentTableType, tableDef, attrRefs[subclassType], attrTRefs[subclassType]);
                }
            }
        }

        private void GetRefsAndTRefsAttributes(AttributeDefinition attrDef, short currentTableType, List<AttributeDefinition> attrRefs, List<AttributeDefinition> attrTRefs)
        {
            if (attrDef.Type == AttributeType.TRef)
            {
                if (attrDef.ReferedTable != currentTableType)
                    return;

                attrRefs.Add(attrDef);
            }
            else if (attrDef.Type == AttributeType.TTRef)
            {
                attrTRefs.Add(attrDef);
            }
        }

        private void FixRecordReferences(Record record, short currentTableType, ITableDefinition tableDef, List<AttributeDefinition> attrRefs, List<AttributeDefinition> attrTRefs)
        {
            // Refs that point to currentTable
            foreach (var attrDef in attrRefs)
            {
                var oldAutoId = record.Get<ulong>(attrDef.Offset);

                if (oldAutoId == 0)
                    continue;

                if (_mapping.TryGetValue(oldAutoId, out var newAutoId))
                    record.Set(attrDef.Offset, newAutoId);
                else
                {
                    Logger.LogTime($"Found invalid reference in table {tableDef.Name} auto id: {oldAutoId}");
                    record.Set(attrDef.Offset, 0ul);
                }
            }

            // TRefs
            foreach (var attrDef in attrTRefs)
            {
                var referedTableType = record.Get<short>(attrDef.Offset);

                if (referedTableType != currentTableType)
                    continue;

                // TRef
                // 0x0-0x2 short type
                // 0x4-0xC ulong autoid
                var oldAutoId = record.Get<ulong>(attrDef.Offset + 4);

                if (oldAutoId == 0)
                    continue;

                if (_mapping.TryGetValue(oldAutoId, out var newAutoId))
                    record.Set(attrDef.Offset + 4, newAutoId);
                else
                {
                    Logger.LogTime($"Found invalid reference in table {tableDef.Name} auto id: {oldAutoId}");
                    record.Set(attrDef.Offset + 4, 0ul);
                }
            }
        }
    }
}