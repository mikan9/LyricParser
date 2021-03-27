using HtmlAgilityPack;
using LyricParser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LyricParser.Utils.Parsers.Touhou
{
    public class TouhouwikiParser : HtmlParser
    {
        public TouhouwikiParser()
        {
            BaseUrl = Constants.TOUHOUWIKI_URL;
        }

        public override async Task<string> ParseHtml(string artist, string title, string optional = "")
        {
            string html = await GetHtml(BaseUrl + title.Replace(" ", "_").Replace("[", "(").Replace("]", ")"));
            if (string.IsNullOrEmpty(html)) return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            if (doc.DocumentNode.SelectSingleNode("//*[contains(@class,'noarticletext')]") != null) return null; 

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//*[contains(@class,'lyrics_row')]");
            if (nodes == null) return null;

            List<string> verses = new List<string>();
            foreach (HtmlNode node in nodes)
            {
                string inner = node.FirstChild.SelectSingleNode("p").InnerText.Trim(); // Get inner text of the first paragraph of the first td node
                verses.Add(inner);
            }

            return CleanUp(verses);
        }
    }
}
