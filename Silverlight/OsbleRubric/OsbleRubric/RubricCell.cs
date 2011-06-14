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
    public class RubricCell : ICell
    {

        private bool isComboBox;
        private bool isCheckBox;

        public RubricCell(int row, int column, string description)
        {
            isComboBox = false;
            Row = row;
            Column = column;
            Information = description;
        }

        public bool IsComboBox
        {
            get { return isComboBox; }
            set { isComboBox = value; }
        }
        public bool IsCheckBox
        {
            get { return isCheckBox; }
            set { isCheckBox = value; }
        }


    }
}
