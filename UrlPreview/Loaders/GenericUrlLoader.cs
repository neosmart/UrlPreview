using System;
using System.Linq;
using System.Threading.Tasks;

namespace NeoSmart.UrlPreview.Loaders
{
    class GenericUrlLoader : UrlLoader
    {
        protected static readonly string[] LegalSchemes = new[] { "http", "https" };

        public GenericUrlLoader(Uri url, Html html) : base(url, html)
        {
        }

        public override Task<string?> ExtractPageSnippet()
        {
            throw new NotImplementedException();
        }

        public override Task<string?> ExtractPageTitleAsync()
        {
            // First try to find an og:title tag that matches
            var matches = Html.Document?.Descendants("meta")
                .Where(n => n.GetAttributeValue("property", null) == "og:title")
                .Where(n => !string.IsNullOrWhiteSpace(n.GetAttributeValue("content", null)))
                .ToList();

            if (matches?.Count > 0)
            {
                return Task.FromResult<string?>(matches[0].GetAttributeValue("content", null));
            }

            // Otherwise revert to the HTML title
            return Task.FromResult(Html.HtmlTitle);
        }

        static string[] EmptyArray = { };
        public override async Task<string?> ExtractThumbnailAsync()
        {
            // Maybe this isn't an HTML document and it's actually an image
            if (Html.ContentType != null && Html.ContentType.ToLower().StartsWith("image/"))
            {
                return (await Html.IsValidUrlAsync(Html.Uri.ToString())) ? Html.Uri.ToString() : null;
            }

            // First try to find an og:image or og:image:secure_url
            var matches = Html.Document?.Descendants("meta")
                .Where(n => new[] { "og:image:secure_url", "og:image" }.Contains(n.GetAttributeValue("property", null)))
                .Select(n => n.GetAttributeValue("content", null))
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => Html.MakeProperUrl(url)?.ToString());

            if (matches is not null)
            {
                foreach (var url in matches)
                {
                    if (url is not null && await Html.IsValidUrlAsync(url))
                    {
                        return url;
                    }
                }
            }

            // Else try to find the <del>first</del> second image in the document with a valid URL
            // (presuming the first image is a header or something)
            string[] images = Html.Document?.Descendants("img")
                .Select(n => n.GetAttributeValue("src", null))
                .Where(url =>
                {
                    try
                    {
                        var uri = new Uri(url);
                        return LegalSchemes.Contains(uri.Scheme.ToLowerInvariant());
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Select(url => Html.MakeProperUrl(url)?.ToString())
                .Where(url => url is not null)
                .Select(url => url!)
                .ToArray() ?? EmptyArray;

            if (images.Length == 1)
            {
                return (await Html.IsValidUrlAsync(images[0])) ? images[0] : null;
            }

            foreach (var image in images.Skip(1))
            {
                if (await Html.IsValidUrlAsync(image))
                {
                    return image;
                }
            }

            return null;
        }
    }
}
