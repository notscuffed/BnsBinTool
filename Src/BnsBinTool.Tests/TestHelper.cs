using System;
using System.IO;
using NUnit.Framework;

namespace BnsBinTool.Tests
{
    public class TestHelper
    {
        static TestHelper()
        {
            BasePath = AppContext.BaseDirectory.Split(
                new[] {"BnsBinTool.Tests"},
                StringSplitOptions.RemoveEmptyEntries)[0] + "BnsBinTool.Tests";
        }

        public static string BasePath { get; }

        public static byte[] GetTestFileBytes(string testFileName)
        {
            var path = Path.Combine(BasePath, "TestData", testFileName);
            return File.ReadAllBytes(path);
        }

        public static void AreArraysEqual(byte[] expected, byte[] actual)
        {
            var length = Math.Min(expected.Length, actual.Length);

            for (var i = 0; i < length; i++)
            {
                if (expected[i] == actual[i])
                    continue;

                throw new Exception(
                    $"Array is not the same at index: {i}   0x{i:X8}\r\n" +
                    $"Expected: 0x{expected[i]:X2}\r\n" +
                    $"Actual: 0x{actual[i]:X2}\r\n" +
                    (expected.Length != actual.Length
                        ? $"Lengths are not equal as well: {actual.Length} != {expected.Length}"
                        : "Lengths are equal"));
            }

            Assert.AreEqual(expected.Length, actual.Length,
                "Actual bytes length does not equal input bytes length");
        }
    }
}