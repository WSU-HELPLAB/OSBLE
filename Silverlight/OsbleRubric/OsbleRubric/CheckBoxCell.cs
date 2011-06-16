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
    public class CheckBoxCell : ICell
    {
        bool checkBoxValue;

        public CheckBoxCell(int row, int column, bool checkBoxValue)
        {
            CheckBoxValue = checkBoxValue;
        }

        public bool CheckBoxValue
        {
            get { return checkBoxValue; }
            set { checkBoxValue = value; }
        }
    }
}
