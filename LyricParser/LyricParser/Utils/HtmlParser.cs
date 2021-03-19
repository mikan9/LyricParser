using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LyricParser.Extensions;

namespace LyricParser.Utils
{
    public abstract class HtmlParser
    {
        protected string BaseUrl { get; set; }

        public HtmlParser() { }

        protected virtual async Task<string> GetHtml(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    return await client.GetStringAsync(url).ConfigureAwait(false);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine(e.ToString() + "@" + url);
                }
            }

            return null;
        }

        protected virtual string CleanUp(List<string> parts)
        {
            StringBuilder cleanData = new StringBuilder();
            foreach (string str in parts)
            {
                cleanData.Append(HtmlDecode(str.Replace("<br>", Environment.NewLine).Trim() + Environment.NewLine + Environment.NewLine));
            }
            return RemoveExcessNewLines(cleanData.ToString());
        }

        protected virtual string CleanUp(string lyrics)
        {
            return RemoveExcessNewLines(HtmlDecode(lyrics.Replace("<br>", Environment.NewLine).Trim()));
        }

        protected virtual string HtmlDecode(string str)
        {
            return HttpUtility.HtmlDecode(str);
        }

        protected virtual string RemoveExcessNewLines(string str)
        {
            string newStr = "";
            string[] parts = str.Split('\n');
            for(int i = 0; i < parts.Length; ++i)
            {
                if (parts.Length > 1)
                {
                    if (i == 0)
                    {
                        newStr += parts[i];
                        if (!parts[i + 1].IsEmptyOrNewLine() && !parts[i].EndWithNewLine()) newStr += Environment.NewLine;
                    }
                    else if (i < parts.Length - 1)
                    {
                        if (parts[i + 1].IsEmptyOrNewLine() && parts[i].IsEmptyOrNewLine()) continue;
                        newStr += parts[i];
                        if (!parts[i].EndWithNewLine()) newStr += Environment.NewLine;
                    }
                    else
                    {
                        if (parts[i - 1].IsEmptyOrNewLine() && parts[i].IsEmptyOrNewLine()) continue;
                        newStr += parts[i];
                    }
                }
            }
            return newStr;
        }

        public abstract Task<string> ParseHtml(string artist, string title, string optional = "");
    }
}
