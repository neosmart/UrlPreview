using NeoSmart.UrlPreview.Loaders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeoSmart.UrlPreview
{
    public class UrlPreview
    {
        private static readonly string[] LegalSchemes = { "http", "https" };

        public Uri Uri { get; private set; } = null!;
        public string Url => Uri.ToString();
        public string? ContentType { get; private set; }

        // Begin user-settable parameters to modify preview state/behavior
        /// <summary>
        /// If set, overrides the default UrlPreview user agent used to make the GET request.
        /// </summary>
        public string? UserAgent { get; set; }
        /// <summary>
        /// If set, overrides the default UrlPreview HTTP <c>Referer</c> header in the GET request.
        /// </summary>
        public Uri? Referrer { get; set; }

        public UrlPreview()
        {
        }

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
            if (UserAgent is string userAgent)
            {
                html.UserAgent = userAgent;
            }
            if (Referrer is Uri referrer)
            {
                html.Referrer = referrer;
            }
            await html.LoadAsync(Uri, cancel);
            cancel.ThrowIfCancellationRequested();

            ContentType = html.ContentType;

            UrlLoader urlLoader;
            if (Uri.Host.Equals("amzn.to", StringComparison.OrdinalIgnoreCase)
                || Uri.Host.StartsWith("amazon.", StringComparison.OrdinalIgnoreCase)
                || Uri.Host.StartsWith("www.amazon.", StringComparison.OrdinalIgnoreCase)
                || Uri.Host.StartsWith("smile.amazon.", StringComparison.OrdinalIgnoreCase))
            {
                urlLoader = new AmazonLoader(Uri, html);
                var tag = "tag=neos00-20";
                var url = Uri.ToString();
                if (!url.Contains("tag="))
                {
                    if (!url.Contains('?'))
                    {
                        Uri = new Uri($"{url}?{tag}");
                    }
                    else
                    {
                        Uri = new Uri(url.Replace("?", $"?{tag}&"));
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
