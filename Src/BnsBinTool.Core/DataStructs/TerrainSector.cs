using System.Runtime.InteropServices;

namespace BnsBinTool.Core.DataStructs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainSector
    {
        public int SectorClass;
        public int Value1; // height map?
        public int Value2; // something with objects? (trees)
    }
}