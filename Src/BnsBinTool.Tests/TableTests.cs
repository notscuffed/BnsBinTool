using System.Collections.Generic;
using System.IO;
using BnsBinTool.Core.Models;
using BnsBinTool.Core.Serialization;
using NUnit.Framework;

namespace BnsBinTool.Tests
{
    [TestFixture]
    public class TableTests
    {
        public static IEnumerable<string> TestData()
        {
            var directory = Path.Combine(TestHelper.BasePath, "TestData", "Tables");

            foreach (var file in Directory.EnumerateFiles(directory, "*.bin", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        [SetUp]
        public void SetUp()
        {
            TableWriter.GlobalCompressionBlockSize = 0x3FFF;
        }

        [TestCaseSource(nameof(TestData))]
        public void Repack(string filePath)
        {
            var originalBytes = File.ReadAllBytes(filePath);
            var table = Table.ReadFromBytes(originalBytes);

            var bytes = table.ToArray(is64Bit:false);

            var table2 = Table.ReadFromBytes(bytes);

            for (var i = 0; i < table.Records.Count; i++)
            {
                TestHelper.AreArraysEqual(table.Records[i].Data, table2.Records[i].Data);
                TestHelper.AreArraysEqual(table.Records[i].StringLookup.Data, table2.Records[i].StringLookup.Data);
            }
        }
    }
}