using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeoSmart.FriendlyUrl
{
    abstract class UrlLoader
    {
        protected Html _html;

        public UrlLoader(Html html)
        {
            _html = html;
        }

        public abstract Task<string> ExtractPageTitleAsync();
        public abstract Task<string> ExtractThumbnailAsync();
        public abstract Task<string> ExtractPageSnippet();
    }
}
