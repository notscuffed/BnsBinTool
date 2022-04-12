using System.Collections.Generic;
using System.IO;
using BnsBinTool.Core.Models;
using BnsBinTool.Core.Sources;
using NUnit.Framework;

namespace BnsBinTool.Tests
{
    [TestFixture]
    public class BinTerrainTests
    {
        public static IEnumerable<string> TestData()
        {
            var directory = Path.Combine(TestHelper.BasePath, "TestData", "BinTerrains");

            foreach (var file in Directory.EnumerateFiles(directory, "*.cterrain", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
        
        [TestCaseSource(nameof(TestData))]
        public void Repack(string filePath)
        {
            var originalBytes = File.ReadAllBytes(filePath);
            var table = BinTerrain.ReadFrom(new MemorySource(originalBytes));

            var memoryStream = new MemoryStream();
            var binaryWriter = new BinaryWriter(memoryStream);
            table.WriteTo(binaryWriter);
            binaryWriter.Flush();

            TestHelper.AreArraysEqual(originalBytes, memoryStream.ToArray());
        }
    }
}