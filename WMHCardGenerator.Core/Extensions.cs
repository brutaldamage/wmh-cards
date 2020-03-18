using System;
using System.Text.RegularExpressions;

namespace WMHCardGenerator.Core
{
    public static class Extensions
    {
        public static string RemoveBetween(this string s, char begin, char end)
        {
            Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", begin, end));
            return regex.Replace(s, string.Empty);
        }
    }
}
