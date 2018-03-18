using System;
using System.Collections.Generic;
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

            bool secondWordFirstLetter = false;

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
    }
}
