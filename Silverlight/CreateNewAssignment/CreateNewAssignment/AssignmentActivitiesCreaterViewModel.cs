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
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

using CreateNewAssignment.AssignmentActivities;
using System.Windows.Media.Imaging;

namespace CreateNewAssignment
{
    public class AssignmentActivitiesCreaterViewModel
    {
        private AssignmentActivitiesCreaterView thisView = new AssignmentActivitiesCreaterView();

        private MonthViewModel calanderLeft = new MonthViewModel();
        private MonthViewModel calanderRight = new MonthViewModel();
        private List<CalendarDayItemView> listOfDays = new List<CalendarDayItemView>();
        private SortedList<AssignmentActivityViewModel> assignmentActivites = new SortedList<AssignmentActivityViewModel>();
        private AssignmentActivityViewModel aaVMDragging = null;
        private AddActivityMenu activityMenu = new AddActivityMenu();
        private CalendarDayItemView dayRightClickedOn;

        public AssignmentActivitiesCreaterViewModel()
        {
            calanderLeft.MonthYear = new DateTime(2011, 7, 1);
            calanderRight.MonthYear = new DateTime(2011, 8, 1);
            calanderLeft.GetView().SetValue(Grid.RowProperty, 2);
            calanderLeft.GetView().SetValue(Grid.ColumnProperty, 0);
            calanderRight.GetView().SetValue(Grid.RowProperty, 2);
            calanderRight.GetView().SetValue(Grid.ColumnProperty, 2);

            thisView.LayoutRoot.Children.Add(calanderRight.GetView());
            thisView.LayoutRoot.Children.Add(calanderLeft.GetView());

            calanderLeft.MouseRightButtonDown += new MouseButtonEventHandler(Calander_MouseRightButtonDown);
            calanderRight.MouseRightButtonDown += new MouseButtonEventHandler(Calander_MouseRightButtonDown);

            thisView.MouseRightButtonDown += new MouseButtonEventHandler(thisView_MouseRightButtonDown);
            thisView.LeftCalanderScrollButton.Click += new RoutedEventHandler(LeftCalanderScrollButton_Click);
            thisView.RightCalanderScrollButton.Click += new RoutedEventHandler(RightCalanderScrollButton_Click);
            //thisView.test.Click += new RoutedEventHandler(testBtn_click);

            thisView.SubmissionIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);
            thisView.PeerReviewIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);
            thisView.IssueVotingIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);
            thisView.AuthorRebuttalIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);
            thisView.StopIcon.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityIcon_MouseLeftButtonDown);

            foreach (UIElement ui in calanderLeft.GetView().MonthLayout.Children)
            {
                if (ui is CalendarDayItemView)
                {
                    listOfDays.Add(ui as CalendarDayItemView);
                }
            }

            foreach (UIElement ui in calanderRight.GetView().MonthLayout.Children)
            {
                if (ui is CalendarDayItemView)
                {
                    listOfDays.Add(ui as CalendarDayItemView);
                }
            }

        }

        void thisView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        void Calander_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            activityMenu.AddNewActicity += new NewActivityEventHandler(activityMenu_AddNewActicity);
            dayRightClickedOn = GetDayFromPoint(GetMouseLocation(e));
            activityMenu.Open();
        }

        void activityMenu_AddNewActicity(object sender, NewActivityEventArgs e)
        {
            activityMenu.AddNewActicity -= new NewActivityEventHandler(activityMenu_AddNewActicity);

            //we treat this as if they just dropped it onto that calendar day
            droppedAssignmentActivity(dayRightClickedOn, new AssignmentActivityViewModel(e.activity));

            dayRightClickedOn = null;
        }

        Point GetMouseLocation(MouseEventArgs mouseEvent)
        {
            return mouseEvent.GetPosition(thisView.MouseLayer);
        }


        void AssignmentActivity_MouseMove(object sender, MouseEventArgs e)
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
                                UpdateActivityDisplay();
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
        bool isValidAddition(AssignmentActivityViewModel aaVM, DateTime newDate)
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
        bool isValidDeletion(AssignmentActivityViewModel aaVM)
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
        bool AssignmentActivitiesIsValid()
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
        bool isValidMove(AssignmentActivityViewModel aaVM, DateTime newDate)
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

        void AssignmentActivityIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startDraggingActivity(new AssignmentActivityViewModel(AssignmentActivitiesFactory.GetActivitiesFromImage(sender as Image)), GetMouseLocation(e));
        }

        void startDraggingActivity(AssignmentActivityViewModel source, Point mouseLocation)
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

        void dragging_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            thisView.MouseLayer.Children.Remove((sender as AssignmentActivityViewModel).GetView());

            
            CalendarDayItemView day = GetDayFromPoint(GetMouseLocation(e));

            
            if (day != null)
            {
                droppedAssignmentActivity(day, aaVMDragging);
            }
            aaVMDragging = null;
        }

        CalendarDayItemView GetDayFromPoint(Point mouseLocation)
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

        void droppedAssignmentActivity(CalendarDayItemView day, AssignmentActivityViewModel aaVM)
        {

            aaVM.MouseMove -= new MouseEventHandler(AssignmentActivity_MouseMove);
            aaVM.MouseLeftButtonUp -= new MouseButtonEventHandler(dragging_MouseLeftButtonUp);
            Activities activity = aaVM.ActivityType;

            if (isValidAddition(aaVM, day.MyDate))
            {
                aaVM.StartDateTime = day.MyDate;
                aaVM.CalendarDay = day;
                assignmentActivites.AddInOrder(aaVM);
                aaVM.MouseLeftButtonDown += new MouseButtonEventHandler(AssignmentActivityViewModel_MouseLeftButtonDown);
                aaVM.MouseRightButtonDown += new MouseButtonEventHandler(AssignmentActivityViewModel_MouseRightButtonDown);
                aaVM.RemoveRequested += new EventHandler(aaVM_RemoveRequested);
                UpdateActivityDisplay();
                /*
                MessageBox.Show(aaVM.StartDateTime.ToString());
                MessageBox.Show(aaVM.ActivityType.ToString());
                */
                
            }
            else
            {
                Activities[] allowedActivites = AssignmentActivitiesFactory.AllowedPreceedActivities(aaVM.ActivityType);
                MessageBox.Show(string.Join(" or ", allowedActivites) + " must proceed a(n) " + aaVM.ActivityType.ToString());
            }
        }

        void AssignmentActivityViewModel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
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

        void aaVM_RemoveRequested(object sender, EventArgs e)
        {
            AssignmentActivityViewModel aavm = (sender as AssignmentActivityViewModel);
            if (isValidDeletion(aavm))
            {
                aavm.Remove();
                assignmentActivites.Remove(aavm);
                UpdateActivityDisplay();
            }
            else
            {
                MessageBox.Show("I am sorry this Assignment Activity cannot be removed");
            }
        }

        public void UpdateActivityDisplay()
        {
            displayTimeline();
            ClearColorOfDays();

            
            int index = assignmentActivites.FindInsertionSpot(new AssignmentActivityViewModel(Activities.Null, calanderLeft.MonthYear));
            if(index > 0)
            {
                ChangeColorOfDaysToTheRight(calanderLeft.GetCalendarDayItemView(calanderLeft.MonthYear), assignmentActivites[index-1].ActivityColor);
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

        void ClearColorOfDays()
        {
            Brush transparent = new SolidColorBrush(Colors.Transparent);

            for (int index = 0; index < listOfDays.Count; index++ )
            {
                    listOfDays[index].LeftSideColor = transparent;
                    listOfDays[index].RightSideColor = transparent;
            }
        }

        void ChangeColorOfDaysToTheRight(CalendarDayItemView day, Brush color)
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

        void AssignmentActivityViewModel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AssignmentActivityViewModel aavm = sender as AssignmentActivityViewModel;
            Point mouseLocation = GetMouseLocation(e);

            aaVMDragging = aavm;
            aavm.MouseMove += new MouseEventHandler(AssignmentActivity_MouseMove);
            aavm.CaptureMouse();
        }

        void RightCalanderScrollButton_Click(object sender, RoutedEventArgs e)
        {
            calanderLeft.MonthYear = calanderLeft.MonthYear.AddMonths(1);
            calanderRight.MonthYear = calanderRight.MonthYear.AddMonths(1);
            UpdateCalanders();
        }

        /// <summary>
        /// removes the children from all the stackpanels associated with timeline (labels for date, time, icons for event, and rects for event)
        /// </summary>
        void clearTimeline()
        {
            for (int i = thisView.Timeline.Children.Count - 1; i >= 0; i--)
            {
                thisView.Timeline.Children.RemoveAt(i);
            }
            for (int i = thisView.TimelineDates1.Children.Count - 1; i >= 0; i--)
            {
                thisView.TimelineDates1.Children.RemoveAt(i);
            }
            for (int i = thisView.TimelineTimes.Children.Count - 1; i >= 0; i--)
            {
                thisView.TimelineTimes.Children.RemoveAt(i);
            }
            for (int i = thisView.TimelineIcons.Children.Count - 1; i >= 0; i--)
            {
                thisView.TimelineIcons.Children.RemoveAt(i);
            }
            for (int i = thisView.TimelineLines.Children.Count - 1; i >= 0; i--)
            {
                thisView.TimelineLines.Children.RemoveAt(i);
            }
        }

        /// <summary>
        /// given an activity, returns the color associated with that activity
        /// </summary>
        Color getEventFillColor(Activities activity)
        {
            switch (activity) //assigning color
            {
                case Activities.Submission: return Colors.Green;
                case Activities.PeerReview: return Colors.Blue;
                case Activities.IssueVoting: return Colors.Purple;
                case Activities.AuthorRebuttal: return Colors.Red;
                case Activities.Stop: return Colors.White;
                default: return Colors.Orange;
            }   
        }

        /// <summary>
        /// returns a list of rectangles that are no smaller than minRectWidth, stretch timelineWidth, and each rectangles width corrisponds to how much time it takes
        /// </summary>
        List<Rectangle> getRectangles(double minRectWidth, double rectHeight, double timelineWidth)
        {
            double TotalTime = TotalEventTime();
            bool endsWithStop = CalenderEndsWithStop();
            double minEventWidth = minRectWidth; //in pixels
            int eventsWithMinWidth = 1; //starts at 1 because final event always has min width
            double timeNotConsidered = 0;
            double timelineHeight = rectHeight;

            List<Rectangle> rectList = new List<Rectangle>();
            List<int> dontCheck = new List<int>();

            bool allValid;
            do
            {
                allValid = true;

                for (int i = 0; i < assignmentActivites.Count; i++)
                {
                    if (i >= rectList.Count)
                        rectList.Add(new Rectangle());

                    if (assignmentActivites.Count == 1) //only 1 activity, make it expand whole timeline
                    {
                        //rectList[i].Width = thisView.LayoutRoot.MinWidth;
                        rectList[i].Width = timelineWidth;
                    }
                    else if (i == (assignmentActivites.Count - 1)) //last activity, make it expand exactly the min width
                    {
                        rectList[i].Width = minEventWidth;
                    }
                    else //normal activity, width it set on of timetaken / totaltime, with a minimum width of minEventWidth
                    {
                        if (!dontCheck.Contains(i)) //if it was determined that its width was less than minEventWidth previously, dont change it. 
                           // rectList[i].Width = ((TimeUntilNextEventInDays(assignmentActivites[i])) / (TotalTime - timeNotConsidered)) * (thisView.LayoutRoot.MinWidth - minEventWidth * eventsWithMinWidth);
                            rectList[i].Width = ((TimeUntilNextEventInDays(assignmentActivites[i])) / (TotalTime - timeNotConsidered)) * (timelineWidth - minEventWidth * eventsWithMinWidth);

                        if (rectList[i].Width < minEventWidth)//if any width is less than minEventWidth, must recalculate all widths
                        {
                            timeNotConsidered += TimeUntilNextEventInDays(assignmentActivites[i]);
                            dontCheck.Add(i);
                            rectList[i].Width = minEventWidth;
                            eventsWithMinWidth += 1;
                            allValid = false;
                        }
                    }
                    //setting color & other attributes
                    rectList[i].Height = timelineHeight;
                    rectList[i].HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    rectList[i].Fill = new SolidColorBrush(getEventFillColor(assignmentActivites[i].ActivityType));
                }
            } while (!allValid);
            return rectList;
        }

        /// <summary>
        /// Displays the timeline based off events on calender
        /// </summary>
        void displayTimeline()
        {
            clearTimeline();

            double TotalTime = TotalEventTime();
            bool endsWithStop = CalenderEndsWithStop();
            double minEventWidth = 50; //in pixels
            double timelineHeight = 20;
            double timelineWidth = thisView.LayoutRoot.MinWidth;

            List<Rectangle> rectList = getRectangles(minEventWidth, timelineHeight, timelineWidth);           
            if((int)(timelineWidth / minEventWidth) >= (assignmentActivites.Count)) //
                for (int i = 0; i < rectList.Count; i++) //add children into the view
                {
                    //adding rectangles
                    thisView.Timeline.Children.Add(rectList[i]);

                    //adding labels
                    Label timeLabel = new Label();
                    timeLabel.Width = rectList[i].Width;
                    timeLabel.Content = assignmentActivites[i].StartDateTime.Month.ToString() + "/" + assignmentActivites[i].StartDateTime.Day.ToString() + "\n11:59PM";
                    thisView.TimelineTimes.Children.Add(timeLabel);

                    //adding images
                    Image picToAdd = getEventImage(assignmentActivites[i].ActivityType);
                    int offSetToAllign = 32;
                    if (i >= 1)
                        picToAdd.Margin = new Thickness(rectList[i - 1].Width - 20, offSetToAllign, 0, 0);
                    else
                        picToAdd.Margin = new Thickness(0, offSetToAllign, 0, 0);
                    thisView.TimelineIcons.Children.Add(picToAdd);
                }
        }

        /// <summary>
        /// returns an image corrisponding to the activity given
        /// </summary>
        Image getEventImage(Activities activity)
        {
            Image returnPic = new Image();
            switch (activity)
            {
                case Activities.Submission:
                    returnPic.Source = new BitmapImage(new Uri("Icons/submit-01.png", UriKind.RelativeOrAbsolute));
                    break;
                case Activities.PeerReview:
                    returnPic.Source = new BitmapImage(new Uri("Icons/peer-01.png", UriKind.RelativeOrAbsolute));
                    break;
                case Activities.IssueVoting:
                    returnPic.Source = new BitmapImage(new Uri("Icons/voting-01.png", UriKind.RelativeOrAbsolute));
                    break;
                case Activities.AuthorRebuttal:
                    returnPic.Source = new BitmapImage(new Uri("Icons/rebuttal-01.png", UriKind.RelativeOrAbsolute));
                    break;
                case Activities.Stop:
                    returnPic.Source = new BitmapImage(new Uri("Icons/stop.png", UriKind.RelativeOrAbsolute));
                    break;
                default:
                    break;
            }
            return returnPic;
        }

        /// <summary>
        /// returns -1 if there is no next event
        /// returns the time (in days) until the next event
        /// </summary>
        double TimeUntilNextEventInDays(AssignmentActivityViewModel temp_aaVM)
        {
            double returnVal = 0.0;
            int index = 0;
            AssignmentActivityViewModel temp2 = new AssignmentActivityViewModel(Activities.Null);
            index = assignmentActivites.IndexOf(temp_aaVM);
            temp_aaVM = assignmentActivites[index];

            if (assignmentActivites.Count > (index+1)) //checking if there are enough activities to assign temp2
            {
                temp2 = assignmentActivites[index + 1];
            }
            else
            {
                temp2 = null;
            }

            if (temp_aaVM != null)
            {
                if (temp2 != null) //there is a following event
                {
                    returnVal = (temp2.StartDateTime.Ticks - temp_aaVM.StartDateTime.Ticks) / TimeSpan.TicksPerDay;
                }
                else                //no following event
                {
                    returnVal = 0.0;
                }
            }
            return returnVal;
        }
        
        /// <summary>
        ///returns the time (in days) from start of first event to end of last event
        ///returns the time from start of first event to start of last event if there is no end
        ///returns 0 if there is only 1 event or no events
        /// </summary>
        double TotalEventTime()
        {
            double returnVal = 0.0;
            AssignmentActivityViewModel temp = new AssignmentActivityViewModel(Activities.Null);

            temp = assignmentActivites.GetNextItem(temp);

            if (temp == null) return 0;//no events
            long time1 = temp.StartDateTime.Ticks;
            long time2 = temp.StartDateTime.Ticks;
            long dTimeInDays = temp.StartDateTime.Ticks; 

            while (temp != null)
            {
                time2 = temp.StartDateTime.Ticks; //gets time of old event
                temp = assignmentActivites.GetNextItem(temp);
                if (temp != null)
                {
                    time1 = temp.StartDateTime.Ticks; //gets time of new event
                    dTimeInDays = (time1 - time2) / TimeSpan.TicksPerDay;
                    returnVal += dTimeInDays;
                }
            }
            return returnVal;
        }

        /// <summary>
        /// returns true if the last event is a stop, else false
        /// </summary>
        bool CalenderEndsWithStop()
        {
            bool returnVal = false; 
            AssignmentActivityViewModel temp = new AssignmentActivityViewModel(Activities.Null);
            temp = assignmentActivites.GetNextItem(temp);
            while (temp != null)
            {
                if (temp.ActivityType == Activities.Stop) returnVal = true;
                else returnVal = false; 
                temp = assignmentActivites.GetNextItem(temp);
            }
            return returnVal;
        }


        void LeftCalanderScrollButton_Click(object sender, RoutedEventArgs e)
        {
            calanderLeft.MonthYear = calanderLeft.MonthYear.AddMonths(-1);
            calanderRight.MonthYear = calanderRight.MonthYear.AddMonths(-1);
            UpdateCalanders();
        }

        private void UpdateCalanders()
        {
            
            ClearCalanderMonth(calanderLeft);
            ClearCalanderMonth(calanderRight);
            ClearColorOfDays();

            AssignmentActivityViewModel aavm = GetPreviousAAVMFromDate(calanderLeft.MonthYear);

            if (calanderRight.MonthYear.Day != DateTime.DaysInMonth(calanderRight.MonthYear.Year, calanderRight.MonthYear.Month))
            {
                calanderRight.MonthYear.AddDays(DateTime.DaysInMonth(calanderRight.MonthYear.Year, calanderRight.MonthYear.Month));
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

            UpdateActivityDisplay();
        }

        private CalendarDayItemView findCalendarDayItemView(DateTime dateTime)
        {
            CalendarDayItemView day = calanderLeft.GetCalendarDayItemView(dateTime);
            if (day == null)
            {
                day = calanderRight.GetCalendarDayItemView(dateTime);
            }

            return day;
        }

        AssignmentActivityViewModel GetPreviousAAVMFromDate(DateTime dateTime)
        {
            int index = assignmentActivites.FindInsertionSpot(new AssignmentActivityViewModel(Activities.Null, dateTime));

            if(index <= 0)
            {
                return null;
            }
            else
            {
                return assignmentActivites[index - 1];
            }
        }

        void ClearCalanderMonth(MonthViewModel mvm)
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

        public AssignmentActivitiesCreaterView GetView()
        {
            return thisView;
        }


        public RoutedEventHandler test_Click { get; set; }
    }
}
