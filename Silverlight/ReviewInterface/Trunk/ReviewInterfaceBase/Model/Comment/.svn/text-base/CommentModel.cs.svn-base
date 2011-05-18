using System;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.Model.Comment
{
    public class CommentModel
    {
        public event EventHandler LoadCompleted = delegate { };

        #region Fields

        private static int commentCount = 0;
        private string noteText;
        private ILocation location;
        private string header;
        private string id;

        public string ID
        {
            get { return id; }
            set { id = value; }
        }

        #endregion Fields

        #region Properties

        public string Header
        {
            get { return header; }
            set { header = value; }
        }

        public string NoteText
        {
            get
            {
                return noteText;
            }
            set
            {
                noteText = value;
            }
        }

        public ILocation Location
        {
            get
            {
                return location;
            }
        }

        #endregion Properties

        public CommentModel(ILocation referenceLocation)
        {
            id = commentCount.ToString();
            commentCount++;
            this.location = referenceLocation;
        }
    }
}