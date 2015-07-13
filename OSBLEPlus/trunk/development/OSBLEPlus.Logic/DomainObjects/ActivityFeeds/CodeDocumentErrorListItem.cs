namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class CodeDocumentErrorListItem
    {
        public int CodeFileId { get; set; }

        public virtual CodeDocument CodeDocument { get; set; }

        public int ErrorListItemId { get; set; }

        public virtual ErrorListItem ErrorListItem { get; set; }
    }
}
