using NeoSmart.UrlPreview;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UrlPreviewClient
{
    class PreviewClient
    {
        public async Task Start()
        {
            while (true)
            {
                Console.Write("Preview URL: ");
                var url = Console.ReadLine();

                await Preview(url);
            }
        }

        private async Task Preview(string url)
        {
            var preview = new UrlPreview(url);
            await preview.GetPreviewAsync();
            var serialized = JsonConvert.SerializeObject(preview, Formatting.Indented);
            Console.WriteLine(serialized);
        }
    }
}
