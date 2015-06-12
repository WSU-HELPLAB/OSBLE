using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OsbleRubric
{
    public class HeaderCell : ICell
    {
        int comboBoxValue;

        public HeaderCell(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int ComboBoxValue
        {
            get { return comboBoxValue; }
            set { comboBoxValue = value; }
        }

    }
}
