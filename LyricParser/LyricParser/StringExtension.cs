using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LyricParser
{
    public static class StringExtension
    {
        public static string StartWithUpperCase(this String str, int index)
        {
            char[] chars = str.ToCharArray();

            bool firstLetter = true;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsLetter(c) && firstLetter && index == 0)
                {
                    chars[i] = c.ToString().ToUpper()[0];
                    firstLetter = false;
                }
            }
            return chars.GetString();
        }

        public static string StartWithLowerCase(this String str, int index)
        {
            char[] chars = str.ToCharArray();

            bool firstLetter = true;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsLetter(c) && firstLetter)
                {
                    chars[i] = c.ToString().ToLower()[0];
                    firstLetter = false;
                }
            }

            return chars.GetString();
        }

        public static string Replace(this string s, char[] separators, string newVal)
        {
            string[] temp;

            temp = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return String.Join(newVal, temp);
        }

        public static string Replace(this string s, string[] separators, string newVal)
        {
            string[] temp;

            temp = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return String.Join(newVal, temp);
        }

        public static int IndexOfNth(this String str, char value, int nth = 0)
        {
            if (nth >= 0)
            {
                int offset = str.IndexOf(value);
                for (int i = 0; i < nth; i++)
                {
                    if (offset == -1) return -1;
                    offset = str.IndexOf(value, offset + 1);
                }
                return offset;
            }
            else return -1;
        }

        public static string RemoveBracket(this String str, char chr, string[] exept = null)
        {
            if (str.Contains(chr))
            {
                string tmp = str;
                //if(exept != null)
                //{
                //    if (exept.Any(e => str.Contains(e))) return str;
                //}

                //char[] special = new char[] { '[', '{' };
                //int offset = 1;
                //int start = str.IndexOf(chr);
                //if (special.Any(s => chr.Equals(s))) offset = 2;
                //int end = str.IndexOf((char)((int)chr + offset));

                //return str.Remove(start, end - start + 1);
                char[] special = new char[] { '[', '{' };

                for (int i = 0; i < str.Count(c => c == chr); ++i)
                {
                    if (exept != null && exept.Any(e => tmp.GetBracketContent(chr, 0).StartsWith(e))) continue;
                    int start = tmp.IndexOfNth(chr, 0);
                    int offset = 1;

                    if (special.Any(s => chr.Equals(s))) offset = 2;
                    int end = tmp.IndexOfNth((char)((int)chr + offset), 0);
                    tmp = tmp.Remove(start, end - start + 1);
                }

                return tmp;
            }
            else return str;
        }

        public static string GetBracketContent(this String str, char chr, int index = 0)
        {
            if (str.Contains(chr))
            {
                char[] special = new char[] { '[', '{' };
                int offset = 1;
                int start = str.IndexOfNth(chr, index);
                if (special.Any(s => chr.Equals(s))) offset = 2;
                int end = str.IndexOfNth((char)((int)chr + offset), index);

                return str.Substring(start + 1, end - start - 1);
            }
            else return str;
        }

        public static string CleanHTML(this String str)
        {
            str = Regex.Replace(str, "<.*?>", string.Empty);
            str = str.Replace("&#160;", "");
            str = str.Replace("\n\n\n", "\n");
            str = str.Replace("\n\n", "\n");
            str = str.Replace("&lt;", "<");
            str = str.Replace("&gt;", ">");

            return str;
        }

        public static string GetString(this char[] array)
        {
            string str = "";
            foreach (char c in array)
            {
                str += c.ToString();
            }
            return str;
        }

        public static bool IsNumeric(this String Value)
        {
            try
            {
                double.Parse(Value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetString (this List<string> list)
        {
            string str = "";
            foreach(string s in list)
            {
                str += "{" + s + "}";
            }
            return str;
        }
    }
}
