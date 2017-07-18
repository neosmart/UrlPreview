using NeoSmart.Hashing.XXHash;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace NeoSmart.UrlPreview
{
    /// <summary>
    /// An HTTP request is uniquely identified by more than just the URL, it's the entire request.
    /// Just because a server keeps 301ing you to the same request over and over again, it does not 
    /// actually mean there is an infinite loop - it could be ammending the request you make until
    /// some pre-condition is met at which time the request will go through, as stupid as this 
    /// approach may be. *cough* eBay *cough*
    /// </summary>
    struct UniqueRequest
    {
        /// <summary>
        /// This is a URI because URI encoding can differ but the URL can ultimately be the same
        /// </summary>
        public Uri RequestUri;
        public HttpMethod Method;
        public SortedSet<KeyValuePair<string, IEnumerable<string>>> Headers;

        public override int GetHashCode()
        {
            var xxState = XXHash.CreateState32();
            XXHash.UpdateState32(xxState, ToBytes(RequestUri.ToString()));
            XXHash.UpdateState32(xxState, ToBytes(Method.Method));
            
            foreach (var kv in Headers)
            {
                //case-insensitive keys
                XXHash.UpdateState32(xxState, ToBytes(kv.Key.ToLowerInvariant()));
                foreach (var headerValue in kv.Value)
                {
                    //case-sensitive values?
                    XXHash.UpdateState32(xxState, ToBytes(headerValue));
                }
            }

            return (int) XXHash.DigestState32(xxState);
        }

        public byte[] ToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        public static UniqueRequest From(HttpRequestMessage request)
        {
            return new UniqueRequest
            {
                RequestUri = request.RequestUri,
                Method = request.Method,
                Headers = new SortedSet<KeyValuePair<string, IEnumerable<string>>>(request.Headers, new HeaderCollectionComparer()),
            };
        }

        class HeaderCollectionComparer : IComparer<KeyValuePair<string, IEnumerable<string>>>
        {
            public int Compare(KeyValuePair<string, IEnumerable<string>> x, KeyValuePair<string, IEnumerable<string>> y)
            {
                return StringComparer.CurrentCultureIgnoreCase.Compare(x.Key, y.Key);
            }
        }
    }
}
