using HtmlAgilityPack;
using LyricParser.Common;
using System.Threading.Tasks;
using System;

namespace LyricParser.Utils.Parsers.Western
{
    public class LyricsfreakParser : HtmlParser
    {
        public LyricsfreakParser()
        {
            BaseUrl = Constants.LYRICSFREAK_URL;
        }
        public override async Task<string> ParseHtml(string artist, string title, string optional = "")
        {
            string html = await GetHtml(BaseUrl + artist.Replace(" ", "+") + "+" + title.Replace(" ", "+"));
            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            bool foundMatch = false;

            var results = doc.DocumentNode.SelectNodes("//div[contains(@class,'js-sort-table-content-item')]");
            if (results == null) return null;

            foreach (var child in results)
            {
                var a = child.SelectSingleNode("//a[contains(@class, 'song')]");
                string _title = HtmlDecode(a.InnerText).ToLower().Trim();
                string _artist = HtmlDecode(child.SelectSingleNode("//div[contains(@class, 'lf-list__title--secondary')]").SelectSingleNode("a").InnerText.Replace("&middot;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;", "")).ToLower().Trim();
                if (_title == null || _artist == null) continue;

                if (_title == title && _artist == artist)
                {
                    foundMatch = true;
                    html = await GetHtml("https://www.lyricsfreak.com" + a.GetAttributeValue("href", "null").Replace(" ", "%20"));
                    break;
                }
            }

            if (!foundMatch) return null;

            doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNode lyrics = doc.DocumentNode.SelectSingleNode("//div[contains(@id,'content')]");
            if (lyrics == null) return null;

            return CleanUp(lyrics.InnerText.Replace("\n", Environment.NewLine));
        }
    }
}
