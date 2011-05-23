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
using System.Collections.Generic;
using CreateNewAssignment.AssignmentActivities;

namespace CreateNewAssignment
{
    public class TimelineViewModel
    {
        TimelineView thisView = new TimelineView();
        SortedList<AssignmentActivityViewModel> assignmentActivites;
        private double minEventWidthInPixels;
        private double timelineHeightInPixels;
        private double timelineWidthInPixels;

        public double MinEventWidthInPixels
        {
            get { return minEventWidthInPixels; }
            set { minEventWidthInPixels = value; }
        }
        public double TimelineHeightInPixels
        {
            get { return timelineHeightInPixels; }
            set { timelineHeightInPixels = value; }
        }
        public double TimelineWidthInPixels
        {
            get { return timelineWidthInPixels; }
            set { timelineWidthInPixels = value; }
        }

        public TimelineViewModel()
        {
            MinEventWidthInPixels = 50;
            TimelineHeightInPixels = 20;
            TimelineWidthInPixels = thisView.ActualWidth;
        }
        public TimelineView GetView()
        {
            return thisView;
        }
        public void displayTimeline(SortedList<AssignmentActivityViewModel> aavm)
        {
            assignmentActivites = aavm;
            clearTimeline();
            TimelineWidthInPixels = thisView.ActualWidth;

            
            if ((int)(TimelineWidthInPixels / MinEventWidthInPixels) >= (assignmentActivites.Count))
            {
                List<Rectangle> rectList = getRectangles();
                double TotalTime = TotalEventTime();
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
                    Image picToAdd = new Image() { Source = AssignmentActivitiesFactory.GetImageSourceFromActivities(assignmentActivites[i].ActivityType) };
                    int offSetToAllign = 32;
                    if (i >= 1)
                        picToAdd.Margin = new Thickness(rectList[i - 1].Width - 20, offSetToAllign, 0, 0);
                    else
                        picToAdd.Margin = new Thickness(0, offSetToAllign, 0, 0);
                    thisView.TimelineIcons.Children.Add(picToAdd);
                }
            }
        }
        /// <summary>
        /// removes the children from all the stack panels associated with timeline (labels for date, time, icons for event, and rects for event)
        /// </summary>
        private void clearTimeline()
        {
            for (int i = thisView.Timeline.Children.Count - 1; i >= 0; i--)
                thisView.Timeline.Children.RemoveAt(i);
            for (int i = thisView.TimelineDates1.Children.Count - 1; i >= 0; i--)
                thisView.TimelineDates1.Children.RemoveAt(i);
            for (int i = thisView.TimelineTimes.Children.Count - 1; i >= 0; i--)
                thisView.TimelineTimes.Children.RemoveAt(i);
            for (int i = thisView.TimelineIcons.Children.Count - 1; i >= 0; i--)
                thisView.TimelineIcons.Children.RemoveAt(i);
        }

        /// <summary>
        /// returns a list of rectangles that are no smaller than minRectWidth, stretch timelineWidth, and each rectangles width corrisponds to how much time it takes
        /// </summary>
        //private List<Rectangle> getRectangles(double minRectWidth, double rectHeight, double timelineWidth)
        private List<Rectangle> getRectangles()
        {
            double TotalTime = TotalEventTime();
            int eventsWithMinWidth = 1; //starts at 1 because final event always has min width
            double timeNotConsidered = 0;

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
                        rectList[i].Width = TimelineWidthInPixels;
                    }
                    else if (i == (assignmentActivites.Count - 1)) //last activity, make it expand exactly the min width
                    {
                        rectList[i].Width = MinEventWidthInPixels;
                    }
                    else //normal activity, width it set on of timetaken / totaltime, with a minimum width of MinEventWidthInPixels
                    {
                        if (!dontCheck.Contains(i)) //if it was determined that its width was less than MinEventWidthInPixels previously, dont change it.
                            // rectList[i].Width = ((TimeUntilNextEventInDays(assignmentActivites[i])) / (TotalTime - timeNotConsidered)) * (thisView.LayoutRoot.MinWidth - MinEventWidthInPixels * eventsWithMinWidth);
                            rectList[i].Width = ((TimeUntilNextEventInDays(assignmentActivites[i])) / (TotalTime - timeNotConsidered)) * (TimelineWidthInPixels - MinEventWidthInPixels * eventsWithMinWidth);

                        if (rectList[i].Width < MinEventWidthInPixels)//if any width is less than MinEventWidthInPixels, must recalculate all widths
                        {
                            timeNotConsidered += TimeUntilNextEventInDays(assignmentActivites[i]);
                            dontCheck.Add(i);
                            rectList[i].Width = MinEventWidthInPixels;
                            eventsWithMinWidth += 1;
                            allValid = false;
                        }
                    }
                    //setting color & other attributes
                    rectList[i].Height = TimelineHeightInPixels;
                    rectList[i].HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    rectList[i].Fill = AssignmentActivitiesFactory.GetColor(assignmentActivites[i].ActivityType);
                }
            } while (!allValid);
            return rectList;
        }

        /// <summary>
        /// returns -1 if there is no next event
        /// returns the time (in days) until the next event
        /// </summary>
        private double TimeUntilNextEventInDays(AssignmentActivityViewModel temp_aaVM)
        {
            double returnVal = 0.0;
            int index = 0;
            AssignmentActivityViewModel temp2 = new AssignmentActivityViewModel(Activities.Null);
            index = assignmentActivites.IndexOf(temp_aaVM);
            temp_aaVM = assignmentActivites[index];

            if (assignmentActivites.Count > (index + 1)) //checking if there are enough activities to assign temp2
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
        private double TotalEventTime()
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
    }
}
