using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Resources.CSVReader
{
    //EndState
    public class EndState : CSVState
    {
        CSVDriver _CSVDriver;

        public EndState(CSVDriver CSVDriver)
        {
            _CSVDriver = CSVDriver;
        }

        public void Handle()
        {
            //End state does nothing. Let driver handle it from here.
        }
    }
}