using System.Threading.Tasks;

namespace NeoSmart.UrlPreview
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
