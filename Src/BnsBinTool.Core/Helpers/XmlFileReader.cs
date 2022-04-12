using System.Xml;

namespace BnsBinTool.Core.Helpers
{
    public class XmlFileReader : XmlTextReader
    {
        public string FilePath { get; }
        public string FullFilePath { get; }
        
        public XmlFileReader(string fullFilePath, string filePath) : base(fullFilePath)
        {
            FilePath = filePath;
            FullFilePath = fullFilePath;
            WhitespaceHandling = WhitespaceHandling.None;
        }
    }
}