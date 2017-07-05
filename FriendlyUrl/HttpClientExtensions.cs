using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace NeoSmart.FriendlyUrl
{
    static class HttpClientExtensions
    {
        public class InvalidRedirectException : Exception
        {
            public InvalidRedirectException(string from, string to)
                : base($"Invalid redirect request from {from} to {to}")
            {
            }

            public InvalidRedirectException(string message)
                : base(message)
            {
            }
        }

        public static async Task<HttpResponseMessage> GetAsyncRedirect(this HttpClient client, String url, CancellationToken? cancel = null)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return await client.GetAsyncRedirect(uri, cancel);
            }
            return null;
        }

        public static async Task<HttpResponseMessage> GetAsyncRedirect(this HttpClient client, Uri uri, CancellationToken? cancel = null)
        {
            HashSet<string> visited = new HashSet<string>() { uri.ToString() };

            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancel ?? CancellationToken.None);
            while (((int)response.StatusCode) >= 300 && (int)response.StatusCode < 400)
            {
                response.Dispose();
                var redirect = response.Headers.Location;
                if (!redirect.IsAbsoluteUri)
                {
                    if (!Uri.TryCreate(uri, redirect, out redirect))
                    {
                        throw new InvalidRedirectException(uri.ToString(), redirect.ToString());
                    }
                }
                if (!visited.Add(redirect.ToString()))
                {
                    throw new InvalidRedirectException($"Infinite redirection encountered loading URI {uri}");
                }
                if (visited.Count > 100)
                {
                    throw new InvalidRedirectException($"Too many redirections encountered loading URI {uri}");
                }
                response = await client.GetAsync(redirect, HttpCompletionOption.ResponseHeadersRead, cancel ?? CancellationToken.None);
            }
            return response;
        }
    }
}
