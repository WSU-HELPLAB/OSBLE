using System;
using System.Windows;
using System.Windows.Controls;

namespace ReviewInterfaceBase.View.Document.FindWindow
{
    public partial class Header : UserControl
    {
        public string HeaderLabel
        {
            get
            {
                return Label.Content as String;
            }
            set
            {
                Label.Content = HeaderLabel;
                UpdateContent();
            }
        }

        public Header()
        {
            InitializeComponent();
            this.SizeChanged += new SizeChangedEventHandler(HeaderView_SizeChanged);
        }

        private void HeaderView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateContent();
        }

        public void UpdateContent()
        {
            this.SizeChanged -= new SizeChangedEventHandler(HeaderView_SizeChanged);
            double totalWidth = this.ActualWidth;
            double totalHeight = this.ActualHeight;

            double LabelWidth = this.Label.ActualWidth + Label.Margin.Left + Label.Margin.Right;
            double LabelHeight = this.Label.ActualHeight;

            double ButtonsWidth = this.Buttons.ActualWidth + Buttons.Margin.Left + Buttons.Margin.Right;
            double ButtonsHeight = this.Buttons.ActualHeight;

            double rectWidth = totalWidth - (LabelWidth + ButtonsWidth);
            double rectHeight = totalHeight;

            if (rectHeight < 0)
            {
                rectHeight = 0;
            }
            if (rectWidth < 0)
            {
                rectWidth = 0;
            }

            Filler.Width = rectWidth;
            Filler.Height = rectHeight;
            this.SizeChanged += new SizeChangedEventHandler(HeaderView_SizeChanged);
        }
    }
}