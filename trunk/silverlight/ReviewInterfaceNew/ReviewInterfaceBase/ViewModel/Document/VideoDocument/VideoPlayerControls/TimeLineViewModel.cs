using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ReviewInterfaceBase.HelperClasses;
using ReviewInterfaceBase.View.Document.VideoPlayerControls;

namespace ReviewInterfaceBase.ViewModel.Document.VideoDocument.VideoPlayer
{
    public class TimelineViewModel
    {
        public event RoutedPropertyChangedEventHandler<TimeSpan> RequestProgressChanged = delegate { };

        private TimelineView thisView = new TimelineView();

        private Rectangle positionSelector = new Rectangle() { IsHitTestVisible = false, Fill = new SolidColorBrush(Colors.Green), Width = 1, Height = 50 };

        private ObservableCollection<TimelineMarker> timelineMarkers = new ObservableCollection<TimelineMarker>();

        public ObservableCollection<TimelineMarker> NotePositions
        {
            get { return timelineMarkers; }
            set { timelineMarkers = value; }
        }

        public Duration Maximum
        {
            get
            {
                return new Duration(new TimeSpan((long)thisView.progress.Maximum));
            }
            set
            {
                if (value.TimeSpan.Ticks > 0)
                {
                    thisView.TotalTimeTextBlock.Text = ConvertPercentageToStringTime(1);
                    thisView.progress.Maximum = value.TimeSpan.Ticks;
                }
            }
        }

        public TimeSpan Progress
        {
            get
            {
                return new TimeSpan((long)(thisView.progress.Value));
            }
            set
            {
                thisView.progress.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(progress_ValueChanged);
                thisView.progress.Value = value.Ticks;
                thisView.CurrentTimeTextBlock.Text = ConvertPercentageToStringTime(thisView.progress.Value / thisView.progress.Maximum);
                thisView.progress.ValueChanged += new RoutedPropertyChangedEventHandler<double>(progress_ValueChanged);
            }
        }

        public TimelineViewModel()
        {
            thisView.progress.Minimum = 0;
            thisView.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);
            thisView.progress.MouseEnter += new MouseEventHandler(progress_MouseEnter);
            thisView.progress.MouseLeave += new MouseEventHandler(progress_MouseLeave);
            thisView.progress.MouseMove += new MouseEventHandler(progress_MouseMove);
            thisView.progress.ValueChanged += new RoutedPropertyChangedEventHandler<double>(progress_ValueChanged);
            ToolTipService.SetToolTip(thisView.progress, new ToolTip());
            timelineMarkers.CollectionChanged += new NotifyCollectionChangedEventHandler(notePositions_CollectionChanged);
        }

        private void progress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RequestProgressChanged(this, new RoutedPropertyChangedEventArgs<TimeSpan>(new TimeSpan(), new TimeSpan((long)e.NewValue)));
        }

        private void thisView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateAll();
        }

        private void notePositions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                UpdateAdd(e.NewItems);
            }
            else
            {
                UpdateAll();
            }
        }

        public void UpdateAdd(IList itemsToBeAdded)
        {
            foreach (TimelineMarker ts in itemsToBeAdded)
            {
                addNewRect(ts.Time);
            }
        }

        public void Highlight(TimelineMarker timelineMarker)
        {
            Rectangle rect = thisView.CanvasOverlay.Children[timelineMarkers.IndexOf(timelineMarker)] as Rectangle;
            rect.Fill = new SolidColorBrush(Colors.Red);
        }

        public void RemovedHighlight(TimelineMarker timelineMarker)
        {
            Rectangle rect = thisView.CanvasOverlay.Children[timelineMarkers.IndexOf(timelineMarker)] as Rectangle;
            rect.Fill = new SolidColorBrush(Colors.Blue);
        }

        private void addNewRect(TimeSpan ts)
        {
            Rectangle note = new Rectangle() { IsHitTestVisible = false, Fill = new SolidColorBrush(Colors.Blue), Width = 1, Height = 50 };

            double percentage = ts.Ticks / thisView.progress.Maximum;

            double position = thisView.progress.ActualWidth * percentage;

            note.SetValue(Canvas.LeftProperty, position);
            note.SetValue(Canvas.TopProperty, 0.0);

            thisView.CanvasOverlay.Children.Add(note);
        }

        public void UpdateAll()
        {
            thisView.CanvasOverlay.Children.Clear();

            foreach (TimelineMarker tlMarker in timelineMarkers)
            {
                addNewRect(tlMarker.Time);
            }
        }

        private void progress_MouseMove(object sender, MouseEventArgs e)
        {
            double position = e.GetPosition(thisView.progress).X;
            double percentage = position / thisView.progress.ActualWidth;
            this.positionSelector.SetValue(Canvas.LeftProperty, position);
            ToolTip tp = (ToolTipService.GetToolTip(thisView.progress) as ToolTip);

            tp.Content = ConvertPercentageToStringTime(percentage);
            tp.IsOpen = false;
            ToolTipService.SetPlacement(thisView.progress, System.Windows.Controls.Primitives.PlacementMode.Mouse);
            tp.IsOpen = true;
        }

        private string ConvertPercentageToStringTime(double percentage)
        {
            TimeSpan timeSpan = new TimeSpan((long)Math.Round(Maximum.TimeSpan.Ticks * percentage, 2));
            //tp.Content = timeSpan.ToString();

            return HelperClass.ConvertTimeSpanToString(timeSpan);
        }

        private void progress_MouseLeave(object sender, MouseEventArgs e)
        {
            thisView.CanvasOverlay.Children.Remove(positionSelector);
            (ToolTipService.GetToolTip(thisView.progress) as ToolTip).IsOpen = false;
        }

        private void progress_MouseEnter(object sender, MouseEventArgs e)
        {
            //this.positionSelector.SetValue(Canvas.LeftProperty, e.GetPosition(thisView.progress).X);

            //thisView.CanvasOverlay.Children.Add(positionSelector);
        }

        public TimelineView GetView()
        {
            return thisView;
        }

        public void addTimelineMarker(TimelineMarker marker)
        {
            timelineMarkers.Add(marker);
        }
    }
}