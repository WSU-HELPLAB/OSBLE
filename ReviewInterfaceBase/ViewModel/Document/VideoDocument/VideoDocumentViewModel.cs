using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.View.Document.VideoPlayer;
using ReviewInterfaceBase.ViewModel.Comment.Location;
using ReviewInterfaceBase.ViewModel.FindWindow;

namespace ReviewInterfaceBase.ViewModel.Document.VideoDocument.VideoPlayer
{
    public class VideoPlayerViewModel : IDocumentViewModel
    {
        public event RoutedPropertyChangedEventHandler<TimeSpan> AddNewComment = delegate { };

        private int documentID;

        VideoDocumentView thisView = new VideoDocumentView();

        TimelineViewModel timelineViewModel = new TimelineViewModel();

        DispatcherTimer timer = new DispatcherTimer();

        bool isPlayButton = true;

        private bool allowsComments = false;

        private bool isDisplayed = false;

        public bool IsDisplayed
        {
            get { return isDisplayed; }
            set
            {
                if (value != isDisplayed)
                {
                    if (value)
                    {
                        setPlayButtonImage(true);
                        thisView.Video.Play();
                        timer.Start();
                    }
                    else
                    {
                        setPlayButtonImage(false);
                        thisView.Video.Pause();
                        timer.Stop();
                    }
                }
                isDisplayed = value;
            }
        }

        public int DocumentID
        {
            get { return documentID; }
        }

        public void AllowNewComments()
        {
            //if we already allow comments we don't need to add another button
            if (allowsComments == false)
            {
                Button addNewCommentButton = new Button()
                {
                    Content = new Image() { Source = new BitmapImage(new Uri("/Osble;component/View/Icons/NoteHS.png", UriKind.Relative)) },
                    Margin = new Thickness(0, 0, 5, 0)
                };

                ToolTipService.SetToolTip(addNewCommentButton, "Add Note");

                addNewCommentButton.Click += new RoutedEventHandler(AddAnnotation_Click);

                thisView.ControlToolBar.Children.Insert(thisView.ControlToolBar.Children.Count - 1, addNewCommentButton);

                allowsComments = true;
            }
        }

        public VideoPlayerViewModel(int documentID)
        {
            this.documentID = documentID;

            VolumeControlViewModel volumeControlVM = new VolumeControlViewModel();

            //This will not always fire when it is supposed to bug in SL
            thisView.Video.MediaOpened += new RoutedEventHandler(Video_MediaOpened);

            thisView.Video.AutoPlay = false;

            Binding muteBinding = new Binding("IsMuted") { Source = volumeControlVM, Mode = BindingMode.TwoWay };

            thisView.Video.SetBinding(MediaElement.IsMutedProperty, muteBinding);

            Binding volumeBinding = new Binding("Volume") { Source = volumeControlVM, Mode = BindingMode.TwoWay };

            thisView.Video.SetBinding(MediaElement.VolumeProperty, volumeBinding);

            thisView.ControlToolBar.Children.Add(volumeControlVM.GetView());

            timelineViewModel.GetView().SetValue(Grid.RowProperty, 1);

            timelineViewModel.RequestProgressChanged += new RoutedPropertyChangedEventHandler<TimeSpan>(timelineViewModel_RequestProgressChanged);

            thisView.LayoutRoot.Children.Add(timelineViewModel.GetView());

            thisView.Video.MediaEnded += new RoutedEventHandler(Video_MediaEnded);
            thisView.PlayButton.Click += new RoutedEventHandler(PlayButton_Click);
            thisView.StepBackButton.Click += new RoutedEventHandler(StepBackButton_Click);
            thisView.StepForwardButton.Click += new RoutedEventHandler(StepForwardButton_Click);
            thisView.FullScreenButton.Click += new RoutedEventHandler(FullScreenButton_Click);

            thisView.JumpForwardButton.Click += new RoutedEventHandler(JumpForwardButton_Click);
            thisView.JumpBackButton.Click += new RoutedEventHandler(JumpBackButton_Click);

            //that is 1 millisecond
            timer.Interval = new TimeSpan(1000);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        private void Video_MediaEnded(object sender, RoutedEventArgs e)
        {
            thisView.Video.Position = new TimeSpan(0);
            setPlayButtonImage(true);
        }

        public object FindNext(object lastFound, FindWindowOptions options)
        {
            return null;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("ID", documentID.ToString());
        }

        public VideoLocation GetReferenceLocationFromXml(XElement xmlLocation)
        {
            TimeSpan timeIndex = TimeSpan.Parse(xmlLocation.Attribute("TimeIndex").Value);

            VideoLocation location = new VideoLocation(timeIndex);

            return location;
        }

        public void RemoveCommentReference(TimeSpan marker)
        {
            int i = 0;
            while (i < timelineViewModel.NotePositions.Count)
            {
                TimelineMarker ts = timelineViewModel.NotePositions[i];
                if (ts.Time == marker)
                {
                    timelineViewModel.NotePositions.Remove(ts);
                    thisView.Video.Markers.Remove(ts);
                    //we removed an element so got to decrement i
                    i--;
                }
                i++;
            }
        }

        public void CreateCommentReference(TimeSpan timeSpan)
        {
            TimelineMarker marker = new TimelineMarker() { Time = timeSpan };
            timelineViewModel.addTimelineMarker(marker);
            thisView.Video.Markers.Add(marker);

            //we need to wait till the video has been opened so it can set the TimeLineMarkers where they need to be
            thisView.Video.MediaOpened += new RoutedEventHandler(UpdateComments);
        }

        private void UpdateComments(object sender, RoutedEventArgs e)
        {
            thisView.Video.MediaOpened -= new RoutedEventHandler(UpdateComments);
            timelineViewModel.UpdateAll();
        }

        public void HighlightCommentReference(TimeSpan marker)
        {
            foreach (TimelineMarker tlMarker in timelineViewModel.NotePositions)
            {
                if (tlMarker.Time == marker)
                {
                    timelineViewModel.Highlight(tlMarker);
                }
            }
        }

        public void RemoveHighlightingCommentReference(TimeSpan marker)
        {
            foreach (TimelineMarker tlMarker in timelineViewModel.NotePositions)
            {
                if (tlMarker.Time == marker)
                {
                    timelineViewModel.RemovedHighlight(tlMarker);
                }
            }
        }

        public void SetSource(Stream stream)
        {
            thisView.Video.SetSource(stream);
        }

        private void AddAnnotation_Click(object sender, RoutedEventArgs e)
        {
            bool unqiueTime = true;
            TimelineMarker marker = new TimelineMarker() { Time = thisView.Video.Position };
            foreach (TimelineMarker previousMarker in thisView.Video.Markers)
            {
                if (previousMarker.Time == marker.Time)
                {
                    unqiueTime = false;
                    break;
                }
            }
            if (unqiueTime)
            {
                timelineViewModel.addTimelineMarker(marker);
                thisView.Video.Markers.Add(marker);
                RoutedPropertyChangedEventArgs<TimeSpan> timeLineArgs = new RoutedPropertyChangedEventArgs<TimeSpan>(new TimeSpan(), marker.Time);
                AddNewComment(this, timeLineArgs);
            }
            else
            {
                MessageBox.Show("There already exists a comment at this time index, another one cannot be created");
            }
        }

        private void JumpBackButton_Click(object sender, RoutedEventArgs e)
        {
            //The minus 1 millisecond is incase we are sitting right at a comment we don't want to find that one again so increment by 1 millisecond
            //the most accurate this is appears to be milliseconds so we add 1 millisecond to the time.
            TimeSpan currentTime = thisView.Video.Position - new TimeSpan(0, 0, 0, 0, 1);
            TimelineMarker closestBackwards = null;
            foreach (TimelineMarker maker in thisView.Video.Markers)
            {
                if (currentTime > maker.Time)
                {
                    if (closestBackwards == null)
                    {
                        closestBackwards = maker;
                    }
                    else if (closestBackwards.Time < maker.Time)
                    {
                        closestBackwards = maker;
                    }
                }
            }
            if (closestBackwards != null)
            {
                thisView.Video.Position = closestBackwards.Time;
            }
            else
            {
                //set it to the start
                thisView.Video.Position = new TimeSpan(0);
            }
        }

        private void JumpForwardButton_Click(object sender, RoutedEventArgs e)
        {
            //The plus 1 millisecond is incase we are sitting right at a comment we don't want to find that one again so increment by 1 millisecond
            //the most accurate this is appears to be milliseconds so we add 1 millisecond to the time.
            TimeSpan currentTime = thisView.Video.Position + new TimeSpan(0, 0, 0, 0, 1);
            TimelineMarker closestForwards = null;
            foreach (TimelineMarker maker in thisView.Video.Markers)
            {
                if (currentTime < maker.Time)
                {
                    if (closestForwards == null)
                    {
                        closestForwards = maker;
                    }
                    else if (closestForwards.Time > maker.Time)
                    {
                        closestForwards = maker;
                    }
                }
            }
            if (closestForwards != null)
            {
                thisView.Video.Position = closestForwards.Time;
            }
            else
            {
                //set it to the end
                thisView.Video.Position = thisView.Video.NaturalDuration.TimeSpan;
            }
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Host.Content.IsFullScreen = !Application.Current.Host.Content.IsFullScreen;
        }

        private void timelineViewModel_RequestProgressChanged(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            thisView.Video.Position = e.NewValue;
        }

        /// <summary>
        /// This will not always fire when it is supposed to bug in SL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Video_MediaOpened(object sender, RoutedEventArgs e)
        {
            timelineViewModel.Maximum = thisView.Video.NaturalDuration;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            //this is on a timer and not a binding because a binding to Position breaks SL
            //assuming because it gets updated a lot and SL cant handle it (does not throw an error though)
            timelineViewModel.Progress = thisView.Video.Position;
        }

        private void StepForwardButton_Click(object sender, RoutedEventArgs e)
        {
            thisView.Video.Position = thisView.Video.Position + new TimeSpan(0, 0, 5);
        }

        private void StepBackButton_Click(object sender, RoutedEventArgs e)
        {
            thisView.Video.Position = thisView.Video.Position - new TimeSpan(0, 0, 5);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlayButton)
            {
                //this is because the MediaOpened Event is not guaranteed to fired
                timelineViewModel.Maximum = thisView.Video.NaturalDuration;
                setPlayButtonImage(false);
                thisView.Video.Play();
            }
            else
            {
                thisView.Video.Pause();
                setPlayButtonImage(true);
            }
        }

        private void setPlayButtonImage(bool setToPlay)
        {
            if (setToPlay)
            {
                thisView.PlayButton.Content = new Image() { Source = new BitmapImage(new Uri("/Osble;component/View/Icons/Play.png", UriKind.Relative)) };
                isPlayButton = true;
            }
            else
            {
                thisView.PlayButton.Content = new Image() { Source = new BitmapImage(new Uri("/Osble;component/View/Icons/Pause.png", UriKind.Relative)) };
                isPlayButton = false;
            }
        }

        public VideoDocumentView GetView()
        {
            return thisView;
        }
    }
}