using System.IO;
using System.Runtime.InteropServices;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Sources;

namespace BnsBinTool.Core.Models
{
    public class BinTerrain
    {
        // Offsets in names are relative to file beginning (including version)
        public short Version { get; init; }
        public int Unk06 { get; set; }
        public short ZoneId { get; set; }
        public short UnkC0 { get; set; }
        public byte[] UnkE0 { get; set; }
        public short SectorCntX { get; set; }
        public short SectorCntY { get; set; }
        public int Unk26 { get; set; }

        public TerrainSector[] Sectors { get; set; }
        public long Data1Attribute { get; set; }
        public byte[] Data2 { get; set; }
        public long Data2Attribute { get; set; }
        public byte[] Data3 { get; set; }
        public long Data3Attribute { get; set; }
        public byte[] Data4 { get; set; }
        public long Data4Attribute { get; set; }
        public byte[] Data5 { get; set; }
        public long Data5Attribute { get; set; }

        public static BinTerrain ReadFrom(ISource source)
        {
            using var reader = source.CreateReader();

            var binTerrain = new BinTerrain
            {
                Version = reader.ReadInt16()
            };

            reader.ReadInt32(); // size

            binTerrain.Unk06 = reader.ReadInt32();
            binTerrain.ZoneId = reader.ReadInt16();
            binTerrain.UnkC0 = reader.ReadInt16();
            binTerrain.UnkE0 = reader.ReadBytes(0x14);
            binTerrain.SectorCntX = reader.ReadInt16();
            binTerrain.SectorCntY = reader.ReadInt16();
            binTerrain.Unk26 = reader.ReadInt32();

            var offset1 = reader.ReadInt64();
            binTerrain.Data2Attribute = reader.ReadInt64();
            var offset2 = reader.ReadInt64();
            binTerrain.Data3Attribute = reader.ReadInt64();
            var offset3 = reader.ReadInt64();
            binTerrain.Data4Attribute = reader.ReadInt64();
            var offset4 = reader.ReadInt64();
            binTerrain.Data5Attribute = reader.ReadInt64();
            var offset5 = reader.ReadInt64();

            reader.BaseStream.Seek(2 + offset1, SeekOrigin.Begin);
            var bytes = reader.ReadBytes((int) (offset2 - offset1));
            binTerrain.Sectors = MemoryMarshal.Cast<byte, TerrainSector>(bytes).ToArray();

            reader.BaseStream.Seek(2 + offset2, SeekOrigin.Begin);
            binTerrain.Data2 = reader.ReadBytes((int) (offset3 - offset2));

            reader.BaseStream.Seek(2 + offset3, SeekOrigin.Begin);
            binTerrain.Data3 = reader.ReadBytes((int) (offset4 - offset3));

            reader.BaseStream.Seek(2 + offset4, SeekOrigin.Begin);
            binTerrain.Data4 = reader.ReadBytes((int) (offset5 - offset4));

            reader.BaseStream.Seek(2 + offset5, SeekOrigin.Begin);
            binTerrain.Data5 = reader.ReadBytes((int) (reader.BaseStream.Length - offset5));

            return binTerrain;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Version);

            var length = Sectors.Length * 12
                         + Data2.Length
                         + Data3.Length
                         + Data4.Length
                         + Data5.Length;
            
            writer.Write(0x70 + length); // size

            writer.Write(Unk06);
            writer.Write(ZoneId);
            writer.Write(UnkC0);
            writer.Write(UnkE0);
            writer.Write(SectorCntX);
            writer.Write(SectorCntY);
            writer.Write(Unk26);

            // first offset always starts at 0x70 (excludes version field for offset)
            var position = 0x70L;
            writer.Write(position);
            writer.Write(Data2Attribute);

            position += Sectors.Length * 12;
            writer.Write(position);
            writer.Write(Data3Attribute);

            position += Data2.Length;
            writer.Write(position);
            writer.Write(Data4Attribute);

            position += Data3.Length;
            writer.Write(position);
            writer.Write(Data5Attribute);

            position += Data4.Length;
            writer.Write(position);

            writer.Write(MemoryMarshal.Cast<TerrainSector, byte>(Sectors));
            writer.Write(Data2);
            writer.Write(Data3);
            writer.Write(Data4);
            writer.Write(Data5);
        }
    }
}