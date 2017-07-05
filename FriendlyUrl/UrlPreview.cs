using System;
using System.Threading.Tasks;

namespace NeoSmart.FriendlyUrl
{
    public class UrlPreview
    {
        public string ImageUrl { get; set; }
        public string Title { get; set; }
        public string Snippet { get; set; }
        public Uri Uri => _uri;
        public string Url => _uri.ToString();
        public string ContentType { get; private set; }

        private Uri _uri;
        public UrlPreview(Uri uri)
        {
            if (uri.Scheme.ToLower() != "http" && uri.Scheme.ToLower() != "https")
            {
                throw new UnsupportedUrlSchemeException();
            }

            _uri = uri;
        }

        public UrlPreview(string uri)
            : this(new Uri(uri))
        { }

        public async Task<UrlPreview> GetPreviewAsync()
        {
            var html = new Html();
            if (!await html.LoadAsync(_uri))
            {
                return null;
            }
            ContentType = html.ContentType;

            UrlLoader urlLoader = new Loaders.GenericUrlLoader(html);
            Title = await urlLoader.ExtractPageTitleAsync();
            ImageUrl = await urlLoader.ExtractThumbnailAsync();

            return this;
        }
    }
}
