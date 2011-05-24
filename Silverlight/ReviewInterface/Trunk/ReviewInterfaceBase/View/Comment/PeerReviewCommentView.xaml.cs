﻿using System.Windows.Controls;
using System.Windows.Data;
using ReviewInterfaceBase.ViewModel.Comment;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.View.Comment
{
    public partial class PeerReviewCommentPartialView : UserControl
    {
        public PeerReviewCommentViewModel ViewModel
        {
            get;
            set;
        }

        /// <summary>
        /// This creates a new PeerReviewCommentView, NOTE: only to be used by PeerReviewCommentViewModel if you need a noteText
        /// create a PeerReviewCommentViewModel and it will create a View for you.  This is the same for ALL Views and ViewModels
        /// </summary>
        public PeerReviewCommentPartialView(int documentID, ILocation referenceLocation)
        {
            InitializeComponent();
        }

        /// <summary>
        /// This is a work around because binding to text property has a bug in that it does not update
        /// on every character just when it loose focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Note_TextChanged(object sender, TextChangedEventArgs e)
        {
            //this forces the binding to update when Text is Changed... like it should do
            BindingExpression bExp = Note.GetBindingExpression(TextBox.TextProperty);
            if (bExp != null)
            {
                bExp.UpdateSource();
            }
        }
    }
}