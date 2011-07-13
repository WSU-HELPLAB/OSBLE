using System;
using System.Collections.Specialized;
using System.ServiceModel.DomainServices.Client;
using System.Windows.Controls;
using PeerReview.HelperClasses;
using ReviewInterfaceBase.ViewModel;
using ReviewInterfaceBase.ViewModel.DocumentHolder;
using ReviewInterfaceBase.Web;

namespace PeerReview
{
    public partial class PeerReview : UserControl
    {
        private MainPageViewModel mpVM = new MainPageViewModel();

        public PeerReview()
        {
            InitializeComponent();

            LocalInitilizer();
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

        private void LocalInitilizer()
        {
            mpVM.DocumentHolderViewModels.CollectionChanged += new NotifyCollectionChangedEventHandler(DocumentHolderViewModels_CollectionChanged);

            this.LayoutRoot.Children.Add(mpVM.GetView());

            FakeDomainContext context = new FakeDomainContext();
            FakeDomainContext fakeDomainContext = new FakeDomainContext();
            var entityQuerey = fakeDomainContext.GetDocumentLocationsQuery();
            var loadDocumentLocationsOperation = fakeDomainContext.Load<ReviewInterfaceBase.Web.DocumentLocation>(entityQuerey);
            loadDocumentLocationsOperation.Completed += new EventHandler(loadDocumentLocationsOperation_Completed);
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