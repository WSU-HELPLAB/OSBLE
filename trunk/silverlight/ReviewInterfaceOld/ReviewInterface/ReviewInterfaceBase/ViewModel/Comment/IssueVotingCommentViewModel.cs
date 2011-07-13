using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.HelperClasses;
using ReviewInterfaceBase.View.Comment;
using ReviewInterfaceBase.ViewModel.CategoryHolder;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Comment
{
    public class IssueVotingCommentViewModel : AbstractCommentViewModel
    {
        private new IssueVotingCommentView thisView;
        private CategoriesHolderViewModel peerReviewCategories;
        private CategoriesHolderViewModel issueVotingCategories;

        public IssueVotingCommentViewModel(XElement xmlComment, NoteAuthor author, ILocation location)
        {
            thisView = new IssueVotingCommentView();

            BorderBrush = author.BorderBrush;
            HeaderBrush = author.HeaderBrush;
            LineBrush = author.LineBrush;
            NoteBrush = author.NoteBrush;
            TextBrush = author.TextBrush;

            base.Initilize(thisView, location);

            thisView.DataContext = this;

            initlizeCommentFromXmlComment(xmlComment, author);

            //setup listeners for thisView
            thisView.Header.MouseLeftButtonDown += new MouseButtonEventHandler(Header_MouseLeftButtonDown);

            //Set up the Title left click to act the same as the header as the user would except
            thisView.Title.MouseLeftButtonDown += new MouseButtonEventHandler(Header_MouseLeftButtonDown);
            thisView.Bottem_Right_Corner.MouseLeftButtonDown += new MouseButtonEventHandler(Bottom_Left_Corner_MouseLeftButtonDown);
            thisView.Minimize_Label.MouseLeftButtonDown += new MouseButtonEventHandler(Minimize_Label_MouseLeftButtonDown);
            thisView.GotFocus += new RoutedEventHandler(thisView_GotFocus);
            thisView.LostFocus += new RoutedEventHandler(thisView_LostFocus);

            thisView.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);
        }

        private void thisView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayout();
        }

        private void initlizeCommentFromXmlComment(XElement xmlComment, NoteAuthor author)
        {
            string authorName = author.Name;
            if (authorName == "Anonymous")
            {
                authorName = author.Role.ToString() + author.AuthorsSoFar;
            }
            thisView.Note.Text = "Author: " + authorName + "\n" + xmlComment.Attribute("NoteText").Value;
            peerReviewCategories = new CategoriesHolderViewModel();
            peerReviewCategories.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);
            XElement categories = xmlComment.Descendants("Categories").ElementAt(0);
            peerReviewCategories.ReadXml(categories);

            thisView.LayoutRoot.Children.Insert(1, peerReviewCategories.GetView());

            issueVotingCategories = new CategoriesHolderViewModel();
            issueVotingCategories.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);
            issueVotingCategories.LoadIssueVotingCategories();
            thisView.LayoutRoot.Children.Insert(2, issueVotingCategories.GetView());
        }

        protected override void GiveNoteFocus(object sender, MouseButtonEventArgs e)
        {
            thisView.Note.Focus();
        }

        public override void XmlWrite(XmlWriter writer)
        {
            //need to write XML
        }

        private void UpdateLayout()
        {
            Point currentSize = new Point(base.Width, base.Height);
            Point peerReviewCategoriesSize = new Point();
            if (peerReviewCategoriesSize != null)
            {
                peerReviewCategoriesSize = new Point(peerReviewCategories.GetView().LayoutRoot.ActualWidth, peerReviewCategories.GetView().LayoutRoot.ActualHeight);
                if (issueVotingCategories != null)
                {
                    double ivCatWidth = issueVotingCategories.GetView().LayoutRoot.ActualWidth;
                    if (ivCatWidth > peerReviewCategoriesSize.X)
                    {
                        peerReviewCategoriesSize.X = ivCatWidth;
                    }
                    peerReviewCategoriesSize.Y += issueVotingCategories.GetView().LayoutRoot.ActualHeight;
                }
            }

            //this subtracts everyone else on the comments height to find out what the note height should be
            double noteHeight = currentSize.Y - thisView.Border.BorderThickness.Top -
                thisView.Border.BorderThickness.Bottom - thisView.Header_StackPanel.Height -
                peerReviewCategoriesSize.Y - thisView.Bottem_Right_Corner.Height / 2;

            //if the noteHeight is less than 0 then all the stuff above cannot fit but it must so
            //change the height so it can fit and change the Note height to 0
            if (noteHeight < 0)
            {
                //this causes another call back to this function but since noteHeight will
                //equal 0 it will not come in here so no recursion
                Height += Math.Abs(noteHeight);
                thisView.Note.Height = 0;
            }
            else
            {
                //otherwise the note takes up as much room as it can
                thisView.Note.Height = noteHeight;
            }

            //find out the width the note can be
            double noteWidth = currentSize.X - thisView.Border.BorderThickness.Left -
                thisView.Border.BorderThickness.Right;

            double limitingWidth;

            //find out if categoriesHolder or the HeaderContent is going to be the limit on minWidth
            if (peerReviewCategoriesSize.X > thisView.HeaderContent.ActualWidth)
            {
                limitingWidth = peerReviewCategoriesSize.X;
            }
            else
            {
                limitingWidth = thisView.HeaderContent.ActualWidth;
            }
            if (noteWidth < limitingWidth)
            {
                //since noteWidth is smaller than the limit we change the width of the whole thing
                //so it meets the limit
                Width = limitingWidth + thisView.Border.BorderThickness.Left +
                thisView.Border.BorderThickness.Right;

                noteWidth = limitingWidth;
            }

            thisView.Header.Width = noteWidth - thisView.HeaderContent.ActualWidth;

            thisView.Note.Width = noteWidth;

            //Call this so it can fire the event and let anyone else know that our size changed
            base.thisView_SizeChanged();
        }
    }
}