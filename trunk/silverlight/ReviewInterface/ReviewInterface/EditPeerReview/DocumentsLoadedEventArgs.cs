using System;
using System.Collections.Generic;

using ReviewInterfaceBase.HelperClasses;

namespace EditPeerReview.HelperClasses
{
    public delegate void DocumentsLoadedEventHandler(object sender, DocumentsLoadedEventArgs e);

    public class DocumentsLoadedEventArgs : EventArgs
    {
        private IList<DocumentInfo> documents = new List<DocumentInfo>();

        public IList<DocumentInfo> Documents
        {
            get { return documents; }
        }

        public DocumentsLoadedEventArgs(IList<DocumentInfo> documents)
        {
            this.documents = documents;
        }
    }
}