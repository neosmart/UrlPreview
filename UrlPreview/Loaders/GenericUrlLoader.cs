using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoSmart.UrlPreview.Loaders
{
    class GenericUrlLoader : UrlLoader
    {
        private static string[] LegalSchemes = new string[] { "http", "https" };
        
        public GenericUrlLoader(Html html) : base(html)
        {
        }

        public override Task<string> ExtractPageSnippet()
        {
            throw new NotImplementedException();
        }

        public override Task<string> ExtractPageTitleAsync()
        {
            // First try to find an og:title tag that matches
            var matches = _html.Document.Descendants("meta")
                .Where(n => n.GetAttributeValue("property", null) == "og:title")
                .Where(n => !string.IsNullOrWhiteSpace(n.GetAttributeValue("content", null)))
                .ToList();
            
            if (matches.Count > 0)
            {
                return Task.FromResult(matches[0].GetAttributeValue("content", null));
            }

            // Otherwise revert to the HTML title
            return Task.FromResult(_html.HtmlTitle);
        }

        public override async Task<string> ExtractThumbnailAsync()
        {
            // Maybe this isn't an HTML document and it's actually an image
            if (_html.ContentType != null && _html.ContentType.ToLower().StartsWith("image/"))
            {
                return (await _html.IsValidUrlAsync(_html.Uri.ToString())) ? _html.Uri.ToString() : null;
            }

            // First try to find an og:image or og:image:secure_url
            var matches = _html.Document.Descendants("meta")
                .Where(n => new[] { "og:image:secure_url", "og:image" }.Contains(n.GetAttributeValue("property", null)))
                .Select(n => n.GetAttributeValue("content", null))
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => _html.MakeProperUrl(url).ToString());

            foreach (var url in matches)
            {
                if (await _html.IsValidUrlAsync(url))
                {
                    return url;
                }
            }

            // Else try to find the <del>first</del> second image in the document with a valid URL
            // (presuming the first image is a header or something)
            var images = _html.Document.Descendants("img")
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
                .Select(url => _html.MakeProperUrl(url).ToString())
                .ToArray();

            if (images.Length == 1)
            {
                return (await _html.IsValidUrlAsync(images[0])) ? images[0] : null;
            }

            foreach (var image in images.Skip(1))
            {
                if (await _html.IsValidUrlAsync(image))
                {
                    return image;
                }
            }

            return null;
        }
    }
}
