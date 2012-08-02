using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text;
using OSBLE.Resources;

namespace OSBLE.Resources.CSVReader
{

    public class CSVReader
    {
        private CSVDriver _CSVDriver;

        public CSVReader(Stream CSVStream)
        {
            _CSVDriver = new CSVDriver(CSVStream);
        }

        /// <summary>
        /// Parses the CSV into a List of List of strings. Where each List of strings represents 1 row."
        /// </summary>
        public List<List<string>> Parse()
        {
            return _CSVDriver.Drive();
        }
    }

    

    

    

    

    

}