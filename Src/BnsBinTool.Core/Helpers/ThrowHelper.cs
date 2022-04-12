using System;
using System.Diagnostics.CodeAnalysis;
using BnsBinTool.Core.Exceptions;

namespace BnsBinTool.Core.Helpers
{
    public static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowException(string text)
        {
            throw new Exception(text);
        }
        
        [DoesNotReturn]
        public static void ThrowInvalidRefException(string reference)
        {
            throw new BnsInvalidReferenceException(reference);
        }

        [DoesNotReturn]
        public static void ThrowOutOfRangeException(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }

        [DoesNotReturn]
        public static void ThrowException(this XmlFileReader reader, string message)
        {
            throw new BnsXmlFileReaderException(message, reader.FilePath, reader.LineNumber, reader.LinePosition);
        }

        [DoesNotReturn]
        public static void ThrowNotImplementedException()
        {
            throw new NotImplementedException();
        }
        
        [DoesNotReturn]
        public static T ThrowNotImplementedException<T>()
        {
            throw new NotImplementedException();
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException(string message = null)
        {
            throw new InvalidOperationException(message);
        }

        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }
        
        [DoesNotReturn]
        public static void ThrowArgumentException(string paramName)
        {
            throw new ArgumentException(paramName);
        }
        
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
        
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName, object actualValue, string message = null)
        {
            throw new ArgumentOutOfRangeException(paramName, actualValue, message);
        }

        [DoesNotReturn]
        public static void ThrowObjectDisposedException(string objectName = null)
        {
            throw new ObjectDisposedException(objectName);
        }

        [DoesNotReturn]
        public static void ThrowNotSupportedException(string message = null)
        {
            throw new NotSupportedException(message);
        }
    }
}