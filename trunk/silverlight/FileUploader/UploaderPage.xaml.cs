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

namespace FileUploader
{
    public partial class UploaderPage : Page
    {
        private string authToken = "";
        private bool FileRecieved;
        public string RootPath;

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

        //reference to our web service
        UploaderWebServiceClient syncedFiles = new UploaderWebServiceClient();

        // Modal Syncing progress window
        UploadingModal syncing = new UploadingModal();
        // Modal Uploading progress window
        UploadingModal uploading = new UploadingModal();

        // Creating course key for upload location combo box
        KeyValuePair<int, string> course;

        public UploaderPage(string authenticationToken)
        {
            InitializeComponent();
            
            authToken = authenticationToken;

            //get our local path
            LocalPath =  GetLastLocalPath();
            RootPath = LocalPath;

            //listeners for our web service
            syncedFiles.GetFileListCompleted += new EventHandler<GetFileListCompletedEventArgs>(syncedFiles_GetFileListCompleted);
            syncedFiles.SyncFileCompleted += new EventHandler<SyncFileCompletedEventArgs>(syncedFiles_SyncFileCompleted);
            syncedFiles.GetValidUploadLocationsCompleted += new EventHandler<GetValidUploadLocationsCompletedEventArgs>(syncedFiles_GetValidUploadLocationsCompleted);

            //local event listeners
            SyncButton.Click += new RoutedEventHandler(SyncButton_Click);
            SendFileButton.Click += new RoutedEventHandler(SendFileButton_Click);
            LocalFileList.EmptyDirectoryEncountered += new EventHandler(LocalFileList_EmptyDirectoryEncountered);
            LocalFileList.ParentDirectoryRequest += new EventHandler(LocalFileList_ParentDirectoryRequest);
            LocalFileTextBox.KeyUp += new KeyEventHandler(LocalFileTextBox_KeyUp);
            UploadLocation.SelectionChanged += new SelectionChangedEventHandler(UploadLocation_SelectionChanged);
            UpButton.Click += new RoutedEventHandler(UpButton_Click);
            DownButton.Click += new RoutedEventHandler(DownButton_Click);

            //get the remote server file list
            syncedFiles.GetValidUploadLocationsAsync(authToken);
            
            //get the local files
            LocalFileList.DataContext = BuildLocalDirectoryListing(LocalPath);
        }

        void DownButton_Click(object sender, RoutedEventArgs e)
        {
            RemoteFileList.MoveSelectionDown();
            syncedFiles.UpdateListingOrderAsync(RemoteFileList.DataContext, course.Key, authToken);
        }

        void UpButton_Click(object sender, RoutedEventArgs e)
        {
            RemoteFileList.MoveSelectionUp();
            syncedFiles.UpdateListingOrderAsync(RemoteFileList.DataContext, course.Key, authToken);
        }

        void UploadLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //very hack-ish at the moment, may need to revise
            course = (KeyValuePair<int, string>)UploadLocation.SelectedValue;
            syncedFiles.GetFileListAsync(course.Key, authToken);
        }

        void syncedFiles_GetValidUploadLocationsCompleted(object sender, GetValidUploadLocationsCompletedEventArgs e)
        {
            UploadLocation.Items.Clear();
            UploadLocation.ItemsSource = e.Result;
            UploadLocation.SelectedIndex = 0;
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
            LocalPath = listing.Name.Substring(0, listing.Name.LastIndexOf('\\'));
            LocalFileList.DataContext = BuildLocalDirectoryListing(LocalPath);
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
            LocalFileList.DataContext = BuildLocalDirectoryListing(LocalPath);
        }

        void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            uploading.LayoutUpdated += new EventHandler(uploading_LayoutUpdated);
            uploading.Show();
        }

        void uploading_LayoutUpdated(object sender, EventArgs e)
        {
            uploading.LayoutUpdated -= new EventHandler(uploading_LayoutUpdated);
            string relpath = "";
            string selected = "";
            DirectoryListing Listing = new DirectoryListing();
            
            /*****************************************************************
            // needs to be set to the selected dir or file data context
            Listing = this.LocalFileList.DataContext;

            // need to assign selected file or dir to "selected"
            selected = LocalPath;

            //sets the amount of elements in the progress bar
            uploading.UploadProgressBar.Maximum = 10000;
            *****************************************************************/

            traveseDirectories(selected, relpath, Listing);

            // update the synced files Window
            syncedFiles.GetFileListAsync(course.Key, authToken);
        }

        void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            syncing.LayoutUpdated += new EventHandler(syncing_LayoutUpdated);
            syncing.Show();
            
        }

        void syncing_LayoutUpdated(object sender, EventArgs e)
        {
            syncing.LayoutUpdated -= new EventHandler(syncing_LayoutUpdated);
            string relpath = "";
            DirectoryListing Listing = new DirectoryListing();
            Listing = this.LocalFileList.DataContext;

            //sets the amount of elements in the progress bar
            syncing.UploadProgressBar.Maximum = 10000;

            traveseDirectories(LocalPath, relpath, Listing);

            // update the synced files Window
            syncedFiles.GetFileListAsync(course.Key, authToken);
        }
                
        // solicites the server to create the directories and files in the tree
        private void traveseDirectories(string path, string relative, DirectoryListing Listing)
        {
            SyncCurrentDirectories(Listing, relative);                          // creates the directories
            int fileindex = 0;
            foreach (string file in Directory.EnumerateFiles(path))
            {
                FileRecieved = false;
                DateTime updated = Listing.Files[fileindex].LastModified;
                SyncCurrentFile(file, updated);                                 // tells server to create file
                fileindex++;
            }
            
            foreach (string directory in Directory.EnumerateDirectories(path))
            {
                relative = directory.Substring(LocalPath.Length + 1);
                Listing = BuildLocalDirectoryListing(directory);  // drops down a level
                traveseDirectories(directory, relative, Listing);               // recursive call
            } 
        }

        // prompts server to create all directories on the current level
        void SyncCurrentDirectories(DirectoryListing level, string relative)
        {
            if (level.Directories.Count > 0)
            {
                syncedFiles.PrepCurrentPathAsync(level, relative, course.Key, authToken);
            }
        }

        // Prompts the server to create the file
        void SyncCurrentFile(string file, DateTime updated)
        {
            //Server Relative path
            string relativepath = file.Substring(LocalPath.Length + 1);
            
            // updating the textblock with current file uploading Local relative path
            syncing.UploadingFile.Text = file.Substring(RootPath.Length + 1);
            syncing.UploadingFile.UpdateLayout();

            //update progres bar (ie add a tick)
            syncing.UploadProgressBar.Value++;
            syncing.UploadProgressBar.UpdateLayout();
            
            
            MemoryStream s = new MemoryStream();
            StreamWriter writer = new StreamWriter(s);
            writer.Write(file);
            writer.Flush();

            using (Stream stream = s)
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);

                syncedFiles.SyncFileAsync(relativepath, data, updated, course.Key, authToken);
            }
            // wait till the file is uploaded
            //while (!FileRecieved)
            //{
            //    //sleep
            //    TimeSpan waitTime = new TimeSpan(0, 0, 1);

            //    Thread.Sleep(waitTime);
            //}
        }

        // when one file is done being created it will set the bool to true
        void syncedFiles_SyncFileCompleted(object sender, SyncFileCompletedEventArgs e)
        {
            //boolean file sync complete
            FileRecieved = true;
            
        }
        
        public DirectoryListing BuildLocalDirectoryListing(string path)
        {
            DirectoryListing listing = new DirectoryListing();
            listing.Directories = new ObservableCollection<DirectoryListing>();
            listing.Files = new ObservableCollection<FileListing>();
            listing.Name = path;

            //The "..." always goes on top
            listing.Directories.Add(new ParentDirectoryListing() { Name = "..." });

            //apparently, some folders are restricted.  Use a try block to catch
            //write exceptions
            try
            {
                //add files first
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    FileListing fileListing = new FileListing();
                    fileListing.LastModified = File.GetLastWriteTime(file);
                    fileListing.Name = Path.GetFileName(file);
                    listing.Files.Add(fileListing);
                }

                //add other directories
                foreach (string folder in Directory.EnumerateDirectories(path))
                {
                    DirectoryListing dList = new DirectoryListing();
                    dList.Files = new ObservableCollection<FileListing>();
                    dList.Directories = new ObservableCollection<DirectoryListing>();
                    dList.LastModified = Directory.GetLastWriteTime(folder);
                    dList.Name = folder.Substring(folder.LastIndexOf('\\') + 1);
                    listing.Directories.Add(dList);
                }
            }
            catch (Exception ex)
            {
                //something went wrong, oh well (for now)
            }
            return listing;
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

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        void syncedFiles_GetFileListCompleted(object sender, GetFileListCompletedEventArgs e)
        {
            RemoteFileList.DataContext = e.Result;
            syncing.Close();
        }
    }
}
