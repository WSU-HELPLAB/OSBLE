﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using ReviewInterfaceBase.View.Comment;
using ReviewInterfaceBase.ViewModel.CategoryHolder;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Comment
{
    public class PeerReviewCommentViewModel : AbstractCommentViewModel
    {
        //private new PeerReviewCommentView thisView;
        CategoriesHolderViewModel categoriesHolderVM;

        /// <summary>
        /// This creates a NoteText that is a PeerReviewCommentViewModel, PeerReviewCommentView, and CommentModel
        /// PeerReviewCommentView and CommentModel can be accessed by GetView or GetModel
        /// </summary>
        /// <param name="ReferenceLocation">A reference to the location that this noteText is commenting on</param>
        public PeerReviewCommentViewModel(CommentView view, ILocation referenceLocation)
        {
            base.thisView = (view as CommentView);
            //base.Initilize(thisView, referenceLocation);
        }

        public void Initialize(int documentID)
        {
            //setup listeners for thisView
            //thisView.Header.MouseLeftButtonDown += new MouseButtonEventHandler(Header_MouseLeftButtonDown);

            //Set up the Title left click to act the same as the header as the user would except
            /*thisView.Title.MouseLeftButtonDown += new MouseButtonEventHandler(Header_MouseLeftButtonDown);
            thisView.X_Label.MouseLeftButtonDown += new MouseButtonEventHandler(X_Label_MouseLeftButtonDown);
            thisView.Bottem_Right_Corner.MouseLeftButtonDown += new MouseButtonEventHandler(Bottom_Left_Corner_MouseLeftButtonDown);
            thisView.Minimize_Label.MouseLeftButtonDown += new MouseButtonEventHandler(Minimize_Label_MouseLeftButtonDown);*/
            thisView.GotFocus += new RoutedEventHandler(thisView_GotFocus);
            thisView.LostFocus += new RoutedEventHandler(thisView_LostFocus);

            thisView.Note.TextChanged += new TextChangedEventHandler(base.Note_TextChanged);

            categoriesHolderVM = new CategoriesHolderViewModel();

            categoriesHolderVM.LoadCategories(documentID);

            //thisView.LayoutRoot.Children.Insert(1, categoriesHolderVM.GetView());

            categoriesHolderVM.SizeChanged += new SizeChangedEventHandler(categoriesHolderVM_SizeChanged);
        }

        public override bool Focus()
        {
            return thisView.Note.Focus();
        }

        protected override void GiveNoteFocus(object sender, MouseButtonEventArgs e)
        {
            Focus();
        }

        private void categoriesHolderVM_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            Point currentSize = new Point(base.Width, base.Height);

            Point categoriesHolderSize = new Point(categoriesHolderVM.GetView().LayoutRoot.ActualWidth, categoriesHolderVM.GetView().LayoutRoot.ActualHeight);

            //this subtracts everyone else on the comments height to find out what the note height should be
            double noteHeight = currentSize.Y - thisView.Border.BorderThickness.Top -
                thisView.Border.BorderThickness.Bottom - thisView.Header_StackPanel.Height -
                categoriesHolderSize.Y - thisView.Bottem_Right_Corner.Height / 2;

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
            if (categoriesHolderSize.X > thisView.HeaderContent.ActualWidth)
            {
                limitingWidth = categoriesHolderSize.X;
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

        public override void XmlWrite(XmlWriter writer)
        {
            writer.WriteStartElement("Comment");
            writer.WriteAttributeString("ID", thisModel.ID.ToString());
            writer.WriteAttributeString("NoteText", thisModel.NoteText);
            thisModel.Location.WriteXml(writer);
            categoriesHolderVM.WriteXml(writer);
            writer.WriteEndElement();
        }
    }
}