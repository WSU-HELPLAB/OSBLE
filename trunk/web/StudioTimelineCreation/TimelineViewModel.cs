using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
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

        public void UpdateTimeline(SortedList<AssignmentActivityViewModel> aavm)
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
                    timeLabel.Content = assignmentActivites[i].StartDateTime.Month.ToString() + "/" + assignmentActivites[i].StartDateTime.Day.ToString() + "\n" + assignmentActivites[i].StartDateTime.ToString("h:mm tt");
                    thisView.TimelineTimes.Children.Add(timeLabel);

                    //adding images
                    Image picToAdd = new Image() { Source = AssignmentActivitiesFactory.GetImageSourceFromActivities(assignmentActivites[i].ActivityType) };
                    int offSetToAllign = 32;
                    if (i >= 1)
                    {
                        picToAdd.Margin = new Thickness(rectList[i - 1].Width - 20, offSetToAllign, 0, 0);
                    }
                    else
                    {
                        picToAdd.Margin = new Thickness(0, offSetToAllign, 0, 0);
                    }
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
                    rectList[i].Height = 10;
                    rectList[i].HorizontalAlignment = HorizontalAlignment.Left;
                    rectList[i].VerticalAlignment = VerticalAlignment.Center;
                    rectList[i].Fill = AssignmentActivitiesFactory.GetColor(assignmentActivites[i].ActivityType);
                }
            } while (!allValid);
            return rectList;
        }

        /// <summary>
        /// returns -1 if there is no next event
        /// returns the time (in days) until the next event
        /// </summary>
        private double TimeUntilNextEventInDays(AssignmentActivityViewModel activitiy)
        {
            AssignmentActivityViewModel nextActivitiy = null;

            nextActivitiy = assignmentActivites.GetNextItem(activitiy);

            if (activitiy != null && nextActivitiy != null)
            {
                return (nextActivitiy.StartDateTime.Ticks - activitiy.StartDateTime.Ticks) / TimeSpan.TicksPerDay;
            }
            else                // activity or no following activity
            {
                return 0.0;
            }
        }

        /// <summary>
        ///returns the time (in days) from start of first event to end of last event
        ///returns the time from start of first event to start of last event if there is no end
        ///returns 0 if there is only 1 event or no events
        /// </summary>
        private double TotalEventTime()
        {
            //we need at least 2 events before we can find the time between them
            if (assignmentActivites.Count >= 2)
            {
                AssignmentActivityViewModel first = assignmentActivites[0];
                AssignmentActivityViewModel last = assignmentActivites[assignmentActivites.Count - 1];
                return (last.StartDateTime.Ticks - first.StartDateTime.Ticks) / TimeSpan.TicksPerDay;
            }
            else
            {
                return 0.0;
            }
        }
    }
}