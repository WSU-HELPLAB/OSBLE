using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text;

namespace OSBLE.Resources.CSVReader
{
    public class CSVDriver
    {
        private CSVState _currentState;
        private CSVState _defaultState;
        private CSVState _quoteState;
        private CSVState _secondQuoteState;
        private CSVState _endState;

        private StreamReader _CSVStream;

        private List<List<string>> _table;
        private List<string> _currentRow;

        private string currentLine;
        private int currentLinePos;

        private StringBuilder currentCell;

        
        /// <summary>
        /// Object that drives a state machine that will read a CSV cell by cell. 
        /// </summary>
        /// <param name="CSVStream"></param>
        public CSVDriver(Stream CSVStream)
        {
            CSVStream.Position = 0;
            _CSVStream = new StreamReader(CSVStream);
            currentCell = new StringBuilder();
            _table = new List<List<string>>();
            _currentRow = new List<string>();

            //Initialize all states
            _defaultState = new DefaultState(this);
            _quoteState = new QuoteState(this);
            _secondQuoteState = new SecondQuoteState(this);
            _endState = new EndState(this);

            //Set the current state in defaultState
            _currentState = _defaultState;
        }


        /// <summary>
        /// This function Drives the CSV input through a state machine (see CSVCellReaderStateMachine.png) that grabs one cell at a time
        /// until an entire table is built up.
        /// </summary>
        /// <returns></returns>
        public List<List<string>> Drive()
        {

            currentLine = _CSVStream.ReadLine();

            while (currentLine != null)
            {
                _currentState.Handle();


                //If the _currentState is EndState or the line has been exhausted, the cell is finished.
                if (_currentState is EndState || currentLinePos == currentLine.Length)
                {
                    //Add our currentCell into our currentRow and clear the currentCell.
                    _currentRow.Add(currentCell.ToString());
                    currentCell.Clear();

                    //change state back to default
                    SetStateToDefaultState();

                    if (currentLinePos == currentLine.Length) //The line is over
                    {

                        //If the last charcter in the line is a ',' that means there was one more empty cell to append to the 
                        //row. 
                        try
                        {
                            if (currentLine[currentLinePos - 1] == ',')
                            {
                                _currentRow.Add("");
                            }
                        }
                        catch (Exception)
                        {
                        }



                        //Add the row into our table and reset the currentRow, get a new currentLine, and reset currentLinePos
                        _table.Add(_currentRow.ToList());
                        _currentRow.Clear();
                        currentLinePos = 0;
                        currentLine = _CSVStream.ReadLine();
                    }
                }
            }
            return _table;
        }


        /// <summary>
        /// Appends a character to the current cell that is being constructed.
        /// </summary>
        /// <param name="charToAppend"></param>
        public void AppendToCurrentCell(char charToAppend)
        {
            currentCell.Append(charToAppend);
        }

        /// <summary>
        /// Gets the next character in the line and increases the value of currentLinePos by 1.
        /// </summary>
        /// <returns></returns>
        public char GetNextCharacter()
        {
            char returnVal = ' ';
            try
            {
                returnVal = this.currentLine[this.currentLinePos];
                this.currentLinePos++;
            }
            catch(Exception)
            {

            }
            
            return returnVal;
        }

        public void SetStateToDefaultState()
        {
            _currentState = _defaultState;
        }

        public void SetStateToQuoteState()
        {
            _currentState = _quoteState;
        }

        public void SetStateToSecondQuoteState()
        {
            _currentState = _secondQuoteState;
        }

        public void SetStateToEndState()
        {
            _currentState = _endState;
        }


    }
}