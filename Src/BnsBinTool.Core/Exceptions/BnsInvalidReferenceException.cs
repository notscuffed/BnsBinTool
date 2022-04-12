using System;

namespace BnsBinTool.Core.Exceptions
{
    public class BnsInvalidReferenceException : BnsException
    {
        public string Reference { get; set; }
        
        public BnsInvalidReferenceException(string reference) : base($"Invalid reference: '{reference}'")
        {
            Reference = reference;
        }

        public BnsInvalidReferenceException(string reference, Exception innerException) : base($"Invalid reference: '{reference}'", innerException)
        {
            Reference = reference;
        }
    }
}