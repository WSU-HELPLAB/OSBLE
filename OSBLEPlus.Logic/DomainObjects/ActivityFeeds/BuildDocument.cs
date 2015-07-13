using System;
using OSBLE.Interfaces;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class BuildDocument
    {
        public int BuildId { get; set; }
        
        public virtual BuildEvent Build { get; set; }

        public int DocumentId { get; set; }

        public virtual CodeDocument Document { get; set; }

        public int? NumberOfInserted { get; set; }

        public int? NumberOfModified { get; set; }

        public int? NumberOfDeleted { get; set; }

        public string ModifiedLines { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        public virtual IUser UpdatedByUser { get; set; }
    }
}
