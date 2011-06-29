using System.Xml;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Comment
{
    public static class CommentFactory
    {
        public static PeerReviewCommentViewModel CreateNewComment(int documentID, ILocation ReferenceLocation)
        {
            return null;
        }

        public static PeerReviewCommentViewModel LoadComment(XmlReader reader)
        {
            return null;
        }
    }
}