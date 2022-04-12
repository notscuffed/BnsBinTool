using System.IO;
using System.Text;
using BnsBinTool.Core.Serialization;
using NUnit.Framework;

namespace BnsBinTool.Tests
{
    [TestFixture]
    public class NameTableTests
    {
        [Test]
        public void Test()
        {
            var nameTableReader = new NameTableReader(false);

            var inputBytes = TestHelper.GetTestFileBytes("global_string_table.bin");
            var inputReader = new BinaryReader(new MemoryStream(inputBytes));
            var table = nameTableReader.ReadFrom(inputReader);

            Assert.AreEqual(inputBytes.Length, inputReader.BaseStream.Position,
                "Input stream has to be at end after reading");

            var outputMemoryStream = new MemoryStream();
            var nameTableWriter = new NameTableWriter();
            nameTableWriter.WriteTo(new BinaryWriter(outputMemoryStream, Encoding.Default, true), table, false);

            var outputBytes = outputMemoryStream.ToArray();

            Directory.CreateDirectory(Path.Combine(TestHelper.BasePath, "Output"));

            TestHelper.AreArraysEqual(inputBytes, outputBytes);
        }
    }
}