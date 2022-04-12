using System;
using System.IO;
using BnsBinTool.Core.Helpers;

namespace BnsBinTool.DefsToSharp
{
    public class CodeWriter : IDisposable
    {
        private const string OneTab = "    ";

        private readonly TextWriter _writer;
        private int _indent;

        public CodeWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public void BeginCase(string value)
        {
            WriteIndentedLine($"case {value}:");
            Indent();
        }

        public void EndCase()
        {
            WriteIndentedLine("break;");
            Unindent();
        }
        
        public void BeginBlock()
        {
            WriteIndentedLine("{");
            Indent();
        }

        public void EndBlock()
        {
            Unindent();
            WriteIndentedLine("}");
        }

        public void Indent()
        {
            _indent++;
        }

        public void Unindent()
        {
            _indent--;

            if (_indent < 0)
                ThrowHelper.ThrowInvalidOperationException("Negative indent");
        }

        public void WriteLine() => _writer.WriteLine();
        public void WriteLine(string line) => _writer.WriteLine(line);
        
        public void WriteIndentedLine()
        {
            WriteIndent();
            _writer.WriteLine();
        }

        public void WriteIndentedLine(string line)
        {
            WriteIndent();
            _writer.WriteLine(line);
        }

        public void Write(string line)
        {
            _writer.Write(line);
        }

        public void WriteIndent()
        {
            for (var i = 0; i < _indent; i++)
                _writer.Write(OneTab);
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}