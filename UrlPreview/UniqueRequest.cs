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
    /// actually mean there is an infinite loop - it could be amending the request you make until
    /// some pre-condition is met at which time the request will go through, as stupid as this
    /// approach may be. *cough* eBay *cough*
    /// </summary>
    class UniqueRequest : IEqualityComparer<UniqueRequest>, IEquatable<UniqueRequest>
    {
        /// <summary>
        /// This is a URI because URI encoding can differ but the URL can ultimately be the same
        /// </summary>
        public Uri RequestUri;
        public HttpMethod Method;
        public SortedSet<KeyValuePair<string, IEnumerable<string>>> Headers;

        public override int GetHashCode()
        {
            var hash = new XXHash32();
            hash.Update(ToBytes(RequestUri.ToString()));
            hash.Update(ToBytes(Method.Method));

            foreach (var kv in Headers)
            {
                //case-insensitive keys
                hash.Update(ToBytes(kv.Key.ToLowerInvariant()));
                foreach (var headerValue in kv.Value)
                {
                    //case-sensitive values?
                    hash.Update(ToBytes(headerValue));
                }
            }

            return (int)hash.Result;
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

        public bool Equals(UniqueRequest x, UniqueRequest y)
        {
            return x.GetHashCode() == y.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is UniqueRequest)
            {
                return ((UniqueRequest)obj).GetHashCode() == GetHashCode();
            }

            return obj.Equals(this);
        }

        public int GetHashCode(UniqueRequest obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(UniqueRequest other)
        {
            return other.GetHashCode() == GetHashCode();
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
