using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Models;

namespace BnsBinTool.Core.Helpers
{
    public class RecordBuilder
    {
        private readonly DatafileDefinition _definitions;
        private readonly ResolvedAliases _resolvedAliases;

        private readonly StringBuilder _stringBuilder = new StringBuilder(0x2000);
        private readonly Dictionary<string, int> _existingStringOffset = new Dictionary<string, int>(0x2000);

        // States
        private bool _isCompressed;

        public RecordBuilder(DatafileDefinition definitions, ResolvedAliases resolvedAliases)
        {
            _definitions = definitions;
            _resolvedAliases = resolvedAliases;
        }

        public StringLookup StringLookup { get; private set; }

        public void InitializeTable(bool isCompressed, StringLookup loadStringLookup = null)
        {
            _isCompressed = isCompressed;

            if (isCompressed)
                return;

            // Per table
            if (loadStringLookup == null)
            {
                StringLookup = new StringLookup {IsPerTable = true};

                _stringBuilder.Clear();
                _existingStringOffset.Clear();

                _stringBuilder.Append("\0");
                _existingStringOffset[""] = 0;
            }
            else
            {
                StringLookup = loadStringLookup;

                _stringBuilder.Clear();
                _existingStringOffset.Clear();

                var data = Encoding.Unicode.GetString(loadStringLookup.Data);

                if (data.Length * 2 != loadStringLookup.Data.Length)
                    ThrowHelper.ThrowException("Invalid loaded lookup length");

                // TODO: load offsets for existing strings
                _stringBuilder.Append(data);
            }
        }

        public void InitializeRecord()
        {
            if (_isCompressed)
            {
                // Per record
                StringLookup = new StringLookup();

                _stringBuilder.Clear();
                _stringBuilder.Append("\0");
                _existingStringOffset.Clear();
                _existingStringOffset[""] = 0;
            }
        }

        public void InitializeMutateRecord(Record record)
        {
            if (_isCompressed)
            {
                var stringLookup = record.StringLookup;
                StringLookup = stringLookup;

                _stringBuilder.Clear();
                _existingStringOffset.Clear();

                var data = Encoding.Unicode.GetString(stringLookup.Data);

                if (data.Length * 2 != stringLookup.Data.Length)
                    ThrowHelper.ThrowException("Invalid loaded lookup length");

                // TODO: load offsets for existing strings
                _stringBuilder.Append(data);
            }
        }

        public void SetDefaultStrings(Record record, IEnumerable<AttributeDefinition> defaultStringAttrsWithValue)
        {
            foreach (var attrDef in defaultStringAttrsWithValue)
            {
                SetString(record, attrDef, attrDef.DefaultValue);
            }
        }

        public void SetAttribute(Record record, AttributeDefinition attrDef, string value)
        {
            switch (attrDef.Type)
            {
                case AttributeType.TRef:
                    SetRef(record, attrDef, value);
                    break;

                case AttributeType.TIcon:
                    SetIcon(record, attrDef, value);
                    break;

                case AttributeType.TTRef:
                    SetTRef(record, attrDef, value);
                    break;

                case AttributeType.TNative:
                    SetNative(record, attrDef, value);
                    break;

                case AttributeType.TVector32:
                    SetVector32(record, attrDef, value);
                    break;

                case AttributeType.TVelocity:
                    SetVelocity(record, attrDef, value);
                    break;

                case AttributeType.TDistance:
                case AttributeType.TInt16:
                case AttributeType.TSub:
                    SetInt16(record, attrDef, value);
                    break;

                case AttributeType.TInt64:
                case AttributeType.TTime64:
                    SetInt64(record, attrDef, value);
                    break;

                case AttributeType.TInt32:
                case AttributeType.TMsec:
                    SetInt32(record, attrDef, value);
                    break;

                case AttributeType.TInt8:
                    SetInt8(record, attrDef, value);
                    break;

                case AttributeType.TFloat32:
                    SetFloat32(record, attrDef, value);
                    break;

                case AttributeType.TString:
                    SetString(record, attrDef, value);
                    break;

                case AttributeType.TBool:
                    SetBool(record, attrDef, value);
                    break;

                case AttributeType.TSeq:
                case AttributeType.TProp_seq:
                {
                    var seqIndex = (sbyte) attrDef.Sequence.IndexOf(value);
                    
                    if (seqIndex == -1)
                        ThrowHelper.ThrowException($"Invalid sequence value: '{value}'");
                    
                    record.Set(attrDef.Offset, seqIndex);
                    break;
                }

                case AttributeType.TSeq16:
                case AttributeType.TProp_field:
                {
                    var seqIndex = (short) attrDef.Sequence.IndexOf(value);
                    
                    if (seqIndex == -1)
                        ThrowHelper.ThrowException($"Invalid sequence value: '{value}'");
                    
                    record.Set(attrDef.Offset, seqIndex);
                    break;
                }

                case AttributeType.TScript_obj:
                    // Ignore
                    break;

                case AttributeType.TIColor:
                    SetIColor(record, attrDef, value);
                    break;

                case AttributeType.TBox:
                    SetBox(record, attrDef, value);
                    break;

                default:
                    ThrowHelper.ThrowException("Unknown type");
                    break;
            }
        }

        public void FinalizeMutateRecord()
        {
            if (_isCompressed)
            {
                // Per record
                StringLookup.Data = Encoding.Unicode.GetBytes(_stringBuilder.ToString());
            }
        }

        public void FinalizeRecord()
        {
            if (_isCompressed)
            {
                // Per record
                StringLookup.Data = Encoding.Unicode.GetBytes(_stringBuilder.ToString());
            }
        }

        public void FinalizeTable()
        {
            if (_isCompressed)
                return;

            // Per table
            StringLookup.Data = Encoding.Unicode.GetBytes(_stringBuilder.ToString());
        }

        private static void SetBox(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                var split = value.Split(',', 6);

                var x1 = short.Parse(split[0]);
                var y1 = short.Parse(split[1]);
                var z1 = short.Parse(split[2]);
                var x2 = short.Parse(split[3]);
                var y2 = short.Parse(split[4]);
                var z2 = short.Parse(split[5]);

                record.Set(attrDef.Offset, new Box(x1, y1, z1, x2, y2, z2));
                return;
            }

            ThrowHelper.ThrowException("Value was null");
        }

        private static void SetIColor(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                var split = value.Split(',', 3);

                var r = byte.Parse(split[0]);
                var g = byte.Parse(split[1]);
                var b = byte.Parse(split[2]);

                record.Set(attrDef.Offset, new IColor(r, g, b));
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DIColor);
        }

        private static void SetBool(Record record, AttributeDefinition attrDef, string value)
        {
            var boolean = value == null
                ? attrDef.AttributeDefaultValues.DBool
                : Constants.PositiveXmlValues.Contains(value); // TODO: optimize this

            record.Data[attrDef.Offset] = boolean ? (byte) 1 : (byte) 0;
        }

        private void SetString(Record record, AttributeDefinition attrDef, string value)
        {
            value ??= attrDef.AttributeDefaultValues.DString;

            if (!_existingStringOffset.TryGetValue(value, out var stringOffset))
            {
                stringOffset = _stringBuilder.Length * 2;
                _stringBuilder.Append(value);
                _stringBuilder.Append('\0');
                _existingStringOffset[value] = stringOffset;
            }

            record.Set(attrDef.Offset, stringOffset);
        }

        private static void SetFloat32(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                record.Set(attrDef.Offset, float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DFloat);
        }

        private static void SetInt64(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                record.Set(attrDef.Offset, long.Parse(value));
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DLong);
        }

        private static void SetInt32(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                record.Set(attrDef.Offset, int.Parse(value));
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DInt);
        }

        private static void SetInt16(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                record.Set(attrDef.Offset, short.Parse(value));
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DShort);
        }

        private static void SetInt8(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                record.Set(attrDef.Offset, sbyte.Parse(value));
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DByte);
        }

        private void SetRef(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                if (_resolvedAliases.ByAlias[attrDef.ReferedTable].TryGetValue(value, out var @ref))
                {
                    record.Set(attrDef.Offset, @ref);
                    return;
                }

                record.Set(attrDef.Offset, value.ToRef());
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DRef);
        }

        private void SetIcon(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                var colon = value.LastIndexOf(':');

                if (colon != -1)
                {
                    var split = new[] {value[..colon], value[(colon + 1)..]};
                    var i32 = int.Parse(split[1]);

                    if (_resolvedAliases.ByAlias[_definitions.IconTextureTableId].TryGetValue(split[0], out var @ref))
                    {
                        record.Set(attrDef.Offset, new IconRef(@ref.Id, @ref.Variant, i32));
                        return;
                    }

                    @ref = split[0].ToRef();
                    record.Set(attrDef.Offset, new IconRef(@ref.Id, @ref.Variant, i32));
                    return;
                }

                ThrowHelper.ThrowException($"Invalid icon reference string: '{value}'");
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DIconRef);
        }

        private void SetTRef(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                var split = value.Split(':', 2);

                if (split.Length == 2)
                {
                    var referedTableDef = _definitions[split[0]];

                    if (referedTableDef != null)
                    {
                        if (_resolvedAliases.ByAlias[referedTableDef.Type].TryGetValue(split[1], out var @ref))
                        {
                            record.Set(attrDef.Offset, new TRef(referedTableDef.Type, @ref.Id, @ref.Variant));
                            return;
                        }

                        @ref = split[1].ToRef();
                        record.Set(attrDef.Offset, new TRef(referedTableDef.Type, @ref.Id, @ref.Variant));
                        return;
                    }

                    ThrowHelper.ThrowException($"Invalid typed reference, refered table doesn't exist: '{split[0]}'");
                    return;
                }

                ThrowHelper.ThrowException($"Invalid typed reference string: '{value}'");
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DTRef);
        }

        private void SetNative(Record record, AttributeDefinition attrDef, string value)
        {
            value ??= "";

            var offset = _stringBuilder.Length * 2;
            _stringBuilder.Append(value);
            _stringBuilder.Append('\0');

            record.Set(attrDef.Offset, new Native(_stringBuilder.Length * 2 - offset, offset));
        }

        private static void SetVector32(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                var split = value.Split(',', 3);

                var x = int.Parse(split[0]);
                var y = int.Parse(split[1]);
                var z = int.Parse(split[2]);

                record.Set(attrDef.Offset, new Vector32(x, y, z));
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DVector32);
        }

        private static void SetVelocity(Record record, AttributeDefinition attrDef, string value)
        {
            if (value != null)
            {
                record.Set(attrDef.Offset, ushort.Parse(value));
                return;
            }

            record.Set(attrDef.Offset, attrDef.AttributeDefaultValues.DVelocity);
        }
    }
}