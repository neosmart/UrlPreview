using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoSmart.UrlPreview.Loaders
{
    class GenericUrlLoader : UrlLoader
    {
        public GenericUrlLoader(Html html) : base(html)
        {
        }

        public override Task<string> ExtractPageSnippet()
        {
            throw new NotImplementedException();
        }

        public override async Task<string> ExtractPageTitleAsync()
        {
            //first try to find an og:title tag that matches
            var matches = _html.Document.Descendants("meta")
                .Where(n => n.GetAttributeValue("property", null) == "og:title")
                .Where(n => !string.IsNullOrWhiteSpace(n.GetAttributeValue("content", null)));
            if (matches.Any())
            {
                return matches.First().GetAttributeValue("content", null);
            }

            //otherwise
            return _html.HtmlTitle;
        }

        public override async Task<string> ExtractThumbnailAsync()
        {
            //maybe this isn't an HTML document and it's actually an image
            if (_html.ContentType != null && _html.ContentType.ToLower().StartsWith("image/"))
            {
                return (await _html.IsValidUrlAsync(_html.Uri.ToString())) ? _html.Uri.ToString() : null;
            }

            //first try to find an og:image or og:image:secure_url
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

            //else try to find the <del>first</del> second image in the document with a valid URL
            //(presuming the first image is a header or something)
            var images = _html.Document.Descendants("img")
                .Select(n => n.GetAttributeValue("src", null))
                .Where(url => !string.IsNullOrWhiteSpace(url))
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
