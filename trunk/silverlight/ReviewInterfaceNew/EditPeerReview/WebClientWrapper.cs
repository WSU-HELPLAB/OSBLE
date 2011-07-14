using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using OSBLE.Models.ViewModels.ReviewInterface;
using ReviewInterfaceBase.HelperClasses;

namespace EditPeerReview.HelperClasses
{
    /// <summary>
    /// VS2010 really doesn't like this being the in the ReviewInterfaceBase causes many warnings about two different classes and it picks one but I believe it picks different ones in different places leadings to run time errors
    /// </summary>
    public class WebClientWrapper
    {
        /// <summary>
        /// This fires whenever all the documents have been loaded
        /// </summary>
        public event DocumentsLoadedEventHandler LoadCompleted = delegate { };

        /// <summary>
        /// This is the WebClient we are using.  It does not like getting multiple requests at once and so
        /// this class wraps it to allow for multiple requests 'at once' but really just feed the WebClient one at
        /// a time
        /// </summary>
        private WebClient client = new WebClient();

        //this holds the documentLoactions
        private IEnumerable documentLocations;

        //This is the list of LoadedDocuments
        private List<DocumentInfo> documents = new List<DocumentInfo>();

        //this is the enumerator we use to iterate through the documentLocations
        private IEnumerator documentEnumerator;

        /// <summary>
        /// This gets the Documents that were loaded from the documentLocations.
        /// This is only valid after LoadCompleted has fired
        /// </summary>
        public List<DocumentInfo> Documents
        {
            get { return documents; }
        }

        /// <summary>
        /// The Constructor
        /// </summary>
        /// <param name="documentLocations">Where the documents will be loaded from</param>
        public WebClientWrapper(IEnumerable documentLocations)
        {
            this.documentLocations = documentLocations;
        }

        /// <summary>
        /// This starts the asynchronous load of all the documents
        /// </summary>
        public void StartAsycLoad()
        {
            documentEnumerator = documentLocations.GetEnumerator();

            documentEnumerator.Reset();

            //according to MSDN a next is needed after a reset in order for it to point to the first element because a reset puts it before the first element
            documentEnumerator.MoveNext();

            //Attach a listener for when it is done loading the first document
            client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);

            //Get the current docLocation
            DocumentLocation docLocation = documentEnumerator.Current as DocumentLocation;
            Uri url = new Uri(docLocation.Location, UriKind.Relative);

            //Load the first Document
            client.OpenReadAsync(url, new DocumentInfo(docLocation.ID, docLocation.Location, docLocation.Author, (ReviewInterfaceBase.HelperClasses.Classification)docLocation.Role));
        }

        private void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                //Get the userState which is the DocumentInfo
                DocumentInfo doc = e.UserState as DocumentInfo;

                //Set the stream to be the stream we just loaded
                doc.Stream = e.Result;

                //Add the DocumentInfo to our list
                documents.Add(doc);
            }
            else
            {
                //I should probably fix this error message when I figure out what it should actually say
                MessageBox.Show("There was an error reading one of the files, please reformat your hard drive and try again");
            }

            //We move to the next document
            documentEnumerator.MoveNext();

            //for some reason Enumerator does not know if it is at the end until it tries to read current and fails
            //so a try and catch is the only way
            try
            {
                //Same as above
                DocumentLocation docLocation = documentEnumerator.Current as DocumentLocation;
                Uri url = new Uri(docLocation.Location, UriKind.Relative);
                client.OpenReadAsync(url, new DocumentInfo(docLocation.ID, docLocation.Location, docLocation.Author, (Classification)docLocation.Role));
            }
            catch
            {
                //It is at the end of the list so we must be done so we fire this event
                LoadCompleted(this, new DocumentsLoadedEventArgs(documents));
            }
        }
    }
}