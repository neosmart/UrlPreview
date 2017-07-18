using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeoSmart.UrlPreview
{
    public class UrlPreview
    {
        public Uri Uri { get; private set; }
        public string Url => Uri.ToString();
        public string ContentType { get; private set; }

        public UrlPreview()
        {
        }

        public UrlPreview(Uri uri)
        {
            if (uri.Scheme.ToLower() != "http" && uri.Scheme.ToLower() != "https")
            {
                throw new UnsupportedUrlSchemeException();
            }

            Uri = uri;
        }

        public UrlPreview(string uri)
            : this(new Uri(uri))
        { }

        public async Task<PreviewResult> GetPreviewAsync(CancellationToken? cancel = null)
        {
            var html = new Html();
            await html.LoadAsync(Uri, cancel);
            cancel?.ThrowIfCancellationRequested();

            ContentType = html.ContentType;

            UrlLoader urlLoader = new Loaders.GenericUrlLoader(html);

            return new PreviewResult
            {
                Url = Uri,
                Title = await urlLoader.ExtractPageTitleAsync(),
                ImageUrl = await urlLoader.ExtractThumbnailAsync()
            };
        }
    }
}
