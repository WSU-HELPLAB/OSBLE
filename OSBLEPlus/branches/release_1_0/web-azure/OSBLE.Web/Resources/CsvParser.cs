using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

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
        /// get the next row in the csv file stream. Each string in the list represents one cell.
        /// Example usage:
        ///     do {
        ///         List<string> l = getNextRow();
        ///     } while( l != null);
        /// </summary>
        /// <returns>List of strings, representing a row in the CSV. Returns null if the end has been reached.</returns>
        public List<string> getNextRow()
        {
            if (CSV.EndOfStream)
            {
                return null;
            }
            List<string> row = new List<string>();
            StringBuilder cell = new StringBuilder();

            do
            {
                char c = (char)CSV.Read();

                if (c == '"')
                {
                    //read anything until endofstream or another " (NOT "")
                    do
                    {
                        c = (char)CSV.Read();
                        if (c == '"')
                        {
                            c = (char)CSV.Read();
                            if (c == '"')
                            {
                                cell.Append(c);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            cell.Append(c);
                        }
                    } while (!CSV.EndOfStream);
                }

                if (c == '\r')
                {
                    CSV.Read();
                    break;
                }
                else if (c == ',')
                {
                    if (cell.Length == 0)
                    {
                        cell.Append("");
                    }
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else
                {
                    cell.Append(c);
                }
            } while (!CSV.EndOfStream);
            
            //reached end of stream, but haven't added the last field yet

                row.Add(cell.ToString());

            return row;
        }

    }
}
