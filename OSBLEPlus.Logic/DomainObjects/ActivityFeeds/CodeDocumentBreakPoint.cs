﻿namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class CodeDocumentBreakPoint
    {
        public int CodeFileId { get; set; }

        public virtual CodeDocument CodeDocument { get; set; }

        public int BreakPointId { get; set; }

        public virtual BreakPoint BreakPoint { get; set; }
    }
}
