using System;
using System.IO;

namespace BnsBinTool.Core.Models
{
    public unsafe class Record
    {
        public byte XmlNodeType
        {
            get
            {
                fixed (byte* ptr = Data) return ((byte*) ptr)[0];
            }
            set
            {
                fixed (byte* ptr = Data) ((byte*) ptr)[0] = value;
            }
        }

        public short SubclassType
        {
            get
            {
                fixed (byte* ptr = Data) return ((short*) (ptr + 2))[0];
            }
            set
            {
                fixed (byte* ptr = Data) ((short*) (ptr + 2))[0] = value;
            }
        }

        public ushort DataSize
        {
            get
            {
                fixed (byte* ptr = Data) return ((ushort*) (ptr + 4))[0];
            }
            set
            {
                fixed (byte* ptr = Data) ((ushort*) (ptr + 4))[0] = value;
            }
        }

        public virtual int RecordId
        {
            get
            {
                fixed (byte* ptr = Data) return ((int*) (ptr + 8))[0];
            }
            set
            {
                fixed (byte* ptr = Data) ((int*) (ptr + 8))[0] = value;
            }
        }

        public virtual int RecordVariationId
        {
            get
            {
                fixed (byte* ptr = Data) return ((int*) (ptr + 12))[0];
            }
            set
            {
                fixed (byte* ptr = Data) ((int*) (ptr + 12))[0] = value;
            }
        }

        public byte[] Data { get; set; }
        public StringLookup StringLookup { get; set; }
        
        public int SizeWithLookup
        {
            get
            {
                var size = Data.Length;

                if (StringLookup?.Data != null)
                    size += StringLookup.Data.Length;

                return size;
            }
        }

        public static Record ReadFrom(BinaryReader reader)
        {
            var record = new Record();

            reader.ReadInt32();
            var size = reader.ReadUInt16();

            reader.BaseStream.Seek(-6, SeekOrigin.Current);

            record.Data = reader.ReadBytes(size);

            return record;
        }

        public T As<T>() where T : Record, new()
        {
            return new T
            {
                Data = Data,
                StringLookup = StringLookup
            };
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Data);
        }

        public Record Duplicate()
        {
            var data = new byte[Data.Length];
            Array.Copy(Data, data, data.Length);
            
            var record = new Record {Data = data};

            if (StringLookup != null && !StringLookup.IsPerTable)
                record.StringLookup = StringLookup.Duplicate();

            return record;
        }
    }
}