using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Awesomium.Core;
using OSBIDE.Controls.ViewModels;

namespace OSBIDE.Controls.Views
{
    public class BrowserViewModelChangedEventArgs : EventArgs
    {
        public BrowserViewModel OldModel { get; private set; }
        public BrowserViewModel NewModel { get; private set; }
        public BrowserViewModelChangedEventArgs(BrowserViewModel old, BrowserViewModel newModel)
            : base()
        {
            OldModel = old;
            NewModel = newModel;
        }
    }

    /// <summary>
    /// Interaction logic for BrowserView.xaml
    /// </summary>
    public partial class BrowserView : UserControl
    {
        public event EventHandler<BrowserViewModelChangedEventArgs> BrowserViewModelChanged = delegate { };

        public BrowserView()
        {
            InitializeComponent();
        }

        private void UpdateUrl()
        {
            if (ViewModel.Url.Length > 0)
            {
                BrowserWindow.Source = new Uri(string.Format("{0}?auth={1}", ViewModel.Url, ViewModel.AuthKey));
            }
        }

        public BrowserViewModel ViewModel
        {
            get
            {
                return this.DataContext as BrowserViewModel;
            }
            set
            {
                if (ViewModel != null)
                {
                    ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
                BrowserViewModel oldModel = this.DataContext as BrowserViewModel;
                this.DataContext = value;
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                Dispatcher.Invoke(new Action(UpdateUrl));
                BrowserViewModelChanged(this, new BrowserViewModelChangedEventArgs(oldModel, value));
            }
        }

        void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Url")
            {
                Dispatcher.Invoke(new Action(UpdateUrl));
            }
        }

        private void BrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (WebCore.ResourceInterceptor == null)
            {
                WebCore.ResourceInterceptor = OsbideResourceInterceptor.Instance;
            }
        }

        private void BrowserWindow_LoadingFrame(object sender, LoadingFrameEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = BrowserWindow.Opacity;
            animation.To = 0;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            //BrowserWindow.BeginAnimation(OpacityProperty, animation);
        }

        private void BrowserWindow_LoadingFrameComplete(object sender, FrameEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = BrowserWindow.Opacity;
            animation.To = 1;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            //BrowserWindow.BeginAnimation(OpacityProperty, animation);
        }
    }
}
