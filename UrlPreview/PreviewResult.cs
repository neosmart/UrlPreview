using System;

namespace NeoSmart.UrlPreview
{
    public struct PreviewResult
    {
        public Uri Url { get; set; }

        public string ImageUrl { get; set; }
        public string Title { get; set; }
        public string Snippet { get; set; }
    }
}
