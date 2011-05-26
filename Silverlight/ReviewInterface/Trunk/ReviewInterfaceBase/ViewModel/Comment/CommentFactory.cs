using System.Collections.Generic;
using System.Windows.Controls;
using System.Xml.Linq;
using ReviewInterfaceBase.View.Comment;
using ReviewInterfaceBase.ViewModel.CategoryHolder;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Comment
{
    public enum CommentType
    {
        PeerReview,
        IssueVoting,
        AuthorRebuttal
    }

    public static class CommentFactory
    {
        private static Dictionary<int, CategoriesHolderViewModel> categoriesHolderDictionary = new Dictionary<int, CategoriesHolderViewModel>();

        public static CommentViewModel CreateComment(CommentType type, bool isReadOnly, int documentID, ILocation referenceLocation)
        {
            if (type == CommentType.PeerReview)
            {
                CommentViewModel cv = new CommentViewModel(referenceLocation);
                UIElementCollection changableContent = cv.GetChangableContent();
                changableContent.Add(new PeerReviewCommentPartialView(isReadOnly));

                CategoriesHolderViewModel chvm;
                if (categoriesHolderDictionary.ContainsKey(documentID))
                {
                    chvm = categoriesHolderDictionary[documentID].Clone();
                }
                else
                {
                    chvm = new CategoriesHolderViewModel();
                    chvm.LoadCategories(documentID);
                }
                changableContent.Add(chvm.GetView());
                return cv;
            }
            return null;
        }

        public static CommentViewModel CreateComment(CommentType type, bool isReadOnly, XElement xmlPeerReviewComment, ILocation referenceLocation)
        {
            if (type == CommentType.IssueVoting)
            {
            }
            return null;
        }
    }
}