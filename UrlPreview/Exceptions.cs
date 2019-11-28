using System;

namespace NeoSmart.UrlPreview
{
    public class UrlPreviewException : Exception
    {
        public UrlPreviewException(string message = "", Exception innerException = null)
            : base(message, innerException)
        { }
    }

    public class UnsupportedUrlSchemeException : UrlPreviewException
    {
        public UnsupportedUrlSchemeException(string message = "", Exception innerException = null)
            : base(message, innerException)
        { }
    }

    public class UrlLoadFailureException : UrlPreviewException
    {
        public int HttpStatus { get; protected set; }
        public UrlLoadFailureException(int httpStatus, string message = "", Exception innerException = null)
            : base(message, innerException)
        {
            HttpStatus = httpStatus;
        }
    }
}
