using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Text.RegularExpressions;

namespace OSBLE.Resources
{
    public class CsvParser
    {
        StreamReader CSV;

        public CsvParser(Stream csv)
        {
            csv.Position = 0;
            CSV = new StreamReader(csv);
        }

        /// <summary>
        /// get the next row in the csv file stream. Each string in the list is one cell.
        /// </summary>
        /// <returns>List of strings, representing a row in the CSV. Returns null of the end has been reached.</returns>
        public List<string> getNextRow()
        {
            List<string> row = new List<string>();

            string line = CSV.ReadLine();
            if (line == null)
            {
                return null;
            }

            Regex regex = new Regex("(\".+\")|(([^,])+?(?=(,|$)))");

            MatchCollection matches = regex.Matches(line);

            for (int i = 0; i < matches.Count; i++)
            {
                row.Add( matches[i].ToString() );
            }

            return row;
        }

    }
}
