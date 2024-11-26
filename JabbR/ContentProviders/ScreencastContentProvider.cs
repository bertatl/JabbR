using System;
using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using Nancy.Helpers;

namespace JabbR.ContentProviders
{
    public class ScreencastContentProvider : CollapsibleContentProvider
    {
        private static readonly string ContentFormat = "<img src=\"{0}\" alt=\"{1}\" />";

        protected override async Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            var pageInfo = await ExtractFromResponse(request);
            return new ContentProviderResult
            {
                Content = String.Format(ContentFormat,
                                        HttpUtility.HtmlAttributeEncode(pageInfo.ImageURL),
                                        HttpUtility.HtmlAttributeEncode(pageInfo.Title)),
                Title = pageInfo.Title
            };
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.Host.IndexOf("screencast.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task<PageInfo> ExtractFromResponse(ContentProviderHttpRequest request)
        {
            //Force https for the url
            var builder = new UriBuilder(request.RequestUri) { Scheme = "https" };

            var response = await Http.GetAsync(builder.Uri);
            var pageInfo = new PageInfo();
using (Stream responseStream = await response.Content.ReadAsStreamAsync())
            {
                var htmlDocument = new HtmlDocument();
                htmlDocument.Load(responseStream);

                HtmlNode title = htmlDocument.DocumentNode.SelectSingleNode("//title");
                HtmlNode imageURL = htmlDocument.DocumentNode.SelectSingleNode("//img[@class='embeddedObject']");
                pageInfo.Title = title != null ? title.InnerText : String.Empty;
                pageInfo.ImageURL = imageURL != null ? imageURL.Attributes["src"].Value : String.Empty;
            }

            return pageInfo;
        }

        private class PageInfo
        {
            public string Title { get; set; }
            public string ImageURL { get; set; }
        }
    }
}