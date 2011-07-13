using System;
using System.Collections.Generic;

using ReviewInterfaceBase.HelperClasses;

namespace IssueVoting.HelperClasses
{
    /// <summary>
    /// This is the eventHandler for DocumentsLoaded
    /// </summary>
    /// <param name="sender">the object that fires this</param>
    /// <param name="e">The event args</param>
    public delegate void DocumentsLoadedEventHandler(object sender, DocumentsLoadedEventArgs e);

    /// <summary>
    /// This is event args for Documents being Loaded it holds a list of documents
    /// </summary>
    public class DocumentsLoadedEventArgs : EventArgs
    {
        private IList<DocumentInfo> documents = new List<DocumentInfo>();

        /// <summary>
        /// Get a lists of Documents
        /// </summary>
        public IList<DocumentInfo> Documents
        {
            get { return documents; }
        }

        /// <summary>
        /// The Constructor
        /// </summary>
        /// <param name="documents">A list of documents</param>
        public DocumentsLoadedEventArgs(IList<DocumentInfo> documents)
        {
            this.documents = documents;
        }
    }
}