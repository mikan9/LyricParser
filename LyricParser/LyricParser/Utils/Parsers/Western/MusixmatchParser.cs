using HtmlAgilityPack;
using LyricParser.Extensions;
using LyricParser.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System;

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

            bool foundMatch = false;

            var results = doc.DocumentNode.SelectNodes("//div[contains(@class,'media-card-text')]");
            if (results == null) return null;

            foreach (var child in results)
            {
                var a = child.SelectSingleNode("//a[contains(@class, 'title')]");
                string _title = a.FirstChild.InnerText.ToLower().Trim();
                string _artist = child.SelectSingleNode("//a[contains(@class, 'artist')]").FirstChild.InnerText.ToLower().Trim();
                if (_title == null || _artist == null) continue;

                if (_title == title && _artist == artist)
                {
                    foundMatch = true;
                    html = await GetHtml("https://www.musixmatch.com" + a.GetAttributeValue("href", "null").Replace(" ", "%20"));
                    break;
                }
            }

            if (!foundMatch) return null;

            doc = new HtmlDocument();
            doc.LoadHtml(html);

            var _empty = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'mxm-lyrics-not-available')]");
            if (_empty != null)
            {
                return null;
            }

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//*[contains(@class,'mxm-lyrics__content')]");
            if (nodes == null) return null;

            StringBuilder lyrics = new StringBuilder();
            foreach (HtmlNode node in nodes)
            {
                lyrics.Append(node.Element("span").InnerHtml + Environment.NewLine);
            }

            return CleanUp(lyrics.ToString());
        }
    }
}
