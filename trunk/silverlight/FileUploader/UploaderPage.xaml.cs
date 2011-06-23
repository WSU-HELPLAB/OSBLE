using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using FileUploader.OsbleServices;
using FileUploader.Controls;
using System.IO.IsolatedStorage;
using System.Windows.Threading;

namespace FileUploader
{
    public partial class UploaderPage : Page
    {
        private string authToken = "";
        private string OsbleUrl = "http://localhost:17532";
        public string RootPath;
        private RemoteFilesContextMenu remoteContextMenu = new RemoteFilesContextMenu();
        private LocalFilesContextMenu localContextMenu = new LocalFilesContextMenu();

        //reference to our web service
        private UploaderWebServiceClient client = new UploaderWebServiceClient();

        //used for pulling files from the server
        private WebClient fileClient = new WebClient();

        // stores 
        private KeyValuePair<int, string> course;

        DispatcherTimer loginCheck = new DispatcherTimer();

        public string LocalPath
        {
            get
            {
                return LocalFileTextBox.Text;
            }
            set
            {
                LocalFileTextBox.Text = value;

                //catch root folder issues on windows systems
                if (LocalPath.Length > 0 && LocalPath.Substring(LocalPath.Length - 1) == ":")
                {
                    LocalPath += "\\";
                }
                
            }
        }

        public UploaderPage(string authenticationToken)
        {
            InitializeComponent();
            
            authToken = authenticationToken;

            //get our local path
            LocalPath =  GetLastLocalPath();
            RootPath = LocalPath;

            //attach a right-click menu to the remote files list
            remoteContextMenu.MenuItemSelected += new EventHandler<RemoteFileEventArgs>(remoteContextMenu_MenuItemSelected);
            RemoteFileList.SetValue(ContextMenuService.ContextMenuProperty, remoteContextMenu);

            //do the same for our local menu
            localContextMenu.MenuItemSelected += new EventHandler<LocalFileEventArgs>(localContextMenu_MenuItemSelected);
            LocalFileList.SetValue(ContextMenuService.ContextMenuProperty, localContextMenu);

            //listeners for our web service
            client.GetFileListCompleted += new EventHandler<GetFileListCompletedEventArgs>(GetFileListCompleted);
            client.GetValidUploadLocationsCompleted += new EventHandler<GetValidUploadLocationsCompletedEventArgs>(GetValidUploadLocationsCompleted);
            client.DeleteFileCompleted += new EventHandler<DeleteFileCompletedEventArgs>(SelectionChanged);
            client.UpdateListingOrderCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(client_UpdateListingOrderCompleted);
            client.IsValidKeyCompleted += new EventHandler<IsValidKeyCompletedEventArgs>(client_IsValidKeyCompleted);

            //local event listeners
            SyncButton.Click += new RoutedEventHandler(SyncButton_Click);
            SendFileButton.Click += new RoutedEventHandler(SendSingleFile);
            LocalFileList.EmptyDirectoryEncountered += new EventHandler(LocalFileList_EmptyDirectoryEncountered);
            LocalFileList.ParentDirectoryRequest += new EventHandler(LocalFileList_ParentDirectoryRequest);
            LocalFileTextBox.KeyUp += new KeyEventHandler(LocalFileTextBox_KeyUp);
            UploadLocation.SelectionChanged += new SelectionChangedEventHandler(SelectionChanged);
            UpButton.Click += new RoutedEventHandler(MoveRemoteSelectionUp);
            DownButton.Click += new RoutedEventHandler(MoveRemoteSelectionDown);
            RemoveRemoteSelectionButton.Click += new RoutedEventHandler(RemoveRemoteSelectionButton_Click);
            DownloadRemoteFileButton.Click += new RoutedEventHandler(DownloadRemoteFile);
            fileClient.OpenReadCompleted += new OpenReadCompletedEventHandler(DownloadRemoteFileCompleted);

            //get the remote server file list
            client.GetValidUploadLocationsAsync(authToken);
            
            //get the local files
            LocalFileList.DataContext = FileOperations.BuildLocalDirectoryListing(LocalPath);

            //periodically check login credentials
            loginCheck.Interval = new TimeSpan(0, 0, 1, 0, 0);
            loginCheck.Tick += new EventHandler(loginCheck_Tick);
            loginCheck.Start();

        }

        void client_IsValidKeyCompleted(object sender, IsValidKeyCompletedEventArgs e)
        {
            //if we lost authentication for some reason, we need to display the login
            //box again
            if (!e.Result)
            {
                loginCheck.Stop();
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.ValidTokenReceived += new EventHandler(ValidTokenReceived);
                loginWindow.Show();
            }
        }

        /// <summary>
        /// Called once we receive a valid login token
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValidTokenReceived(object sender, EventArgs e)
        {
            LoginWindow window = sender as LoginWindow;
            this.authToken = window.Token;
            loginCheck.Start();
        }

        void client_UpdateListingOrderCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            DownButton.IsEnabled = true;
            UpButton.IsEnabled = true;
        }

        void DownloadRemoteFile(object sender, RoutedEventArgs e)
        {
            if (RemoteFileList.SelectedItem is FileListing)
            {
                //send a request to download the selected file
                FileListing fi = RemoteFileList.SelectedItem as FileListing;
                Uri uri = new Uri(OsbleUrl + fi.FileUrl);
                RemoteFileList.IsEnabled = false;
                DownloadRemoteFileButton.IsEnabled = false;
                DownloadRemoteFileButton.Content = "Downloading...";
                fileClient.OpenReadAsync(uri);
            }
        }

        void DownloadRemoteFileCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            Stream remoteStream = e.Result;
            AbstractListing listing = RemoteFileList.SelectedItem;
            
            //write the file to the current local path, using the server file name
            string writePath = Path.Combine(LocalPath, listing.Name);
            FileStream localStream = new FileStream(writePath, FileMode.Create);
            
            //what a cool function!
            remoteStream.CopyTo(localStream);

            //close everything up, re-enable various buttons
            remoteStream.Close();
            localStream.Close();
            RemoteFileList.IsEnabled = true;
            DownloadRemoteFileButton.IsEnabled = true;
            DownloadRemoteFileButton.Content = "Download Selected File";

            //refresh the local data context
            LocalFileList.DataContext = FileOperations.BuildLocalDirectoryListing(LocalPath);
        }

        void GetFileListCompleted(object sender, GetFileListCompletedEventArgs e)
        {
            RemoteFileList.DataContext = e.Result;
        }

        void GetValidUploadLocationsCompleted(object sender, GetValidUploadLocationsCompletedEventArgs e)
        {
            UploadLocation.Items.Clear();
            UploadLocation.ItemsSource = e.Result;
            if (e.Result.Count > 0)
            {
                UploadLocation.SelectedIndex = 0;
            }
        }        

        /// <summary>
        /// Returns the path of the last path on this machine that was synced with the server
        /// </summary>
        /// <returns></returns>
        private string GetLastLocalPath()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists("localpath.txt"))
                {
                    using (IsolatedStorageFileStream stream = store.OpenFile("localpath.txt", FileMode.Open))
                    {
                        StreamReader reader = new StreamReader(stream);
                        string path = reader.ReadLine().Trim();
                        reader.Close();
                        return path;
                    }
                }
                else
                {
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
            }
        }

        /// <summary>
        /// Called whenever we receive input from our local context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void localContextMenu_MenuItemSelected(object sender, LocalFileEventArgs e)
        {
            switch (e.ActionRequested)
            {
                case LocalFileActions.SendSingleFile:
                    SendSingleFile(this, new RoutedEventArgs());
                    break;
            }
        }

        //allows the user to quick navigate to a desired location
        void LocalFileTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LocalPath = (sender as TextBox).Text;
                //LocalFileList_EmptyDirectoryEncountered(new FileList { DataContext = new DirectoryListing() { Name = "" } }, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Moves up one directory on the local machine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocalFileList_ParentDirectoryRequest(object sender, EventArgs e)
        {
            FileList list = sender as FileList;
            list.ClearPreviousDirectories();
            DirectoryListing listing = list.DataContext;
            LocalPath = LocalPath.Substring(0, LocalPath.LastIndexOf('\\'));
            LocalFileList.DataContext = FileOperations.BuildLocalDirectoryListing(LocalPath);
        }

        /// <summary>
        /// Called when our login timer fires
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void loginCheck_Tick(object sender, EventArgs e)
        {
            client.IsValidKeyAsync(authToken);
        }

        /// <summary>
        /// Moves down into the selected directory on the local machine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocalFileList_EmptyDirectoryEncountered(object sender, EventArgs e)
        {
            FileList list = sender as FileList;
            list.ClearPreviousDirectories();
            DirectoryListing listing = list.DataContext;
            LocalPath = Path.Combine(LocalPath, listing.Name);
            LocalFileList.DataContext = FileOperations.BuildLocalDirectoryListing(LocalPath);
        }

        void MoveRemoteSelectionDown(object sender, RoutedEventArgs e)
        {
            DownButton.IsEnabled = false;
            UpButton.IsEnabled = false;
            RemoteFileList.MoveSelectionDown();
            client.UpdateListingOrderAsync(RemoteFileList.DataContext, course.Key, authToken);
        }

        void MoveRemoteSelectionUp(object sender, RoutedEventArgs e)
        {
            DownButton.IsEnabled = false;
            UpButton.IsEnabled = false;
            RemoteFileList.MoveSelectionUp();
            client.UpdateListingOrderAsync(RemoteFileList.DataContext, course.Key, authToken);
        }

        /// <summary>
        /// Called whenever we receive a message from our remote context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void remoteContextMenu_MenuItemSelected(object sender, RemoteFileEventArgs e)
        {
            switch (e.ActionRequested)
            {
                case RemoteFileActions.Delete:
                    ConfirmDeleteButton confirm = new ConfirmDeleteButton();
                    confirm.Show();
                    confirm.OKButton.Click += new RoutedEventHandler(RemoveRemoteSelectionButton_Click);
                    break;

                case RemoteFileActions.Download:
                    DownloadRemoteFile(this, new RoutedEventArgs());
                    break;

                case RemoteFileActions.MoveDown:
                    MoveRemoteSelectionDown(this, new RoutedEventArgs());
                    break;

                case RemoteFileActions.MoveUp:
                    MoveRemoteSelectionUp(this, new RoutedEventArgs());
                    break;
            }

            //The remote file list's selection change event will fire through we're not technically clicking on
            //a file list item.  To get around an accidental tree traversal, set the selected item to null
            RemoteFileList.ClearSelection();
        }

        /// <summary>
        /// Will send the currently selected file to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SendSingleFile(object sender, RoutedEventArgs e)
        {
            //as long as something was selected, we're good to go
            if (LocalFileList.SelectedItem != null)
            {
                //two cases: if a FileListItem is selected, then we need to smash it
                //into a DirectoryListing.  If it's a DirectoryListing, then we're good
                //to go.
                DirectoryListing dl = new DirectoryListing() { Files = new ObservableCollection<FileListing>(), Directories = new ObservableCollection<DirectoryListing>() };
                if (LocalFileList.SelectedItem is FileListing)
                {
                    dl.Files.Add(LocalFileList.SelectedItem as FileListing);
                }
                else if(LocalFileList.SelectedItem is DirectoryListing)
                {
                    //if it's a directory listing, then we need to send the inner contents of the directory
                    dl.Directories.Add(FileOperations.BuildLocalDirectoryListing(LocalFileList.SelectedItem.AbsolutePath, false, true));
                }

                //almost exaxtly the same code as in "SyncButton_Click".  Might want to refactor at
                //some point
                UploadingModal uploader = new UploadingModal();
                uploader.Listing = dl;
                uploader.CourseId = course.Key;
                uploader.AuthToken = authToken;
                uploader.BeginUpload();
                uploader.Show();
                uploader.Closed += new EventHandler(uploader_Closed);
            }
        }

        /// <summary>
        /// Saves the last location synced to the server
        /// </summary>
        /// <param name="path"></param>
        private void SavelastLocalPath(string path)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream stream = store.OpenFile("localpath.txt", FileMode.Create))
                {
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(path);
                    writer.Close();
                }
            }
        }

        void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            //save the last local path
            SavelastLocalPath(LocalPath);

            DirectoryListing listing = FileOperations.BuildLocalDirectoryListing(LocalPath, false, true);
            UploadingModal uploader = new UploadingModal();
            uploader.Listing = listing;
            uploader.CourseId = course.Key;
            uploader.AuthToken = authToken;
            uploader.BeginUpload();
            uploader.Show();
            uploader.Closed += new EventHandler(uploader_Closed);
        }

        /// <summary>
        /// Call to remove the selected item from the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RemoveRemoteSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            client.DeleteFileAsync(RemoteFileList.SelectedItem.AbsolutePath, course.Key, authToken);
        }

        void uploader_Closed(object sender, EventArgs e)
        {
            SelectionChanged(this, null);
        }

        void SelectionChanged(object sender, EventArgs e)
        {
            //very hack-ish at the moment, may need to revise
            course = (KeyValuePair<int, string>)UploadLocation.SelectedValue;
            client.GetFileListAsync(course.Key, authToken);
        }
    }
}
