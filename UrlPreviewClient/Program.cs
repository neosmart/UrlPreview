using System;
using System.Threading.Tasks;

namespace UrlPreviewClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new PreviewClient();
            client.Start().GetAwaiter().GetResult();
        }
    }
}
