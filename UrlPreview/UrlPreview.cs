using NeoSmart.UrlPreview.Loaders;
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

            UrlLoader urlLoader;
            if (Uri.Host.Equals("amzn.to", StringComparison.OrdinalIgnoreCase)
                || Uri.Host.StartsWith("amazon.", StringComparison.OrdinalIgnoreCase)
                || Uri.Host.StartsWith("smile.amazon.", StringComparison.OrdinalIgnoreCase))
            {
                urlLoader = new AmazonLoader(Uri, html);
                var tag = "?tag=neos00-20";
                var url = Uri.ToString();
                if (!url.Contains("tag="))
                {
                    if (!url.Contains('?'))
                    {
                        Uri = new Uri($"{url}{tag}");
                    }
                    else
                    {
                        Uri = new Uri(url.Replace("?", tag));
                    }
                }
            }
            else
            {
                urlLoader = new GenericUrlLoader(Uri, html);
            }

            return new PreviewResult
            {
                Url = Uri,
                Title = await urlLoader.ExtractPageTitleAsync(),
                ImageUrl = await urlLoader.ExtractThumbnailAsync()
            };
        }
    }
}
