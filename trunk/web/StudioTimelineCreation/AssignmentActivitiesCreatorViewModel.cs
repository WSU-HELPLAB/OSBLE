using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CreateNewAssignment.AssignmentActivities;
using Newtonsoft.Json;

namespace CreateNewAssignment
{
    public class AssignmentActivitiesCreatorViewModel
    {
        #region Fields

        private AssignmentActivitiesCreatorView thisView = new AssignmentActivitiesCreatorView();
        private MonthViewModel calendarLeft = new MonthViewModel();
        private MonthViewModel calendarRight = new MonthViewModel();
        private List<CalendarDayItemView> listOfDays = new List<CalendarDayItemView>();
        private SortedList<AssignmentActivityViewModel> assignmentActivites = new SortedList<AssignmentActivityViewModel>();
        private AssignmentActivityViewModel aaVMDragging = null;
        private AddActivityMenu activityMenu = new AddActivityMenu();
        private CalendarDayItemView dayRightClickedOn;
        private TimelineViewModel timeline = new TimelineViewModel();

        #endregion Fields

        #region Private Helpers

        private void thisView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Calendar_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            activityMenu.AddNewActivity += new NewActivityEventHandler(activityMenu_AddNewActivity);
            dayRightClickedOn = GetDayFromPoint(GetMouseLocation(e));
            activityMenu.Open();
        }

        private void activityMenu_AddNewActivity(object sender, NewActivityEventArgs e)
        {
            activityMenu.AddNewActivity -= new NewActivityEventHandler(activityMenu_AddNewActivity);

            //we treat this as if they just dropped it onto that calendar day
            droppedAssignmentActivity(dayRightClickedOn, new AssignmentActivityViewModel(e.activity));

            dayRightClickedOn = null;
        }

        private Point GetMouseLocation(MouseEventArgs mouseEvent)
        {
            return mouseEvent.GetPosition(thisView.MouseLayer);
        }

        private void AssignmentActivity_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender == aaVMDragging)
            {
                Point mouseLocation = GetMouseLocation(e);
                if (assignmentActivites.Contains(sender as AssignmentActivityViewModel))
                {
                    AssignmentActivityViewModel aaVM = (sender as AssignmentActivityViewModel);
                    CalendarDayItemView day = GetDayFromPoint(mouseLocation);

                    if (day != null)
                    {
                        //we cannot override an existing activity
                        if (day.AssignmentActivityIconHolder.Content == null)
                        {
                            if (isValidMove(aaVM, day.MyDate))
                            {
                                aaVM.CalendarDay.AssignmentActivityIconHolder.Content = null;
                                day.AssignmentActivityIconHolder.Content = aaVM.GetView();
                                aaVM.CalendarDay = day;
                                aaVM.StartDateTime = day.MyDate;
                                assignmentActivites.Update(aaVM);
                                UpdateActivities();
                            }
                        }
                    }
                }
                else
                {
                    aaVMDragging.GetView().SetValue(Canvas.LeftProperty, mouseLocation.X - aaVMDragging.GetView().ActualWidth / 2);
                    aaVMDragging.GetView().SetValue(Canvas.TopProperty, mouseLocation.Y - aaVMDragging.GetView().ActualHeight / 2);
                }
            }
        }

        /// <summary>
        /// This checks to see if the addition of the aavm with the current date would be valid it does not actually add it to the list
        /// </summary>
        /// <param name="aaVM"></param>
        /// <param name="newDate"></param>
        /// <returns></returns>
        private bool isValidAddition(AssignmentActivityViewModel aaVM, DateTime newDate)
        {
            aaVM.StartDateTime = newDate;
            assignmentActivites.AddInOrder(aaVM);

            bool isValid = AssignmentActivitiesIsValid();

            //we got to remove it
            assignmentActivites.Remove(aaVM);
            return isValid;
        }

        /// <summary>
        /// This checks to see if the addition of the aavm with the current date would be valid it does not actually add it to the list
        /// </summary>
        /// <param name="aaVM"></param>
        /// <param name="newDate"></param>
        /// <returns></returns>
        private bool isValidDeletion(AssignmentActivityViewModel aaVM)
        {
            assignmentActivites.Remove(aaVM);

            bool isValid = AssignmentActivitiesIsValid();

            //we got to add it back in
            assignmentActivites.AddInOrder(aaVM);
            return isValid;
        }

        /// <summary>
        /// Checks to see if the current state of the AssignmentActivitiesIsValid
        /// </summary>
        /// <returns></returns>
        private bool AssignmentActivitiesIsValid()
        {
            if (assignmentActivites.Count == 0)
            {
                return true;
            }
            else
            {
                if (assignmentActivites[0].ActivityType != Activities.Submission)
                {
                    return false;
                }

                if (assignmentActivites.Count > 1)
                {
                    int i = 1;
                    AssignmentActivityViewModel aavmCurrent = assignmentActivites[0];
                    AssignmentActivityViewModel aavmPrevious;
                    while (i < assignmentActivites.Count)
                    {
                        aavmPrevious = aavmCurrent;
                        aavmCurrent = assignmentActivites[i];
                        if (!(AssignmentActivitiesFactory.AllowedPreceedActivities(aavmCurrent.ActivityType).Contains(aavmPrevious.ActivityType)))
                        {
                            return false;
                        }
                        i++;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Checks to see if the move where to occur if the assignmentActivity would be valid
        /// </summary>
        /// <param name="aaVM"></param>
        /// <param name="newDate"></param>
        /// <returns></returns>
        private bool isValidMove(AssignmentActivityViewModel aaVM, DateTime newDate)
        {
            DateTime orginalDay = aaVM.StartDateTime;
            aaVM.StartDateTime = newDate;
            assignmentActivites.Update(aaVM);

            bool isValid = AssignmentActivitiesIsValid();

            //we got to change it back
            aaVM.StartDateTime = orginalDay;
            assignmentActivites.Update(aaVM);
            return isValid;
        }

        private void AssignmentActivityIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startDraggingActivity(new AssignmentActivityViewModel(AssignmentActivitiesFactory.GetActivitiesFromImage(sender as Image)), GetMouseLocation(e));
        }

        private void startDraggingActivity(AssignmentActivityViewModel source, Point mouseLocation)
        {
            aaVMDragging = source;
            source.CaptureMouse();
            source.SetValue(Grid.ColumnSpanProperty, 2);
            source.SetValue(Grid.RowSpanProperty, 2);
            source.SetValue(Canvas.LeftProperty, mouseLocation.X - source.ActualWidth / 2);
            source.SetValue(Canvas.TopProperty, mouseLocation.Y - source.ActualHeight / 2);
            source.MouseMove += new MouseEventHandler(AssignmentActivity_MouseMove);
            source.MouseLeftButtonUp += new MouseButtonEventHandler(dragging_MouseLeftButtonUp);
            thisView.MouseLayer.Children.Add(source.GetView());
        }

        private void dragging_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            thisView.MouseLayer.Children.Remove((sender as AssignmentActivityViewModel).GetView());

            CalendarDayItemView day = GetDayFromPoint(GetMouseLocation(e));

            if (day != null)
            {
                droppedAssignmentActivity(day, aaVMDragging);
            }
            aaVMDragging = null;
        }

        private CalendarDayItemView GetDayFromPoint(Point mouseLocation)
        {
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(mouseLocation, thisView.LayoutRoot);

            var days = from c in elements where c is CalendarDayItemView select c as CalendarDayItemView;

            if (days.Count() > 0)
            {
                return days.First();
            }
            else
            {
                return null;
            }
        }

        private void droppedAssignmentActivity(CalendarDayItemView day, AssignmentActivityViewModel aaVM)
        {
            aaVM.MouseMove -= new MouseEventHandler(AssignmentActivity_MouseMove);
            aaVM.MouseLeftButtonUp -= new MouseButtonEventHandler(dragging_MouseLeftButtonUp);
            Activities activity = aaVM.ActivityType;

            if (isValidAddition(aaVM, day.MyDate))
            {
                addAssignmentActivity(aaVM, day);

            }
            else
            {
                Activities[] allowedActivites = AssignmentActivitiesFactory.AllowedPreceedActivities(aaVM.ActivityType);
                MessageBox.Show(string.Join(" or ", allowedActivites) + " must proceed a(n) " + aaVM.ActivityType.ToString());
            }
        }

        private void addAssignmentActivity(AssignmentActivityViewModel aaVM, CalendarDayItemView day)
        {
            if (day != null)
            {
                aaVM.StartDateTime = day.MyDate;
            }

            aaVM.CalendarDay = day;
            assignmentActivites.AddInOrder(aaVM);
            aaVM.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityViewModel_MouseLeftButtonDown);
            aaVM.MouseRightButtonDown += new MouseButtonEventHandler(AssignmentActivityViewModel_MouseRightButtonDown);
            aaVM.RemoveRequested += new EventHandler(aaVM_RemoveRequested);
            UpdateActivities();
        }

        private void AssignmentActivityViewModel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem mi = new MenuItem();

            mi.Header = "Delete";
            mi.Click += new RoutedEventHandler((sender as AssignmentActivityViewModel).DeleteMenuItem_Clicked);

            menu.Items.Add(mi);

            Point location = e.GetPosition(thisView);

            menu.HorizontalOffset = location.X;
            menu.VerticalOffset = location.Y;

            menu.IsOpen = true;

            e.Handled = true;
        }

        private void aaVM_RemoveRequested(object sender, EventArgs e)
        {
            AssignmentActivityViewModel aavm = (sender as AssignmentActivityViewModel);
            if (isValidDeletion(aavm))
            {
                aavm.Remove();
                assignmentActivites.Remove(aavm);
                UpdateActivities();
            }
            else
            {
                MessageBox.Show("I am sorry this Assignment Activity cannot be removed");
            }
        }

        private void ClearColorOfDays()
        {
            Brush transparent = new SolidColorBrush(Colors.Transparent);

            for (int index = 0; index < listOfDays.Count; index++)
            {
                listOfDays[index].LeftSideColor = transparent;
                listOfDays[index].RightSideColor = transparent;
            }
        }

        private void ChangeColorOfDaysToTheRight(CalendarDayItemView day, Brush color)
        {
            int index = listOfDays.IndexOf(day);

            if (index >= 0)
            {
                listOfDays[index].RightSideColor = color;
                index++;
                while (index < listOfDays.Count)
                {
                    if (listOfDays[index].IsEnabled == true)
                    {
                        listOfDays[index].LeftSideColor = color;
                        if (listOfDays[index].AssignmentActivityIconHolder.Content == null)
                        {
                            listOfDays[index].RightSideColor = color;
                        }
                        else
                        {
                            //we hit something we so we stop
                            break;
                        }
                    }
                    index++;
                }
            }
        }

        private void AssignmentActivityViewModel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AssignmentActivityViewModel aavm = sender as AssignmentActivityViewModel;
            Point mouseLocation = GetMouseLocation(e);

            aaVMDragging = aavm;
            aavm.MouseMove += new MouseEventHandler(AssignmentActivity_MouseMove);
            aavm.CaptureMouse();
        }

        private void RightCalendarScrollButton_Click(object sender, RoutedEventArgs e)
        {
            calendarLeft.MonthYear = calendarLeft.MonthYear.AddMonths(1);
            calendarRight.MonthYear = calendarRight.MonthYear.AddMonths(1);
            UpdateCalendars();
        }

        private void LeftCalendarScrollButton_Click(object sender, RoutedEventArgs e)
        {
            calendarLeft.MonthYear = calendarLeft.MonthYear.AddMonths(-1);
            calendarRight.MonthYear = calendarRight.MonthYear.AddMonths(-1);
            UpdateCalendars();
        }

        private void UpdateCalendars()
        {
            ClearCalendarMonth(calendarLeft);
            ClearCalendarMonth(calendarRight);
            ClearColorOfDays();

            AssignmentActivityViewModel aavm = GetPreviousAAVMFromDate(calendarLeft.MonthYear);

            if (calendarRight.MonthYear.Day != DateTime.DaysInMonth(calendarRight.MonthYear.Year, calendarRight.MonthYear.Month))
            {
                calendarRight.MonthYear.AddDays(DateTime.DaysInMonth(calendarRight.MonthYear.Year, calendarRight.MonthYear.Month));
            }

            aavm = assignmentActivites.GetNextItem(aavm);

            CalendarDayItemView day = null;
            if (aavm != null)
            {
                day = findCalendarDayItemView(aavm.StartDateTime);
            }
            while (day != null)
            {
                day.AssignmentActivityIconHolder.Content = aavm.GetView();
                aavm.CalendarDay = day;

                aavm = assignmentActivites.GetNextItem(aavm);
                if (aavm == null)
                {
                    day = null;
                }
                else
                {
                    day = findCalendarDayItemView(aavm.StartDateTime);
                }
            }

            UpdateActivities();
        }

        private CalendarDayItemView findCalendarDayItemView(DateTime dateTime)
        {
            CalendarDayItemView day = calendarLeft.GetCalendarDayItemView(dateTime);
            if (day == null)
            {
                day = calendarRight.GetCalendarDayItemView(dateTime);
            }

            return day;
        }

        private AssignmentActivityViewModel GetPreviousAAVMFromDate(DateTime dateTime)
        {
            int index = assignmentActivites.FindInsertionSpot(new AssignmentActivityViewModel(Activities.Null, dateTime));

            if (index <= 0)
            {
                return null;
            }
            else
            {
                return assignmentActivites[index - 1];
            }
        }

        private void ClearCalendarMonth(MonthViewModel mvm)
        {
            foreach (AssignmentActivityViewModel aavm in assignmentActivites)
            {
                if (aavm.CalendarDay != null)
                {
                    aavm.CalendarDay.AssignmentActivityIconHolder.Content = null;
                    aavm.CalendarDay = null;
                }
            }
        }

        private AssignmentActivityViewModel findFirstActivityAfterDay(DateTime time)
        {
            int index = assignmentActivites.FindInsertionSpot(new AssignmentActivityViewModel(Activities.Null, time));
            return assignmentActivites[index];
        }

        #endregion Private Helpers

        #region Public Methods

        public void UpdateActivities()
        {
            timeline.UpdateTimeline(assignmentActivites);
            ClearColorOfDays();
            if (assignmentActivites.Count != 0)
            {
                int index = assignmentActivites.FindInsertionSpot(new AssignmentActivityViewModel(Activities.Null, calendarLeft.MonthYear));
                if (index > 0)
                {
                    ChangeColorOfDaysToTheRight(calendarLeft.GetCalendarDayItemView(calendarLeft.MonthYear), assignmentActivites[index - 1].ActivityColor);
                }

                if (index < 0)
                {
                    index = 0;
                }
                while (index < assignmentActivites.Count)
                {
                    AssignmentActivityViewModel aavm = assignmentActivites[index];
                    ChangeColorOfDaysToTheRight(aavm.CalendarDay, aavm.ActivityColor);
                    index++;
                }
            }
        }

        public AssignmentActivitiesCreatorViewModel(string SerializedActivitiesJSON)
        {
            //adding the calendars
            calendarLeft.MonthYear = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            calendarRight.MonthYear = calendarLeft.MonthYear.AddMonths(1);
            calendarLeft.GetView().SetValue(Grid.RowProperty, 2);
            calendarLeft.GetView().SetValue(Grid.ColumnProperty, 0);
            calendarRight.GetView().SetValue(Grid.RowProperty, 2);
            calendarRight.GetView().SetValue(Grid.ColumnProperty, 2);
            //done adding the calendars

            thisView.LayoutRoot.Children.Add(calendarRight.GetView());
            thisView.LayoutRoot.Children.Add(calendarLeft.GetView());

            //adding timeline
            timeline.GetView().SetValue(Grid.RowProperty, 3);
            timeline.GetView().SetValue(Grid.ColumnSpanProperty, 3);
            timeline.GetView().SizeChanged += new SizeChangedEventHandler(TimelineSizeChanged);
            thisView.LayoutRoot.Children.Add(timeline.GetView());
            //done adding timeline

            calendarLeft.MouseRightButtonDown += new MouseButtonEventHandler(Calendar_MouseRightButtonDown);
            calendarRight.MouseRightButtonDown += new MouseButtonEventHandler(Calendar_MouseRightButtonDown);

            thisView.MouseRightButtonDown += new MouseButtonEventHandler(thisView_MouseRightButtonDown);
            thisView.LeftCalendarScrollButton.Click += new RoutedEventHandler(LeftCalendarScrollButton_Click);
            thisView.RightCalendarScrollButton.Click += new RoutedEventHandler(RightCalendarScrollButton_Click);

            thisView.SubmissionIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);
            thisView.PeerReviewIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);
            thisView.IssueVotingIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);
            thisView.AuthorRebuttalIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);
            thisView.StopIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);

            foreach (UIElement ui in calendarLeft.GetView().MonthLayout.Children)
            {
                if (ui is CalendarDayItemView)
                {
                    listOfDays.Add(ui as CalendarDayItemView);
                }
            }

            foreach (UIElement ui in calendarRight.GetView().MonthLayout.Children)
            {
                if (ui is CalendarDayItemView)
                {
                    listOfDays.Add(ui as CalendarDayItemView);
                }
            }

            // Deserialize activities from init params string,
            // and place them into calendar.

            List<SerializableActivity> activities = JsonConvert.DeserializeObject<List<SerializableActivity>>(SerializedActivitiesJSON);

            foreach(SerializableActivity sa in activities) {
                CalendarDayItemView day = findCalendarDayItemView(sa.DateTime);

                AssignmentActivityViewModel aaVM;

                // Default is stop.
                switch (sa.ActivityType)
                {
                    case ActivityTypes.Submission:
                        aaVM = new AssignmentActivityViewModel(Activities.Submission);
                        break;
                    case ActivityTypes.PeerReview:
                        aaVM = new AssignmentActivityViewModel(Activities.PeerReview);
                        break;
                    case ActivityTypes.Voting:
                        aaVM = new AssignmentActivityViewModel(Activities.IssueVoting);
                        break;
                    case ActivityTypes.Rebuttal:
                        aaVM = new AssignmentActivityViewModel(Activities.AuthorRebuttal);
                        break;
                    default:
                        aaVM = new AssignmentActivityViewModel(Activities.Stop);
                        break;
                }

                aaVM.StartDateTime = sa.DateTime;

                if (isValidAddition(aaVM, aaVM.StartDateTime.Date))
                {
                    addAssignmentActivity(aaVM, day);
                }
            }

            //UpdateActivityDisplay();

        }

        void TimelineSizeChanged(object sender, SizeChangedEventArgs e)
        {
            timeline.UpdateTimeline(assignmentActivites);
        }

        public AssignmentActivitiesCreatorView GetView()
        {
            return thisView;
        }

        #endregion Public Methods

        public class SerializableActivity
        {
            public DateTime DateTime { get; set; }

            public ActivityTypes ActivityType { get; set; }
        }

        public enum ActivityTypes
        {
            Submission,
            PeerReview,
            Voting,
            Rebuttal,
            Stop
        }
    }
}