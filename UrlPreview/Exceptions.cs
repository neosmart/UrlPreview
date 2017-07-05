using System;

namespace NeoSmart.UrlPreview
{
    public class UrlPreviewException : Exception
    {
        public UrlPreviewException(string message = "", Exception innerException = null)
            : base(message, innerException)
        { }
    }

    public class UnsupportedUrlSchemeException : Exception
    {
        public UnsupportedUrlSchemeException(string message = "", Exception innerException = null)
            : base(message, innerException)
        { }
    }

    public class UnsupportedUrlException : Exception
    {
        public UnsupportedUrlException(string message = "This URL does not support previewing", Exception innerException = null)
            : base(message, innerException)
        { }
    }

    public class UrlLoadFailureException : Exception
    {
        public int HttpStatus { get; protected set; }
        public UrlLoadFailureException(int httpStatus, string message = "", Exception innerException = null)
            : base(message, innerException)
        {
            HttpStatus = httpStatus;
        }
    }
}
