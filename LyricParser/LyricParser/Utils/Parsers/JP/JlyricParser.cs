using HtmlAgilityPack;
using LyricParser.Extensions;
using LyricParser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            return CleanUp(doc.DocumentNode.SelectSingleNode("//*[contains(@id,'Lyric')]").InnerHtml);
        }
    }
}
