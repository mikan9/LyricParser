using HtmlAgilityPack;
using LyricParser.Extensions;
using LyricParser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LyricParser.Utils.Parsers.Western
{
    public class MetrolyricsParser : HtmlParser
    {
        public MetrolyricsParser()
        {
            BaseUrl = Constants.METROLYRICS_URL;
        }

        public override async Task<string> ParseHtml(string artist, string title, string optional = "")
        {
            string html = await GetHtml(BaseUrl + title.ToUrlSafe("-") + "-lyrics-" + artist.ToUrlSafe("-") + ".html");
            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//p[contains(@class, 'verse')]");
            if (nodes == null) return null;

            List<string> verses = new List<string>();
            foreach (HtmlNode node in nodes)
            {
                string inner = node.InnerText.Trim();
                verses.Add(inner);
            }

            return CleanUp(verses);
        }
    }
}
