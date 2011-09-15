using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace CreateNewAssignment
{
    public partial class CalendarDayItemView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private Brush rightSideColor = new SolidColorBrush(Colors.Transparent);
        private Brush leftSideColor = new SolidColorBrush(Colors.Transparent);
        private string date = " ";
        private ImageSource assignmentPhaseIconPath;
        private DateTime myDate;

        public DateTime MyDate
        {
            get { return myDate; }
            set { myDate = value; }
        }

        public ImageSource AssignmentPhaseIconPath
        {
            get { return assignmentPhaseIconPath; }
            set 
            {
                assignmentPhaseIconPath = value; 
                PropertyChanged(this, new PropertyChangedEventArgs("AssignmentPhaseIconPath"));
            }
        }

        public string DateString
        {
            get{ return date; }
            set
            {
                date = value;
                PropertyChanged(this, new PropertyChangedEventArgs("DateString"));
            }
        }

        public Brush RightSideColor
        {
            get { return rightSideColor; }
            set 
            {
                rightSideColor = value;
                PropertyChanged(this, new PropertyChangedEventArgs("RightSideColor"));
            }
        }

        public Brush LeftSideColor
        {
            get { return leftSideColor; }
            set 
            {
                leftSideColor = value;
                PropertyChanged(this, new PropertyChangedEventArgs("LeftSideColor"));
            }
        }

        public CalendarDayItemView()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public void ClearData()
        {
            RightSideColor = new SolidColorBrush(Colors.Transparent);
            LeftSideColor = new SolidColorBrush(Colors.Transparent);
            DateString = " ";
            AssignmentPhaseIconPath = null;
            MyDate = default(DateTime);
        }

    }
}
