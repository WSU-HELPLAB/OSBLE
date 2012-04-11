using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using OSBLE.Models.ViewModels.ReviewInterface;
using ReviewInterfaceBase.HelperClasses;

namespace IssueVoting.HelperClasses
{
    /// <summary>
    /// VS2010 really doesn't like this being the in the ReviewInterfaceBase causes many warnings about two different classes and it picks one but I believe it picks different ones in different places leadings to run time errors
    /// </summary>
    public class WebClientWrapper
    {
        public event DocumentsLoadedEventHandler LoadCompleted = delegate { };
        private WebClient client = new WebClient();
        private IEnumerable documentLocations;
        private List<DocumentInfo> documents = new List<DocumentInfo>();
        private IEnumerator documentEnumerator;

        public List<DocumentInfo> Documents
        {
            get { return documents; }
            set { documents = value; }
        }

        public WebClientWrapper(IEnumerable documentLocations)
        {
            this.documentLocations = documentLocations;
        }

        public void StartAsycLoad()
        {
            documentEnumerator = documentLocations.GetEnumerator();

            documentEnumerator.Reset();
            documentEnumerator.MoveNext();
            //according to MSDN a next is needed after a reset in order for it to point to the first element because a reset puts it before the first element
            client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
            DocumentLocation docLocation = documentEnumerator.Current as DocumentLocation;
            Uri url = new Uri(docLocation.Location, UriKind.Relative);
            client.OpenReadAsync(url, new DocumentInfo(docLocation.ID, docLocation.Location, docLocation.Author, (Classification)docLocation.Role));
        }

        private void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                DocumentInfo doc = e.UserState as DocumentInfo;
                doc.Stream = e.Result;
                documents.Add(doc);
            }
            else
            {
                MessageBox.Show("There was an error.");
            }

            documentEnumerator.MoveNext();

            //for some reason Enumerator does not know if it is at the end until it tries to read current and fails
            //so a try and catch is the only way
            try
            {
                DocumentLocation docLocation = documentEnumerator.Current as DocumentLocation;
                Uri url = new Uri(docLocation.Location, UriKind.Relative);
                client.OpenReadAsync(url, new DocumentInfo(docLocation.ID, docLocation.Location, docLocation.Author, (Classification)docLocation.Role));
            }
            catch
            {
                LoadCompleted(this, new DocumentsLoadedEventArgs(documents));
            }
        }
    }
}