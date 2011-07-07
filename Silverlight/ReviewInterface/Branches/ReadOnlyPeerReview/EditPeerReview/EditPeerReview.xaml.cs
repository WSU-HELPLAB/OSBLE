﻿using System;
using System.Collections.Specialized;
using System.ServiceModel.DomainServices.Client;
using System.Windows.Controls;
using EditPeerReview.HelperClasses;
using ReviewInterfaceBase.ViewModel;
using ReviewInterfaceBase.ViewModel.DocumentHolder;
using ReviewInterfaceBase.Web;
using System.Collections.Generic;

using ReviewInterfaceBase.HelperClasses;
using System.Xml.Linq;

namespace EditPeerReview
{
    public partial class EditPeerReview : UserControl
    {
        private MainPageViewModel mpVM = new MainPageViewModel();

        //my additions
        /// <summary>
        /// This is how we know if the documents are 'opened' on the main page
        /// This only turns to true after they have been opened and the UI thread is started
        /// </summary>
        private bool documentsOpened = false;

        /// <summary>
        /// This keeps a reference for all the PeerReviewDocuments
        /// </summary>
        private IList<DocumentInfo> PeerReviewDocuments = null;



        

        private void ReadPeerReview(XDocument xDoc, string author, Classification role)
        {
            NoteAuthor noteAuthor = new NoteAuthor(role, author);

            IEnumerable<XElement> ele = xDoc.Descendants("Document");

            //For each peerReview we match up the document is made on using the ID from the peerReviewDocument and the DocumentId
            foreach (XElement document in ele)
            {
                //get the id of the document
                string id = document.Attribute("ID").Value;
                foreach (IDocumentHolderViewModel DocumentHolder in mpVM.DocumentHolderViewModels)
                {
                    if (DocumentHolder.idOfDocument.ToString() == id)
                    {
                        DocumentHolder.LoadIssueVotingComments(document, noteAuthor);
                    }
                }
            }
        }

        private void loadPeerReviewLocationsOperation_Completed(object sender, EventArgs e)
        {
            //We have loaded the locations of the PeerReivews now we give it to the WebClient so it can load the documents
            LoadOperation<DocumentLocation> loadPeerReviewLocationOperation = sender as LoadOperation<DocumentLocation>;

            WebClientWrapper clientWrapper = new WebClientWrapper(loadPeerReviewLocationOperation.Entities);
            clientWrapper.LoadCompleted += new DocumentsLoadedEventHandler(PeerReviewDocumentsLoadCompleted);
            clientWrapper.StartAsycLoad();
        }

        private void PeerReviewDocumentsLoadCompleted(object sender, DocumentsLoadedEventArgs e)
        {
            //We have finished loading the PeerReviewDocuments now
            PeerReviewDocuments = e.Documents;
            OpenPeerReviewDocuments();
        }

        private void OpenPeerReviewDocuments()
        {
            //We need two things to have happened before we can proceed.
            //We need for the documents to have been opened by mainPage and we need to have the PeerReviewDocumentsLoad
            //Since both of this involved asynchronous process we cannot say which order they will happen in and thus need to check for both
            //Both should call this function and since both will set their item with in this thread there should be no problems with
            //multi-threaded systems
            if (documentsOpened == true && PeerReviewDocuments != null)
            {
                //Load each document
                foreach (DocumentInfo peerReview in PeerReviewDocuments)
                {
                    ReadPeerReview(XDocument.Load(peerReview.Stream), peerReview.Author, peerReview.Role);
                }
            }
        }

        private void mpVM_OpeningDocumentsComplete(object sender, EventArgs e)
        {
            mpVM.OpeningDocumentsComplete -= new EventHandler(mpVM_OpeningDocumentsComplete);
            documentsOpened = true;
            OpenPeerReviewDocuments();
        }


        //my additions end

        public EditPeerReview()
        {
            InitializeComponent();

            //Attach a handler for when the MainPage is done opening the documents.
            mpVM.OpeningDocumentsComplete += new EventHandler(mpVM_OpeningDocumentsComplete);

            LocalInitilizer();
        }

     
        private void LocalInitilizer()
        {
            mpVM.DocumentHolderViewModels.CollectionChanged += new NotifyCollectionChangedEventHandler(DocumentHolderViewModels_CollectionChanged);

            this.LayoutRoot.Children.Add(mpVM.GetView());

            FakeDomainContext context = new FakeDomainContext();
            FakeDomainContext fakeDomainContext = new FakeDomainContext();
            var entityQuerey = fakeDomainContext.GetDocumentLocationsQuery();
            var loadDocumentLocationsOperation = fakeDomainContext.Load<ReviewInterfaceBase.Web.DocumentLocation>(entityQuerey);
            loadDocumentLocationsOperation.Completed += new EventHandler(loadDocumentLocationsOperation_Completed);


            //Now we start two asynchronous threads and they both need to finish before we can proceed

            /*
            //Load Document Locations
            var documentQuerey = fakeDomainContext.GetDocumentLocationsQuery();
            var loadDocumentLocationsOperation = fakeDomainContext.Load<ReviewInterfaceBase.Web.DocumentLocation>(documentQuerey);
            loadDocumentLocationsOperation.Completed += new EventHandler(loadDocumentLocationsOperation_Completed);*/

            //Load PeerReview Locations
            var peerReivewQuerey = fakeDomainContext.GetPeerReviewLocationsQuery();
            var loadPeerReviewLocationsOperation = fakeDomainContext.Load<ReviewInterfaceBase.Web.DocumentLocation>(peerReivewQuerey);
            loadPeerReviewLocationsOperation.Completed += new EventHandler(loadPeerReviewLocationsOperation_Completed);
        }

        private void loadDocumentLocationsOperation_Completed(object sender, EventArgs e)
        {
            LoadOperation<DocumentLocation> loadOperation = sender as LoadOperation<DocumentLocation>;

            WebClientWrapper clientWrapper = new WebClientWrapper(loadOperation.Entities);
            clientWrapper.LoadCompleted += new DocumentsLoadedEventHandler(clientWrapper_LoadCompleted);
            clientWrapper.StartAsycLoad();
        }

        private void clientWrapper_LoadCompleted(object sender, DocumentsLoadedEventArgs e)
        {
            mpVM.LoadDocuments(e.Documents);
        }


        private void DocumentHolderViewModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (IDocumentHolderViewModel DocumentHolder in e.NewItems)
            {
                DocumentHolder.AllowNewComments();
            }
        }
    }
}