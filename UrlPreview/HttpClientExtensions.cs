using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NeoSmart.UrlPreview
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
            Debug.WriteLine("Initial request: {0}", uri);

            //we used to use only the URI as our cylic request detector but that hasn't worked out too well
            //you'd be surprised at the number of top 50 sites that redirect repeatedly but take different actions
            //based on the cookie and other request headers.
            //Now we deduplicate based off of state; i.e. the entire request header that is made
            var visited = new HashSet<int>();

            HttpResponseMessage response;
            var requestUrl = uri;
            do
            {
                response = await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, cancel ?? CancellationToken.None);
                if (((int)response.StatusCode) >= 300 && (int)response.StatusCode < 400)
                {
                    var redirect = response.Headers.Location;
                    if (!redirect.IsAbsoluteUri)
                    {
                        if (!Uri.TryCreate(uri, redirect, out redirect))
                        {
                            throw new InvalidRedirectException(uri.ToString(), redirect.ToString());
                        }
                    }
                    Debug.WriteLine("Redirecting to {0}", redirect);
                    if (!visited.Add(UniqueRequest.From(response.RequestMessage).GetHashCode())
                        || visited.Count > 100)
                    {
                        throw new InvalidRedirectException($"Infinite redirection encountered loading URI {uri}");
                    }
                    requestUrl = redirect;
                }
                else
                {
                    break;
                }
            } while (true);
            return response;
        }
    }
}
