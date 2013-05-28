using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Resources.CSVReader
{
    //QuoteState
    public class QuoteState : CSVState
    {
        CSVDriver _CSVDriver;

        public QuoteState(CSVDriver CSVDriver)
        {
            _CSVDriver = CSVDriver;
        }

        public void Handle()
        {
            char charTohandle = _CSVDriver.GetNextCharacter();


            if (charTohandle == '"')
            {
                //Eat this quote, do not add it into current word
                _CSVDriver.SetStateToSecondQuoteState();
            }
            else
            {
                _CSVDriver.AppendToCurrentCell(charTohandle);
            }
        }
    }
}