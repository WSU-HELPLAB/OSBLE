using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReviewInterfaceBase.View.Comment;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Comment
{
    public class CommentViewModel : AbstractCommentViewModel
    {
        private CommentView thisView = new CommentView();

        public CommentViewModel(ILocation referenceLocation)
        {
            base.Initilize(thisView, referenceLocation);
            thisView.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);
            thisView.DataContext = this;

            thisView.Title.MouseLeftButtonDown += new MouseButtonEventHandler(Header_MouseLeftButtonDown);
            thisView.X_Label.MouseLeftButtonDown += new MouseButtonEventHandler(X_Label_MouseLeftButtonDown);
            //thisView.Bottem_Right_Corner.MouseLeftButtonDown += new MouseButtonEventHandler(Bottom_Left_Corner_MouseLeftButtonDown);
            thisView.Minimize_Label.MouseLeftButtonDown += new MouseButtonEventHandler(Minimize_Label_MouseLeftButtonDown);
            thisView.GotFocus += new RoutedEventHandler(thisView_GotFocus);
            thisView.LostFocus += new RoutedEventHandler(thisView_LostFocus);
        }

        private void thisView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        public UIElementCollection GetChangableContent()
        {
            return thisView.ChangableContent.Children;
        }

        public void RemoveCloseButton()
        {
            thisView.Header_StackPanel.Children.Remove(thisView.X_Label);
            thisView.X_Label = null;
        }

        public override bool Focus()
        {
            //throw new System.NotImplementedException();
            return false;
        }

        public override void XmlWrite(System.Xml.XmlWriter writer)
        {
            //throw new System.NotImplementedException();
        }

        protected override void GiveNoteFocus(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //throw new System.NotImplementedException();
        }
    }
}