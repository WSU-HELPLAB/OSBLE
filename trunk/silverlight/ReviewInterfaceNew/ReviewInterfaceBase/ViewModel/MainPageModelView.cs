﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using OSBLE.Services;
using ReviewInterfaceBase.HelperClasses;
using ReviewInterfaceBase.View;
using ReviewInterfaceBase.View.DocumentHolder;
using ReviewInterfaceBase.ViewModel.DocumentHolder;
using ReviewInterfaceBase.ViewModel.FindWindow;

namespace ReviewInterfaceBase.ViewModel
{
    /// <summary>
    /// This is the top most container
    /// </summary>
    public class MainPageViewModel : INotifyPropertyChanged
    {
        #region Delegates

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        /// <summary>
        /// This is supposed to be fired whenever we would like to close the MainPage but this might not be needed ever
        /// </summary>
        public event EventHandler RequestClose = delegate { };

        /// <summary>
        /// This fires after all the documents are 'open' and displayed that is after the layoutUpdate event has fired.
        /// </summary>
        public event EventHandler OpeningDocumentsComplete = delegate { };

        #endregion Delegates

        #region Fields

        private LoadingWindow loadingWindow = null;

        private ReviewInterfaceDomainContext ReviewInterfaceDC = new ReviewInterfaceDomainContext();

        //OLDCODE
        /// <summary>
        /// This is a reference to rubricViewModel
        /// </summary>
        //private RubricViewModel rubricViewModel = new RubricViewModel();

        private string saveAsDraftButtonContent = "Save As Draft";

        private string publishButtonContent = "Publish";

        /// <summary>
        /// This is a reference to the FindWindowViewModel
        /// </summary>
        private FindWindowViewModel findWindowViewModel = new FindWindowViewModel();

        /// <summary>
        /// This is a reference to the CustomTabControlViewModel
        /// </summary>
        private CustomTabControlViewModel customTabControlViewModel = new CustomTabControlViewModel();

        private ObservableCollection<IDocumentHolderViewModel> documentHolderViewModels = new ObservableCollection<IDocumentHolderViewModel>();

        /// <summary>
        /// This is our flag so we know if we are dragging the Grid Splitter
        /// </summary>
        private bool draggingGridSplitter = false;

        /// <summary>
        /// Our reference to the the MainPageView that this MainPageViewModel is using / manipulating
        /// </summary>
        private MainPageView thisView;

        #endregion Fields

        #region Properties

        public string SaveAsDraftButtonContent
        {
            get { return saveAsDraftButtonContent; }
            set
            {
                saveAsDraftButtonContent = value;
                PropertyChanged(this, new PropertyChangedEventArgs("SaveAsDraftButtonContent"));
            }
        }

        public string PublishButtonContent
        {
            get { return publishButtonContent; }
            set
            {
                publishButtonContent = value;
                PropertyChanged(this, new PropertyChangedEventArgs("PublishButtonContent"));
            }
        }

        //OLDCODE
        /*
        public RubricViewModel RubricViewModel
        {
            get { return rubricViewModel; }
            set { rubricViewModel = value; }
        }
         */

        public FindWindowViewModel FindWindowViewModel
        {
            get { return findWindowViewModel; }
            set { findWindowViewModel = value; }
        }

        public CustomTabControlViewModel CustomTabControlViewModel
        {
            get { return customTabControlViewModel; }
            set { customTabControlViewModel = value; }
        }

        public ObservableCollection<IDocumentHolderViewModel> DocumentHolderViewModels
        {
            get { return documentHolderViewModels; }
            set { documentHolderViewModels = value; }
        }

        #endregion Properties

        #region Constructor

        /// <summary>
        /// Constructor: sets up the view and attaches the needed EventHandlers
        /// </summary>
        public MainPageViewModel()
        {
            //first initialize our view
            thisView = new MainPageView();

            thisView.DataContext = this;

            //This setups up all the events needed on the view directly
            thisView.PublishTop.Click += new RoutedEventHandler(Save_Click);
            thisView.PublishBottom.Click += new RoutedEventHandler(Save_Click);
            thisView.SaveAsDraftTop.Click += new RoutedEventHandler(SaveAsDraft_Click);
            thisView.SaveAsDraftBottom.Click += new RoutedEventHandler(SaveAsDraft_Click);
            thisView.SizeChanged += new SizeChangedEventHandler(thisView_SizeChanged);
            thisView.MouseRightButtonDown += new MouseButtonEventHandler(thisView_MouseRightButtonDown);

            /*OLDCODE2
            //This sets up the events needed for the GridSplitter technically this should be its own View and ViewModel but
            //to much overhead
            thisView.GridSplitter.MouseLeftButtonDown += new MouseButtonEventHandler(GridSplitter_MouseLeftButtonDown);
            thisView.GridSplitter.MouseMove += new MouseEventHandler(GridSplitter_MouseMove);
            thisView.GridSplitter.MouseLeftButtonUp += new MouseButtonEventHandler(GridSplitter_MouseLeftButtonUp);
            thisView.GridSplitter.LostMouseCapture += new MouseEventHandler(GridSplitter_LostMouseCapture);
             */

            findWindowViewModel.FindNext += new EventHandler(findWindowViewModel_FindNext);

            //OLDCODE
            //We set the content of the RubricScrollViewer to be that of the rubricViewModel's view
            //thisView.RubricScrollViewer.Content = rubricViewModel.GetView();
            //rubricViewModel.SizeChanged += new SizeChangedEventHandler(rubricViewModel_SizeChanged);

            //then we add the FindWindowViewModel's View to the LayoutRoot Children
            thisView.LayoutRoot.Children.Add(findWindowViewModel.GetView());

            thisView.CustomTabControlHolder.Content = customTabControlViewModel.GetView();

            customTabControlViewModel.SwitchedTabs += new SwitchedTabEventHandler(customTabControlViewModel_SwitchedTabs);
        }

        #endregion Constructor

        #region Private Event Handlers

        private void customTabControlViewModel_SwitchedTabs(object sender, SwitchedTabEventArgs e)
        {
            if (e.NewTab != null && e.NewTab.Content != null)
            {
                (e.NewTab.Content as IDocumentHolderView).GetViewModel().IsDisplayed = true;
            }
            if (e.OldTab != null && e.OldTab.Content != null)
            {
                (e.OldTab.Content as IDocumentHolderView).GetViewModel().IsDisplayed = false;
            }
        }

        private void findWindowViewModel_FindNext(object sender, EventArgs e)
        {
            findNext();
        }

        public void LoadDocuments(IList<DocumentInfo> Documents)
        {
            bool first = true;
            TabItem firstDocHolder = null;
            //This is where we open and display the documents
            foreach (DocumentInfo document in Documents)
            {
                //Each document gets its own tab
                TabItem DocumentHolder = new TabItem();
                DocumentHolder.Header = document.FileName;
                IDocumentHolderViewModel DocumentHolderViewModel;

                if (document.FileExtension == ".wmv")
                {
                    DocumentHolderViewModel = new VideoDocumentHolderViewModel(document.Id, document.Stream);
                }
                else
                {
                    DocumentHolderViewModel = new SpatialDocumentHolderViewModel(document.Id, new StreamReader(document.Stream), document.FileExtension);

                    (DocumentHolderViewModel as SpatialDocumentHolderViewModel).FindWindowRequested += new EventHandler(avm_FindWindowRequested);
                }

                DocumentHolderViewModels.Add(DocumentHolderViewModel);

                DocumentHolder.Content = DocumentHolderViewModel.GetView();

                customTabControlViewModel.AddTabItem(DocumentHolder);

                //Set the new DocumentHolder as the one selected by the TabControl
                customTabControlViewModel.SelectedTab = DocumentHolder;

                //Then UpdateLayout this will force all of its UI to be sorted out this mostly affects
                //the richTextBox which doesn't figure out its content until is displayed which is problematic
                //when we try to attach comments to the runs that don't really exist yet.
                thisView.UpdateLayout();

                if (first)
                {
                    firstDocHolder = DocumentHolder;
                    first = false;
                }
            }

            customTabControlViewModel.SelectedTab = firstDocHolder;

            //We fire this because the documents have been updated by the UI thread
            thisView.UpdateLayout();
            OpeningDocumentsComplete(this, EventArgs.Empty);
        }

        private void DocumentHolder_LayoutUpdated(object sender, EventArgs e)
        {
            //We fire this because the documents have been updated by the UI thread
            //OpeningDocumentsComplete(this, EventArgs.Empty);
        }

        private StringBuilder SaveReview()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "   ";

            StringBuilder sb = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                //we write the first overall element
                writer.WriteStartElement("ReviewInterfaceBasePeerReview");
                //Then each DocumentHolder writes whatever it has too.
                foreach (IDocumentHolderViewModel DocumentHolderViewModel in DocumentHolderViewModels)
                {
                    DocumentHolderViewModel.WriteXml(writer);
                }
                writer.WriteEndElement();
            }

            //I do not know why but it writes utf-16 and then throws an error when it tries to read it so this is a hot fix
            //I know we want utf-8 but not sure how to the xmlWriter to write that so I just change it manually.
            sb.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            return sb;
        }

        private void SaveAsDraft_Click(object sender, RoutedEventArgs e)
        {
            ReviewInterfaceDomainContext ReviewInterfaceDC = new ReviewInterfaceDomainContext();
            //Then we upload the file as well as register an event for when it has been uploaded
            ReviewInterfaceDC.UploadReviewDraft(SaveReview().ToString()).Completed += new EventHandler(FileUploadComplete);
            SaveAsDraftButtonContent = "Saving...";
        }

        /// <summary>
        /// This is the code that is run when the files is saved
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">not used</param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            //ShowLoadingWindow();
            ReviewInterfaceDomainContext ReviewInterfaceDC = new ReviewInterfaceDomainContext();

            //Then we upload the file as well as register an event for when it has been uploaded
            ReviewInterfaceDC.UploadFile(SaveReview().ToString()).Completed += new EventHandler(FileUploadComplete);

            PublishButtonContent = "Saving...";

            //We hide the SaveAsDraft because when something has been publish it can no longer be saved as a draft
            thisView.SaveAsDraftBottom.Visibility = Visibility.Collapsed;
            thisView.SaveAsDraftTop.Visibility = Visibility.Collapsed;
        }

        private void FileUploadComplete(object sender, EventArgs e)
        {
            PublishButtonContent = "Publish";
            SaveAsDraftButtonContent = "Save As Draft";
            MessageBox.Show("The file was saved");
        }

        private void thisView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //to prevent the Silverlight context menu;
            e.Handled = true;
        }

        private void thisView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            thisView.LayoutRoot.RowDefinitions[0].Height = new GridLength(thisView.MainBorder.ActualHeight - (thisView.MainBorder.BorderThickness.Top + thisView.MainBorder.BorderThickness.Bottom) /*OLDCODE - ( thisView.LayoutRoot.RowDefinitions[2].ActualHeight + thisView.LayoutRoot.RowDefinitions[1].ActualHeight)*/);
            thisView.CustomTabControlHolder.Height = thisView.LayoutRoot.RowDefinitions[0].Height.Value - (thisView.ButtonToolbarBottom.ActualHeight + thisView.ButtonToolbarTop.ActualHeight);
            /*OLDCODE  */
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            //This was code for the early days of testing before it was hooked up to a web service it should still work
            //If hooked up again
            /*
            //we assume for now it is a text file
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Multiselect = true;

            bool first = true;
            if (openFileDialog1.ShowDialog() == true)
            {
                customTabControlViewModel = new CustomTabControlViewModel();
                if (thisView.CustomTabControlHolder.Content != null)
                {
                    //(thisView.CustomTabControlHolder.Content as CustomTabControlViewModel)
                }
                thisView.CustomTabControlHolder.Content = customTabControlViewModel.GetView();

                foreach (FileInfo file in openFileDialog1.Files)
                {
                    TabItem DocumentHolder = new TabItem();
                    DocumentHolder.Header = file.Name;
                    IDocumentHolderViewModel DocumentHolderViewModel;

                    if (file.Extension == ".wmv")
                    {
                        DocumentHolderViewModel = new VideoDocumentHolderViewModel(0, file);
                    }
                    else
                    {
                        DocumentHolderViewModel = new SpatialDocumentHolderViewModel(0, file);

                        (DocumentHolderViewModel as SpatialDocumentHolderViewModel).FindWindowRequested += new EventHandler(avm_FindWindowRequested);
                    }

                    DocumentHolderViewModels.Add(DocumentHolderViewModel);

                    DocumentHolder.Content = DocumentHolderViewModel.GetView();

                    customTabControlViewModel.AddTabItem(DocumentHolder);
                    if (first)
                    {
                        customTabControlViewModel.SelectedTab = DocumentHolder;
                        first = false;
                    }
                }
            }*/
        }

        private void findNext()
        {
            object found = null;

            int index = 0;

            if (findWindowViewModel.FirstDocument == null)
            {
                findWindowViewModel.FirstDocument = DocumentHolderViewModels[customTabControlViewModel.SelectedTabIndex];
                findWindowViewModel.CurrentDocument = findWindowViewModel.FirstDocument;
                findWindowViewModel.CurrentLocationFound = null;
            }

            found = findWindowViewModel.CurrentDocument.FindNext(findWindowViewModel.CurrentLocationFound, findWindowViewModel.Options);

            while (found == null)
            {
                index = DocumentHolderViewModels.IndexOf(findWindowViewModel.CurrentDocument);
                if (index >= DocumentHolderViewModels.Count)
                {
                    index = 0;
                }

                findWindowViewModel.CurrentDocument = DocumentHolderViewModels[index];

                //if we reached where we started we are done
                if (findWindowViewModel.CurrentDocument == findWindowViewModel.FirstDocument)
                {
                    break;
                }

                //we have not yet looked threw this document so lastLocation is going to be null
                found = findWindowViewModel.CurrentDocument.FindNext(null, findWindowViewModel.Options);
            }

            if (found == null)
            {
                MessageBox.Show("We Reached the End");
                findWindowViewModel.FirstDocument = null;
            }
            else
            {
                (findWindowViewModel.CurrentDocument as SpatialDocumentHolderViewModel).ScrollToComment(found);
            }
            findWindowViewModel.CurrentLocationFound = found;
        }

        private void avm_FindWindowRequested(object sender, EventArgs e)
        {
            //if open close if close open
            findWindowViewModel.isOpen = !findWindowViewModel.isOpen;
        }

        //OLDCODE
        /*
        private void rubricViewModel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            thisView.LayoutRoot.RowDefinitions[0].Height = new GridLength(thisView.ActualHeight - (thisView.LayoutRoot.RowDefinitions[2].ActualHeight + thisView.LayoutRoot.RowDefinitions[1].ActualHeight));
            thisView.CustomTabControlHolder.Height = thisView.LayoutRoot.RowDefinitions[0].Height.Value - (thisView.ButtonToolbar.ActualHeight + 10);
        }
         */

        /*OLDCODE2
        private void GridSplitter_LostMouseCapture(object sender, MouseEventArgs e)
        {
            draggingGridSplitter = false;
        }

        private void GridSplitter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            thisView.GridSplitter.ReleaseMouseCapture();
            draggingGridSplitter = false;
        }

        private void GridSplitter_MouseMove(object sender, MouseEventArgs e)
        {
             //This controls how the grid works on the GridSplitter is dragged
            if (draggingGridSplitter)
            {
                double rubricHeight = thisView.ActualHeight - e.GetPosition(thisView).Y;
                double gridSplitterHeight = thisView.LayoutRoot.RowDefinitions[1].ActualHeight;
                double DocumentHeight = thisView.ActualHeight - (thisView.LayoutRoot.RowDefinitions[2].ActualHeight + thisView.LayoutRoot.RowDefinitions[1].ActualHeight);
                if (rubricHeight < gridSplitterHeight)
                {
                    DocumentHeight -= gridSplitterHeight;
                    rubricHeight = 0;
                }
                else if (DocumentHeight < thisView.ButtonToolbar.ActualHeight + 10)
                {
                    rubricHeight = thisView.ActualHeight;
                    rubricHeight -= (gridSplitterHeight + thisView.ButtonToolbar.ActualHeight + 10);
                    DocumentHeight = thisView.ButtonToolbar.ActualHeight + 10;
                }
                thisView.LayoutRoot.RowDefinitions[2].Height = new GridLength(rubricHeight);
                thisView.LayoutRoot.RowDefinitions[0].Height = new GridLength(DocumentHeight);
                thisView.CustomTabControlHolder.Height = DocumentHeight - (thisView.ButtonToolbar.ActualHeight + 10);
            }
        }

        private void GridSplitter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            thisView.GridSplitter.CaptureMouse();
            draggingGridSplitter = true;
        }
        */

        #endregion Private Event Handlers

        #region Public Methods

        /// <summary>
        /// This function should only be used to get a reference to the view so it can be added a UIElement the
        /// view should never be manipulated directly.
        /// </summary>
        /// <returns>A Reference to the view that is currently being manipulated by this view model.</returns>
        public MainPageView GetView()
        {
            return thisView;
        }

        public void HideSaveButtons()
        {
            thisView.PublishTop.Visibility = Visibility.Collapsed;
            thisView.SaveAsDraftTop.Visibility = Visibility.Collapsed;
            thisView.PublishBottom.Visibility = Visibility.Collapsed;
            thisView.SaveAsDraftBottom.Visibility = Visibility.Collapsed;
        }

        public void HideDraftButton()
        {
            thisView.SaveAsDraftTop.Visibility = Visibility.Collapsed;
            thisView.SaveAsDraftBottom.Visibility = Visibility.Collapsed;
        }

        public void ShowLoadingWindow()
        {
            if (loadingWindow == null)
            {
                loadingWindow = new LoadingWindow();
            }
            // loadingWindow.Show();
        }

        public void HideLoadingWindow()
        {
            if (loadingWindow != null)
            {
                MessageBox.Show("Closing Loading Window");
                loadingWindow.Close();
            }
        }

        #endregion Public Methods
    }
}