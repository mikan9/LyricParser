using HtmlAgilityPack;
using LyricParser.Common;
using System.Threading.Tasks;

namespace LyricParser.Utils.Parsers.Anime
{
    public class GendouParser : HtmlParser
    {
        public GendouParser()
        {
            BaseUrl = Constants.GENDOU_URL;
        }

        public override async Task<string> ParseHtml(string artist, string title, string optional = "")
        {
            string html = await GetHtml(BaseUrl + artist.Replace(" ", "+") + "+" + title.Replace(" ", "+") + optional);
            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            return CleanUp(doc.DocumentNode.SelectSingleNode("//*[contains(@id,'content_1')]").InnerHtml);
        }
    }
}
