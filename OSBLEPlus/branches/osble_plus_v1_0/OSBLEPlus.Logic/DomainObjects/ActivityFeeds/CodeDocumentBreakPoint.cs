using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class CodeDocumentBreakPoint
    {
        public int CodeFileId { get; set; }

        public virtual CodeDocument CodeDocument { get { return _code; } set { _code = value; } }

        [NonSerialized]
        private CodeDocument _code;

        public int BreakPointId { get; set; }

        public virtual BreakPoint BreakPoint { get; set; }
    }
}
