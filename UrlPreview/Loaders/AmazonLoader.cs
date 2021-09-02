using System;
using System.Threading.Tasks;

namespace NeoSmart.UrlPreview.Loaders
{
    class AmazonLoader : GenericUrlLoader
    {
        public AmazonLoader(Uri url, Html html) : base(url, html)
        {
        }

        public override Task<string> ExtractThumbnailAsync()
        {
            // As of 2021, the main image for an Amazon product listing URL is "span[data-action=main-image-click] img"
            var mainImage = Html.Document.SelectSingleNode("//span[@data-action='main-image-click']//img[@src]");
            if (mainImage is null)
            {
                mainImage = Html.Document.SelectSingleNode("//img[@id='landingImage'][@src]");
                if (mainImage is null)
                {
                    return base.ExtractThumbnailAsync();
                }
            }

            var url = mainImage.GetAttributeValue("src", null);
            return Task.FromResult(url);
        }
    }
}
