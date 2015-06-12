using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Resources.CSVReader
{
    //SecondQuoteState
    public class SecondQuoteState : CSVState
    {
        CSVDriver _CSVDriver;

        public SecondQuoteState(CSVDriver CSVDriver)
        {
            _CSVDriver = CSVDriver;
        }

        public void Handle()
        {
            char charTohandle = _CSVDriver.GetNextCharacter();

            if (charTohandle == ',')
            {
                _CSVDriver.SetStateToEndState();
            }
            else if (charTohandle == '"')
            {
                //Add this quote, this is the only place quotes are wrote into the string
                _CSVDriver.AppendToCurrentCell(charTohandle);
                _CSVDriver.SetStateToQuoteState();
            }
            else
            {
                _CSVDriver.AppendToCurrentCell(charTohandle);
                _CSVDriver.SetStateToDefaultState();
            }
        }
    }

    
}