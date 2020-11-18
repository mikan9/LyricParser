using HtmlAgilityPack;
using LyricParser.Extensions;
using LyricParser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LyricParser.Utils.Parsers.Vocaloid
{
    public class AtwikiParser : HtmlParser
    {
        public AtwikiParser()
        {
            BaseUrl = Constants.ATWIKI_URL;
        }
        public override async Task<string> ParseHtml(string artist, string title, string optional = "")
        {
            string html = await GetHtml(BaseUrl + title.ToUrlSafe("-") + title.Replace(" ", "+"));
            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var body = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'wikibody')]");
            bool foundLyrics = false;

            List<string> verses = new List<string>();

            foreach (var child in body.ChildNodes)
            {
                if (!foundLyrics && child.InnerText != "歌詞")
                    continue;
                else if (child.InnerText == "歌詞" && !foundLyrics)
                {
                    foundLyrics = true;
                    continue;
                }

                if (foundLyrics)
                {
                    if (child.InnerText == "コメント") break;

                    if (child.HasChildNodes)
                    {
                        if (verses.Count > 0) verses.Add("\r\n\r\n");
                        verses.Add(child.InnerHtml.Replace("\n", "").Trim());
                    }
                }

                return CleanUp(verses);
            }

            return null;
        }
    }
}
