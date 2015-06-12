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


using CreateNewAssignment.AssignmentActivities;

namespace CreateNewAssignment.AssignmentActivities
{
    public class AssignmentActivityViewModel : IComparable<AssignmentActivityViewModel>
    {
        public event EventHandler RemoveRequested = delegate { };
        public event MouseButtonEventHandler MouseLeftButtonDown = delegate { };
        public event MouseButtonEventHandler MouseLeftButtonUp = delegate { };
        public event MouseButtonEventHandler MouseRightButtonDown = delegate { };
        public event MouseEventHandler MouseMove = delegate { };

        private DateTime startDateTime;
        private Activities activityType;
        private AssignmentActivityView thisView = new AssignmentActivityView();
        private CalendarDayItemView calendarDay;


        public CalendarDayItemView CalendarDay
        {
            get 
            {
                return calendarDay;
            }
            set
            {
                if (value != null)
                {
                    value.AssignmentActivityIconHolder.Content = thisView;
                }
                else
                {
                    if (calendarDay != null)
                    {
                        calendarDay.AssignmentActivityIconHolder.Content = null;
                    }
                }
                calendarDay = value;
            }
        }

        public Activities ActivityType
        {
            get { return activityType; }
            set { activityType = value; }
        }

        public Image ActivityImage
        {
            get
            {
                return thisView.Image;
            }
        }

        public DateTime StartDateTime
        {
            get { return startDateTime; }
            set { startDateTime = value; }
        }

        public Brush ActivityColor
        {
            get
            {
                return AssignmentActivitiesFactory.GetColor(activityType);
            }
        }

        public AssignmentActivityView GetView()
        {
            return thisView;
        }

        public AssignmentActivityViewModel(Activities activityType)
        {
            this.activityType = activityType;
            this.startDateTime = default(DateTime);
            LocalInit();
        }

        void LocalInit()
        {
            Cursor = Cursors.Hand;
            thisView.Image.Source = AssignmentActivitiesFactory.GetImageSourceFromActivities(activityType);
            thisView.MouseLeftButtonDown += new MouseButtonEventHandler(thisView_MouseLeftButtonDown);
            thisView.MouseRightButtonDown += new MouseButtonEventHandler(thisView_MouseRightButtonDown);
            thisView.MouseLeftButtonUp += new MouseButtonEventHandler(thisView_MouseLeftButtonUp);
            thisView.MouseMove += new MouseEventHandler(thisView_MouseMove);
            thisView.KeyDown += new KeyEventHandler(thisView_KeyDown);
            thisView.IsTabStop = true;
            thisView.Focus();
        }

        void thisView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                RemoveRequested(this, EventArgs.Empty);
            }
        }

        public AssignmentActivityViewModel(Activities activityType, DateTime startDateTime)
        {
            this.activityType = activityType;
            this.startDateTime = startDateTime;
            LocalInit();
        }

        public void Remove()
        {
            this.CalendarDay.AssignmentActivityIconHolder.Content = null;
            startDateTime = default(DateTime);
            thisView.MouseLeftButtonDown -= new MouseButtonEventHandler(thisView_MouseLeftButtonDown);
            thisView.MouseRightButtonDown -= new MouseButtonEventHandler(thisView_MouseRightButtonDown);
            thisView.MouseLeftButtonUp -= new MouseButtonEventHandler(thisView_MouseLeftButtonUp);
            thisView.MouseMove -= new MouseEventHandler(thisView_MouseMove);
        }

        public void CaptureMouse()
        {
            thisView.CaptureMouse();
        }

        public Cursor Cursor
        {
        get
        {
            return thisView.Cursor;
        }
            set
            {
                thisView.Cursor = value;
            }
        }

        public void SetValue(DependencyProperty dp, object obj)
        {
            thisView.SetValue(dp, obj);
        }

        public double ActualWidth
        {
            get
            {
                return thisView.ActualWidth;
            }
        }

        public double ActualHeight
        {
            get
            {
                return thisView.ActualHeight;
            }
        }

        void thisView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            thisView.Focus();
            MouseLeftButtonUp(this, e);
        }

        void thisView_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMove(this, e);
        }

        void thisView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            thisView.Focus();
            MouseRightButtonDown(this, e);
        }

        public void DeleteMenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            RemoveRequested(this, EventArgs.Empty);
        }

        void thisView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseLeftButtonDown(this, e);
        }

        #region IComparable<AssignmentActivityViewModel> Members

        public int CompareTo(AssignmentActivityViewModel other)
        {
            if (startDateTime < other.StartDateTime)
            {
                return -1;
            }
            else if (startDateTime > other.StartDateTime)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion
    }
}
