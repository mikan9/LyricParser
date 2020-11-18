using HtmlAgilityPack;
using LyricParser.Extensions;
using LyricParser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace LyricParser.Utils.Parsers.JP
{
    public class JlyricParser : HtmlParser
    {
        public JlyricParser()
        {
            BaseUrl = Constants.JLYRIC_URL;
        }

        public override async Task<string> ParseHtml(string artist, string title, string optional = "")
        {
            string html = await GetHtml(BaseUrl + title.Replace(" ", "+") + "&ct=1&ka=" + artist);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            bool foundMatch = false;
            var mnb = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'mnb')]");
            var bdy = mnb.SelectNodes(".//*[contains(@class, 'bdy')]");

            for (int i = 0; i < bdy.ToArray().Length; ++i)
            {
                string _title = "";
                string _artist = "";

                var mid = bdy.ElementAt(i).SelectSingleNode(".//*[contains(@class,'mid')]");
                var sml = bdy.ElementAt(i).SelectSingleNode(".//*[contains(@class,'sml')]");

                _title = mid.FirstChild.InnerText.ToLower().TrimStart().TrimEnd();
                _artist = sml.ChildNodes.ElementAt(1).InnerText.ToLower().TrimStart().TrimEnd();

                if (_title == title.ToLower() && _artist.Contains(artist.ToLower()))
                {
                    foundMatch = true;
                    html = await GetHtml(mid.FirstChild.GetAttributeValue("href", "null"));
                    break;
                }
            }

            if (!foundMatch || string.IsNullOrEmpty(html)) return null;

            doc = new HtmlDocument();
            doc.LoadHtml(html);

            return CleanUp(doc.DocumentNode.SelectSingleNode("//*[contains(@id,'Lyric')]").InnerHtml);
        }
    }
}
