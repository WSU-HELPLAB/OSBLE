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


namespace CreateNewAssignment.AssignmentActivities
{
    public delegate void NewActivityEventHandler(object sender, NewActivityEventArgs e);

    public class NewActivityEventArgs : EventArgs
    {
        public readonly Activities activity;

        public NewActivityEventArgs(Activities activity)
        {
            this.activity = activity;
        }

        public NewActivityEventArgs()
        {
            this.activity = Activities.Null;
        }
    }
}
