using System;
using System.Linq;
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

        private static readonly string[] LegalSchemes = { "http", "https" };

        public UrlPreview(Uri uri)
        {
            if (!LegalSchemes.Contains(uri.Scheme.ToLowerInvariant()))
            {
                throw new UnsupportedUrlSchemeException();
            }

            Uri = uri;
        }

        public UrlPreview(string uri)
            : this(new Uri(uri))
        { }

        public async Task<PreviewResult> GetPreviewAsync(CancellationToken cancel = default)
        {
            var html = new Html();
            await html.LoadAsync(Uri, cancel);
            cancel.ThrowIfCancellationRequested();

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
