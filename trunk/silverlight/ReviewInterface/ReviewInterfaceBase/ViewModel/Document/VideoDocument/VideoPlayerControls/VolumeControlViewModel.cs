using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ReviewInterfaceBase.View.Document.VideoPlayerControls;

namespace ReviewInterfaceBase.ViewModel.Document.VideoDocument.VideoPlayer
{
    public class VolumeControlViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool ismuted = false;

        public bool IsMuted
        {
            get { return ismuted; }
            set
            {
                ismuted = value;
                if (ismuted)
                {
                    thisView.Mute.Content = new Image() { Source = new BitmapImage(new Uri("/Osble;component/View/Icons/mutedSpeaker.png", UriKind.Relative)) };
                }
                else
                {
                    thisView.Mute.Content = new Image() { Source = new BitmapImage(new Uri("/Osble;component/View/Icons/speaker.png", UriKind.Relative)) };
                }
                PropertyChanged(this, new PropertyChangedEventArgs("IsMuted"));
            }
        }

        private double volume = .5;

        public double Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                IsMuted = false;
                PropertyChanged(this, new PropertyChangedEventArgs("Volume"));
            }
        }

        private VolumeControlView thisView = new VolumeControlView();

        public VolumeControlViewModel()
        {
            thisView.Mute.Click += new RoutedEventHandler(Mute_Click);

            thisView.VolumeControlSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(VolumeControlSlider_ValueChanged);
        }

        private void VolumeControlSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Volume = e.NewValue;
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            IsMuted = !IsMuted;
            //Mute(this, e);
        }

        public VolumeControlView GetView()
        {
            return thisView;
        }
    }
}