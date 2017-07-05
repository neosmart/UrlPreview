using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace NeoSmart.FriendlyUrl
{
    public class Html
    {
        public Uri Uri { get; private set; }
        public string UnparsedHtml { get; private set; }
        public string HtmlTitle { get; private set; }
        //public List<KeyValuePair<string, Dictionary<string, string>>> TagList { get; private set; }
        private bool _loaded = false;
        private static CancellationToken _noCancel = new CancellationToken(false);
        private CancellationToken _cancel;
        private const uint maxRead = 1024 * 1024;
        public string ContentType { get; private set; }
        private HtmlAgilityPack.HtmlDocument _document;
        public HtmlAgilityPack.HtmlNode Document => _document?.DocumentNode;

        public async Task<bool> LoadAsync(Uri uri, CancellationToken? cancel = null)
        {
            if (_loaded)
            {
                throw new Exception("Cannot load more than once!");
            }

            Uri = uri;
            _cancel = cancel ?? _noCancel;

            try
            {
                //UAP apps forbid redirect from HTTPS to HTTP
                //We must manually redirect to avoid this issue
                using (var handler = new HttpClientHandler() { AllowAutoRedirect = false })
                using (var wc = new HttpClient(handler))
                using (var response = await wc.GetAsyncRedirect(uri, _cancel))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new UrlLoadFailureException((int)response.StatusCode);
                    }

                    if (response.Headers.TryGetValues("Content-Type", out var contentTypes))
                    {
                        if (contentTypes.FirstOrDefault() != "text/html")
                        {
                            //not an html document
                            return false;
                        }
                    }

                    using (var content = response.Content)
                    using (var responseStream = await content.ReadAsStreamAsync())
                    {
                        ContentType = content.Headers.ContentType?.MediaType;
                        StringBuilder html = new StringBuilder((int)(Math.Min(content.Headers.ContentLength ?? 4 * 1024, (long)maxRead)));
                        var buffer = new byte[4 * 1024];
                        int bytesRead = 0;
                        int totalRead = 0;
                        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, _cancel)) > 0)
                        {
                            html.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                            totalRead += bytesRead;

                            if (totalRead > maxRead)
                            {
                                //stop here so we're not tricked into reading gigabytes and gigabytes of data
                                break;
                                //we are limiting the bytes requested in GetAsyncRedirect()
                            }
                        }
                        UnparsedHtml = html.ToString();
                    }
                }

                _document = new HtmlAgilityPack.HtmlDocument();
                _document.LoadHtml(UnparsedHtml);

                //ExtractAllTags();
                ExtractTitle();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception loading {uri.ToString()}: {ex.ToString()}");
                return false;
            }
        }

        static private Regex _titleTagRegex = new Regex(@"\<[^><]*\btitle\b[^><]*\>([^><]*)<[^><]*/\s*title\b\s*\>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        //<\s*([a-z]+\b)\s*(?>([a-z-]+)\s*(=\s*(?:(['"])(.*?)\4)|[a-z0-9-]+)?\s*)*\s*\/?\s*>
        static private Regex _genericTagRegex = new Regex(@"<\s*([a-z]+\b)\s*(?>(([a-z-]+)\s*=\s*(?:(['" + "\"" + @"])(.*?)\4)|[a-z0-9-]+)?\s*)*\s*\/?\s*>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private string ExtractTitle()
        {
            var matches = _document.DocumentNode.Descendants("title");
            if (matches.Any())
            {
                return matches.First().InnerText;
            }

            return null;
        }

        /// <summary>
        /// Extract a *flat* list of all tags in the document (no nesting) along with a dictionary of their key=value pairs
        /// This was used when HtmlAgilityPack was not available for .NET Core/UWP
        /// </summary>
        /// <returns></returns>
        private List<KeyValuePair<string, Dictionary<string, string>>> ExtractAllTags()
        {
            var tags = new List<KeyValuePair<string, Dictionary<string, string>>>();

            var matches = _genericTagRegex.Matches(UnparsedHtml);
            foreach (Match m in matches)
            {
                var attrs = new Dictionary<string, string>();
                var tag = new KeyValuePair<string, Dictionary<string, string>>(m.Groups[1].Value, attrs);

                //foreach (Capture attr in m.Groups[2].Captures)
                for (int i = 0, j = 0; i < m.Groups[2].Captures.Count; ++i)
                {
                    var attr = m.Groups[2].Captures[i];
                    //tag.Key = attr.Value;
                    if (attr.Value.Contains("="))
                    {
                        var key = m.Groups[2].Captures[i].Value.ToLower().Substring(0, m.Groups[2].Captures[i].Value.IndexOf('=', 0));
                        tag.Value[key] = m.Groups[5].Captures[j++].Value;
                    }
                    else
                    {
                        tag.Value[m.Groups[2].Captures[i].Value.ToLower()] = null;
                    }
                }
                tags.Add(tag);
            }

            return tags;
        }

        public Uri MakeProperUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                if (!uri.IsAbsoluteUri)
                {
                    if (!Uri.TryCreate(this.Uri, url, out uri))
                    {
                        return null;
                    }
                }

                return uri;
            }
            return null;
        }

        public async Task<bool> IsValidUrlAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !new[] { "http", "https" }.Contains(uri.Scheme.ToLower()))
            {
                Debug.WriteLine("Invalid preview URL " + url);
                return false;
            }
            try
            {
                using (var request = new HttpClient())
                using (var response = await request.GetAsyncRedirect(url, _cancel))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                Debug.WriteLine("Invalid preview URL " + url);
                return false;
            }
        }
    }
}
