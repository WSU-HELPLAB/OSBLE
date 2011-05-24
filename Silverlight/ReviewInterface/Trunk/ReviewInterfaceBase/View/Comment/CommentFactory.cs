namespace ReviewInterfaceBase.View.Comment
{
    public static class CommentFactory
    {
        public enum CommentType
        {
            PeerReview,
            IssueVoting,
            AuthorRebuttal
        }

        public static CommentView CreateComment(CommentType type, bool isReadOnly)
        {
            if (type == CommentType.PeerReview)
            {
                CommentView cv = new CommentView();
                return omment();
            }
        }
    }
}