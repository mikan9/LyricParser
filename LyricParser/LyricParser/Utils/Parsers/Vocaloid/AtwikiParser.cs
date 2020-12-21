using HtmlAgilityPack;
using LyricParser.Extensions;
using LyricParser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

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
            string html = await GetHtml(BaseUrl + title.ToUrlSafe("-") + artist.Replace(" ", "+"));
            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            bool foundMatch = false;

            var body = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'wikibody')]");
            var ul = body.SelectSingleNode("ul");
            if (ul == null) return null;

            foreach (var child in ul.SelectNodes("li"))
            {
                var a = child.SelectSingleNode("a");
                if (a != null && a.InnerText.ToLower().TrimStart().TrimEnd() == title.ToLower().TrimStart().TrimEnd())
                {
                    foundMatch = true;
                    html = await GetHtml("https:" + a.GetAttributeValue("href", "null").Replace(" ", "%20"));
                    break;
                }
            }

            if (!foundMatch) return null;

            doc = new HtmlDocument();
            doc.LoadHtml(html);

            body = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'wikibody')]");
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
                        verses.Add(child.InnerHtml.Replace("\n", "").Trim());
                    }
                }

                
            }
            if(foundLyrics)
                return CleanUp(verses);

            return null;
        }
    }
}
