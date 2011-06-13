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
        public string RootPath;

        //reference to our web service
        private UploaderWebServiceClient client = new UploaderWebServiceClient();

        // stores 
        private KeyValuePair<int, string> course;

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

            //listeners for our web service
            client.GetFileListCompleted += new EventHandler<GetFileListCompletedEventArgs>(GetFileListCompleted);
            client.GetValidUploadLocationsCompleted += new EventHandler<GetValidUploadLocationsCompletedEventArgs>(GetValidUploadLocationsCompleted);

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
            client.GetValidUploadLocationsAsync(authToken);
            
            //get the local files
            LocalFileList.DataContext = FileOperations.BuildLocalDirectoryListing(LocalPath);
        }

        void DownButton_Click(object sender, RoutedEventArgs e)
        {
            RemoteFileList.MoveSelectionDown();
            client.UpdateListingOrderAsync(RemoteFileList.DataContext, course.Key, authToken);
        }

        void GetFileListCompleted(object sender, GetFileListCompletedEventArgs e)
        {
            RemoteFileList.DataContext = e.Result;
        }

        void GetValidUploadLocationsCompleted(object sender, GetValidUploadLocationsCompletedEventArgs e)
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
            LocalPath = LocalPath.Substring(0, LocalPath.LastIndexOf('\\'));
            LocalFileList.DataContext = FileOperations.BuildLocalDirectoryListing(LocalPath);
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

        void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
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
            DirectoryListing listing = FileOperations.BuildLocalDirectoryListing(LocalPath, false, true);
            UploadingModal uploader = new UploadingModal();
            uploader.Listing = listing;
            uploader.CourseId = course.Key;
            uploader.AuthToken = authToken;
            uploader.BeginUpload();
            uploader.Show();
        }

        void UpButton_Click(object sender, RoutedEventArgs e)
        {
            RemoteFileList.MoveSelectionUp();
            client.UpdateListingOrderAsync(RemoteFileList.DataContext, course.Key, authToken);
        }

        void UploadLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //very hack-ish at the moment, may need to revise
            course = (KeyValuePair<int, string>)UploadLocation.SelectedValue;
            client.GetFileListAsync(course.Key, authToken);
        }
    }
}
