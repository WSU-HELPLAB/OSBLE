using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using ReviewInterfaceBase.Model.Comment;
using ReviewInterfaceBase.View.Comment;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Comment
{
    public abstract class AbstractCommentViewModel : INotifyPropertyChanged, ICommentViewModel
    {
        #region Delegates

        public event TextChangedEventHandler NoteTextChanged = delegate { };
        public event EventHandler Remove = delegate { };
        public event MouseButtonEventHandler Moving = delegate { };
        public event MouseButtonEventHandler Resizing = delegate { };
        public event MouseButtonEventHandler Minimize = delegate { };
        public event PropertyChangedEventHandler PropertyChanged;
        public event RoutedEventHandler LostFocus = delegate { };
        public event RoutedEventHandler GotFocus = delegate { };
        public event EventHandler Maximize = delegate { };
        public event EventHandler SizeChanged = delegate { };

        #endregion Delegates

        #region Fields

        //this is the Line that connects the text to the noteText
        private List<Line> snippetToCommentLine = new List<Line>();

        //this stores the rectangles that make up the snippetHighlighting
        private List<FrameworkElement> snippetHighlighting = null;

        //these are default colors yellow
        protected Brush noteBrush = new SolidColorBrush(Color.FromArgb(255, 255, 220, 0));
        protected Brush headerBrush = new SolidColorBrush(Color.FromArgb(255, 235, 200, 0));
        protected Brush borderBrush = new SolidColorBrush(Color.FromArgb(255, 225, 190, 0));
        protected Brush lineBrush = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        protected Brush textBrush = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));

        //this sets the default height and width
        private double width = 200;
        private double height = 150;

        //this is a reference to the View
        protected CommentView thisView;

        protected CommentModel thisModel;

        private CollapsedCommentView thisViewCollapsed;

        private bool usingView = true;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// This creates a NoteText that is a PeerReviewCommentViewModel, PeerReviewCommentView, and CommentModel
        /// PeerReviewCommentView and CommentModel can be accessed by GetView or GetModel
        /// </summary>
        /// <param name="ReferenceLocation">A reference to the location that this noteText is commenting on</param>
        protected void Initilize(AbstractCommentView view, ILocation referenceLocation)
        {
            this.thisView = view;
            this.thisModel = new CommentModel(referenceLocation);

            thisViewCollapsed = new CollapsedCommentView(this);
        }

        #endregion Constructor

        #region Properties

        public ICommand StartHeaderMoveCommand
        {
            get;
            set;
        }

        public ICommand CloseCommentCommand
        {
            get;
            set;
        }

        public bool UsingView
        {
            get
            {
                return usingView;
            }
            set
            {
                usingView = value;
            }
        }

        public string Header
        {
            get
            {
                return thisModel.Header;
            }
            set
            {
                thisModel.Header = value;
                NotifyPropertyChanged("Header");
            }
        }

        public string NoteText
        {
            get
            {
                return thisModel.NoteText;
            }
            set
            {
                thisModel.NoteText = value;
                NotifyPropertyChanged("NoteText");
            }
        }

        public Brush LineBrush
        {
            get { return lineBrush; }
            set { lineBrush = value; }
        }

        public Brush TextBrush
        {
            get { return textBrush; }
            set { textBrush = value; }
        }

        /// <summary>
        /// This gets or sets the line that connects the associated text to the commentView
        /// </summary>
        public List<Line> SnippetToCommentLine
        {
            get { return snippetToCommentLine; }
            set { snippetToCommentLine = value; }
        }

        /// <summary>
        /// This gets or sets the list of Rectangles that make up the highlighting of the associated text
        /// </summary>
        public List<FrameworkElement> SnippetHighlighting
        {
            get { return snippetHighlighting; }
            set
            {
                if (value != null)
                {
                    foreach (FrameworkElement uiElement in value)
                    {
                        uiElement.MouseLeftButtonDown += new MouseButtonEventHandler(GiveNoteFocus);
                        ToolTipService.SetToolTip(uiElement, new ToolTip() { Content = GetToolTipView() });
                    }
                }
                snippetHighlighting = value;
            }
        }

        /// <summary>
        /// This sets the color of the Note
        /// </summary>
        public Brush NoteBrush
        {
            get { return noteBrush; }
            set { noteBrush = value; }
        }

        /// <summary>
        /// This sets the color of the header
        /// </summary>
        public Brush HeaderBrush
        {
            get { return headerBrush; }
            set { headerBrush = value; }
        }

        /// <summary>
        /// This sets the color of the border
        /// </summary>
        public Brush BorderBrush
        {
            get { return borderBrush; }
            set { borderBrush = value; }
        }

        /// <summary>
        /// This gets or sets the width, NOTE: does not included the border around the NoteText
        /// </summary>
        public double Width
        {
            get { return width; }
            set
            {
                width = value;
                NotifyPropertyChanged("Width");
            }
        }

        /// <summary>
        /// This gets or sets the height, NOTE: does not included the border around the NoteText
        /// </summary>
        public double Height
        {
            get { return height; }
            set
            {
                height = value;
                NotifyPropertyChanged("Height");
            }
        }

        /// <summary>
        /// This gets or sets the associated text
        /// </summary>
        public ILocation referenceLocation
        {
            get
            {
                return thisModel.Location;
            }
        }

        /// <summary>
        /// This gets or sets the TopLeft corner of the noteText
        /// </summary>
        public Point Location
        {
            get
            {
                return new Point(
                    (double)thisView.GetValue(Canvas.LeftProperty),
                    (double)thisView.GetValue(Canvas.TopProperty)
                    );
            }
            set
            {
                thisView.SetValue(Canvas.LeftProperty, value.X);
                thisView.SetValue(Canvas.TopProperty, value.Y);
            }
        }

        /// <summary>
        /// This gets or sets the size of the noteText, NOTE: this does not included the border
        /// </summary>
        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        #endregion Properties

        #region Public Methods

        public abstract bool Focus();

        public abstract void XmlWrite(XmlWriter writer);

        /// <summary>
        /// This returns the associated PeerReviewCommentView
        /// </summary>
        /// <returns>PeerReviewCommentView</returns>
        public AbstractCommentView GetView()
        {
            return thisView;
        }

        public Label GetToolTipView()
        {
            Label label = new Label();
            Binding text = new Binding("NoteText");
            Binding brush = new Binding("NoteBrush");

            label.DataContext = this;

            label.SetBinding(Label.ContentProperty, text);

            label.SetBinding(Label.BackgroundProperty, brush);

            return label;
        }

        public CollapsedCommentView GetCollapsedView()
        {
            return thisViewCollapsed;
        }

        #endregion Public Methods

        #region Private Methods

        private bool CanExecute(object param)
        {
            return true;
        }

        // NotifyPropertyChanged will raise the PropertyChanged event,
        // passing the source property that is being updated.
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion Private Methods

        #region Events From View

        protected void thisView_LostFocus(object sender, RoutedEventArgs e)
        {
            LostFocus(this, e);
        }

        protected void thisView_GotFocus(object sender, RoutedEventArgs e)
        {
            GotFocus(this, e);
        }

        protected abstract void GiveNoteFocus(object sender, MouseButtonEventArgs e);

        protected void Note_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged("NoteText");
            NoteTextChanged(this, e);
        }

        protected void Maximize_Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Maximize(this, EventArgs.Empty);
            e.Handled = true;
        }

        /// <summary>
        /// This is fires Minimize
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected void Minimize_Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Minimize(this, e);
            e.Handled = true;
        }

        /// <summary>
        /// This is fires Moving
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Moving(this, e);
            e.Handled = true;
        }

        /// <summary>
        /// This is fires Remove
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected void X_Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Remove(this, EventArgs.Empty);
            e.Handled = true;
        }

        /// <summary>
        /// This is fires Resizing
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected void Bottom_Left_Corner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Resizing(this, e);
            e.Handled = true;
        }

        /// <summary>
        /// This is fires the event to let anyone know who is listening that our SizeChanged
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        protected void thisView_SizeChanged()
        {
            SizeChanged(this, EventArgs.Empty);
        }

        #endregion Events From View
    }
}