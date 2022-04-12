using System;
using System.Collections.Generic;
using System.IO;
using BnsBinTool.Core.Models;
using BnsBinTool.Core.Serialization;
using NUnit.Framework;

namespace BnsBinTool.Tests
{
    [TestFixture]
    public class DatafileTests
    {
        public static IEnumerable<string> TestData()
        {
            var directory = Path.Combine(TestHelper.BasePath, "TestData", "Datafiles");

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

        [TestCase(true)]
        [TestCase(false)]
        public void HeaderRepack(bool is64bit)
        {
            var now = DateTime.Now;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

            var datafileHeader = new DatafileHeader
            {
                Magic = "TADBOSLB",
                Reserved = new byte[58],
                TotalTableSize = 1, AliasMapSize = 2, AliasCount = 3, MaxBufferSize = 4,
                CreatedAt = now,
                DatafileVersion = 123
            };

            for (var i = 0; i < datafileHeader.Reserved.Length; i++)
            {
                datafileHeader.Reserved[i] = (byte) i;
            }

            var memoryStream = new MemoryStream();
            var setAliasMapSize = datafileHeader.WriteHeaderTo(new BinaryWriter(memoryStream), 256, 3, is64bit);
            setAliasMapSize(2);
            var result = memoryStream.ToArray();

            var readDatafileHeader = new DatafileHeader();
            readDatafileHeader.ReadHeaderFrom(new BinaryReader(new MemoryStream(result)), is64bit);

            Assert.AreEqual(datafileHeader.Magic, readDatafileHeader.Magic);
            Assert.AreEqual(datafileHeader.TotalTableSize, readDatafileHeader.TotalTableSize);
            Assert.AreEqual(datafileHeader.AliasMapSize, readDatafileHeader.AliasMapSize);
            Assert.AreEqual(datafileHeader.AliasCount, readDatafileHeader.AliasCount);
            Assert.AreEqual(datafileHeader.MaxBufferSize, readDatafileHeader.MaxBufferSize);
            Assert.AreEqual(datafileHeader.CreatedAt, readDatafileHeader.CreatedAt);
            Assert.AreEqual(datafileHeader.DatafileVersion, readDatafileHeader.DatafileVersion);
            Assert.AreEqual(256, readDatafileHeader.ReadTableCount);
            TestHelper.AreArraysEqual(datafileHeader.Reserved, readDatafileHeader.Reserved);
        }


        [TestCaseSource(nameof(TestData))]
        public void FullRepack(string filePath)
        {
            var originalBytes = File.ReadAllBytes(filePath);
            var datafile = Datafile.ReadFromBytes(originalBytes, false);

            var bytes = datafile.ToArray();

            TestHelper.AreArraysEqual(originalBytes, bytes);
        }

        [TestCaseSource(nameof(TestData))]
        public void FullLazyloadRepack(string filePath)
        {
            var originalBytes = File.ReadAllBytes(filePath);
            var datafile = Datafile.ReadFromBytes(originalBytes);

            var bytes = datafile.ToArray();

            TestHelper.AreArraysEqual(originalBytes, bytes);
        }

        [TestCaseSource(nameof(TestData))]
        public void FullLazyloadEvaluatedRepack(string filePath)
        {
            var originalBytes = File.ReadAllBytes(filePath);
            var datafile = Datafile.ReadFromBytes(originalBytes);

            foreach (var table in datafile.Tables)
            {
                _ = table.Records;
            }

            var bytes = datafile.ToArray();

            TestHelper.AreArraysEqual(originalBytes, bytes);
        }
    }
}