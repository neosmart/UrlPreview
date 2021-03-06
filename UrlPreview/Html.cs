﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace NeoSmart.UrlPreview
{
    public class Html
    {
        public Uri Uri { get; private set; }
        public string UnparsedHtml { get; private set; }
        private static readonly string[] LegalSchemes = { "http", "https" };
        public string HtmlTitle { get; private set; }
        private readonly bool _loaded = false;

        private const string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
        private const uint MaxRead = 1024 * 1024;
        public string ContentType { get; private set; }
        private HtmlAgilityPack.HtmlDocument _document;
        public HtmlAgilityPack.HtmlNode Document => _document?.DocumentNode;

        public async Task<bool> LoadAsync(Uri uri, CancellationToken cancel = default)
        {
            Debug.Assert(_loaded == false, "HTML class cannot be reused at this time!");

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
                using (var wc = new HttpClient(handler))
                {
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
                            var html = new StringBuilder((int)(Math.Min(content.Headers.ContentLength ?? 4 * 1024, (long)MaxRead)));
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

                    // ExtractAllTags();
                    HtmlTitle = ExtractTitle();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception loading {uri.ToString()}: {ex.ToString()}");
                throw;
            }
        }

        private static Regex _titleTagRegex = new Regex(
            @"\<[^><]*\btitle\b[^><]*\>([^><]*)<[^><]*/\s*title\b\s*\>", 
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        //<\s*([a-z]+\b)\s*(?>([a-z-]+)\s*(=\s*(?:(['"])(.*?)\4)|[a-z0-9-]+)?\s*)*\s*\/?\s*>
        private static readonly Regex _genericTagRegex = new Regex(
            @"<\s*([a-z]+\b)\s*(?>(([a-z-]+)\s*=\s*(?:(['""])(.*?)\4)|[a-z0-9-]+)?\s*)*\s*\/?\s*>", 
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private string ExtractTitle()
        {
            return _document.DocumentNode.Descendants("title")
                       .FirstOrDefault()?.InnerText ?? string.Empty;
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

        public async Task<bool> IsValidUrlAsync(string url, CancellationToken cancel = default)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !LegalSchemes.Contains(uri.Scheme.ToLower()))
            {
                Debug.WriteLine("Invalid preview URL " + url);
                return false;
            }
            
            try
            {
                using (var request = new HttpClient())
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
