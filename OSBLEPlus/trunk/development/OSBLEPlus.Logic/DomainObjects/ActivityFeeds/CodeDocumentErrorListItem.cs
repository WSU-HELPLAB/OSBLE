using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class CodeDocumentErrorListItem
    {
        public int CodeFileId { get; set; }

        public virtual CodeDocument CodeDocument { get { return _code; } set { _code = value; } }

        [NonSerialized]
        private CodeDocument _code;

        public int ErrorListItemId { get; set; }

        public virtual ErrorListItem ErrorListItem { get; set; }
    }
}
