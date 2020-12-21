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
            if (mnb == null) return null;

            var bdy = mnb.SelectNodes(".//*[contains(@class, 'bdy')]");

            if (doc.DocumentNode.SelectNodes("//*[contains(@class,'mid')]") == null) return null;
            for (int i = 0; i < bdy.ToArray().Length; ++i)
            {
                string _title = "";
                string _artist = "";

                var mid = bdy.ElementAt(i).SelectSingleNode(".//*[contains(@class,'mid')]");
                var sml = bdy.ElementAt(i).SelectSingleNode(".//*[contains(@class,'sml')]");

                if (mid == null) continue;
                
                _title = mid.FirstChild.InnerText.ToLower().Trim();
                _artist = sml.ChildNodes.ElementAt(1).InnerText.ToLower().Trim();

                if(optional == "-d")
                {
                    _title = _title.RemoveDiacritics();
                    _artist = _artist.RemoveDiacritics();
                }

                if (_title == title && _artist.Contains(artist))
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
