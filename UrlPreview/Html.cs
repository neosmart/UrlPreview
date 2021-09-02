using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace NeoSmart.UrlPreview
{
    public class Html
    {
        private static readonly string[] LegalSchemes = { "http", "https" };

        public Uri Uri { get; private set; }
        public string UnparsedHtml { get; private set; }
        public string HtmlTitle { get; private set; }

        private const string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
        private const uint MaxRead = 1024 * 1024;
        public string ContentType { get; private set; }
        private HtmlAgilityPack.HtmlDocument _document;
        public HtmlAgilityPack.HtmlNode Document => _document?.DocumentNode;

        public async Task<bool> LoadAsync(Uri uri, CancellationToken cancel = default)
        {
            Uri = uri;

            try
            {
                // UAP apps forbid redirect from HTTPS to HTTP, we must manually redirect to avoid this issue
                using (var handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                    UseCookies = true
                })
                {
                    var wc = new HttpClient(handler);
                    wc.DefaultRequestHeaders.Add("Accept", "*/*");
                    wc.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                    wc.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.8,fr;q=0.6,de;q=0.4");
                    wc.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
                    wc.DefaultRequestHeaders.Add("DNT", "1");
                    wc.DefaultRequestHeaders.Add("Referer", $"{uri.Scheme}://{uri.Host}/");
                    wc.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    wc.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

                    using (var response = await wc.GetAsyncRedirect(uri, cancel))
                    {
                        cancel.ThrowIfCancellationRequested();
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new UrlLoadFailureException((int)response.StatusCode, response.ReasonPhrase ?? "");
                        }

                        if (response.Headers.TryGetValues("Content-Type", out var contentTypes))
                        {
                            if (contentTypes.FirstOrDefault() != "text/html")
                            {
                                // This is not an html document, so it can't be parsed as one
                                return false;
                            }
                        }

                        using (var content = response.Content)
                        using (var responseStream = await content.ReadAsStreamAsync())
                        {
                            cancel.ThrowIfCancellationRequested();
                            ContentType = content.Headers.ContentType?.MediaType;
                            var html = new StringBuilder((int)Math.Min(content.Headers.ContentLength ?? 4 * 1024, MaxRead));
                            var buffer = new byte[4 * 1024];
                            int bytesRead = 0;
                            int totalRead = 0;
                            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancel)) > 0)
                            {
                                cancel.ThrowIfCancellationRequested();
                                html.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                                totalRead += bytesRead;

                                if (totalRead > MaxRead)
                                {
                                    // We are limiting the bytes requested in GetAsyncRedirect()
                                    // Stop here so we're not tricked into reading gigabytes and gigabytes of data
                                    break;
                                }
                            }
                            UnparsedHtml = html.ToString();
                        }
                    }

                    _document = new HtmlAgilityPack.HtmlDocument();
                    _document.LoadHtml(UnparsedHtml);

                    HtmlTitle = ExtractTitle();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception loading {uri}: {ex}");
                throw;
            }
        }

        private string ExtractTitle()
        {
            var title = _document.DocumentNode.SelectSingleNode("//title");
            return title?.InnerText ?? string.Empty;
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

        public async Task<bool> IsValidUrlAsync(string url, CancellationToken cancel = default)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !LegalSchemes.Contains(uri.Scheme.ToLower()))
            {
                Debug.WriteLine("Invalid preview URL " + url);
                return false;
            }

            try
            {
                var request = new HttpClient();
                using (var response = await request.GetAsyncRedirect(url, cancel))
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
