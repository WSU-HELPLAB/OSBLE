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
    public class AddActivityMenu : ContextMenu
    {
        public event NewActivityEventHandler AddNewActivity = delegate { };

        public AddActivityMenu()
        {
            MenuItem menuItem = new MenuItem();

            menuItem.Header = "Add Submission";
            menuItem.Icon = new Image() { Source = AssignmentActivitiesFactory.GetImageSourceFromActivities(Activities.Submission), Height = 20, Width = 20 };
            menuItem.Click += new RoutedEventHandler(menuItemClicked);
            this.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Add Peer Review";
            menuItem.Icon = new Image() { Source = AssignmentActivitiesFactory.GetImageSourceFromActivities(Activities.PeerReview), Height = 20, Width = 20 };
            menuItem.Click += new RoutedEventHandler(menuItemClicked);
            this.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Add Issue Voting";
            menuItem.Icon = new Image() { Source = AssignmentActivitiesFactory.GetImageSourceFromActivities(Activities.IssueVoting), Height = 20, Width = 20 };
            menuItem.Click += new RoutedEventHandler(menuItemClicked);
            this.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Add Author Rebuttal";
            menuItem.Icon = new Image() { Source = AssignmentActivitiesFactory.GetImageSourceFromActivities(Activities.AuthorRebuttal), Height = 20, Width = 20 };
            menuItem.Click += new RoutedEventHandler(menuItemClicked);
            this.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Header = "Add Stop";
            menuItem.Icon = new Image() { Source = AssignmentActivitiesFactory.GetImageSourceFromActivities(Activities.Stop), Height = 20, Width = 20 };
            menuItem.Click += new RoutedEventHandler(menuItemClicked);
            this.Items.Add(menuItem);
        }

        void menuItemClicked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            Activities activity = AssignmentActivitiesFactory.GetActivitiesFromImage(menuItem.Icon as Image);

            AddNewActivity(this, new NewActivityEventArgs(activity));
        }

        /// <summary>
        /// for some unknown reason this opens where the mouse is even though 0,0 should open in the top left corner given previous experience
        /// </summary>
        public void Open()
        {
            this.IsOpen = true;
        }

    }
}
