using System;
using System.Threading.Tasks;
using System.Web;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class AudioContentProvider : IContentProvider
    {
        public bool IsValidContent(Uri uri)
        {
            return uri.AbsolutePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                   uri.AbsolutePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                   uri.AbsolutePath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ContentProviderResult> GetContent(ContentProviderHttpRequest request)
        {
            string url = request.RequestUri.ToString();
            return Task.FromResult(new ContentProviderResult()
            {
                Content = String.Format(@"<audio controls=""controls"" src=""{1}"">{0}</audio>", LanguageResources.AudioTagSupportRequired, HttpUtility.HtmlAttributeEncode(url)),
                Title = request.RequestUri.AbsoluteUri
            });
        }
    }
}