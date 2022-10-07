using System;
using System.Threading.Tasks;

namespace NeoSmart.UrlPreview
{
    abstract class UrlLoader
    {
        protected Uri Url { get; private set; }
        protected Html Html { get; private set; }

        public UrlLoader(Uri url, Html html)
        {
            Url = url;
            Html = html;
        }

        public abstract Task<string?> ExtractPageTitleAsync();
        public abstract Task<string?> ExtractThumbnailAsync();
        public abstract Task<string?> ExtractPageSnippet();
    }
}
