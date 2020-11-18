using HtmlAgilityPack;
using LyricParser.Extensions;
using LyricParser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LyricParser.Utils.Parsers.Western
{
    public class MusixmatchParser : HtmlParser
    {
        public MusixmatchParser()
        {
            BaseUrl = Constants.MUSIXMATCH_URL;
        }
        public override async Task<string> ParseHtml(string artist, string title, string optional = "")
        {
            string html = await GetHtml(BaseUrl + artist.Replace(" ", "%20") + "-" + title.Replace(" ", "%20"));
            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var _empty = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'mxm-lyrics-not-available')]");
            if (_empty != null)
            {
                return null;
            }

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//*[contains(@class,'mxm-lyrics__content ')]");
            if (nodes == null) return null;

            List<string> verses = new List<string>();
            foreach (HtmlNode node in nodes)
            {
                verses.Add(node.Element("span").InnerHtml);
                verses.Add("\r\n");
            }
            verses.Add("\r\n");

            return CleanUp(verses);
        }
    }
}
