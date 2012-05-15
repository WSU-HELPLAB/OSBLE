using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Resources
{
    public static class StringExtensions
    {
        /// <summary>
        /// Turns the first character of each word into upper case
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string ToFirstCharUpper(this string item)
        {
            string[] pieces = item.Split(' ');
            for (int i = 0; i < pieces.Length; i++)
            {
                char first = char.ToUpper(pieces[i][0]);
                pieces[i] = string.Format("{0}{1}", first, pieces[i].Substring(1));
            }
            string final = string.Join(" ", pieces);
            return final;
        }
    }
}