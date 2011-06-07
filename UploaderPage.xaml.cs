using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.IO;
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

        private string RootPath; 

        //reference to our web service
        UploaderWebServiceClient syncedFiles = new UploaderWebServiceClient();

        // Modal upload progress window
        UploadingModal uploading = new UploadingModal();

        // Creating course key for upload location combo box
        KeyValuePair<int, string> course;

        public UploaderPage(string authenticationToken)
        {

            InitializeComponent();

            authToken = authenticationToken;
            RootPath = LocalPath;

            //get our local path
            LocalPath =  GetLastLocalPath();

            //listeners for our web service
            syncedFiles.GetFileListCompleted += new EventHandler<GetFileListCompletedEventArgs>(syncedFiles_GetFileListCompleted);
            syncedFiles.SyncFileCompleted += new EventHandler<SyncFileCompletedEventArgs>(syncedFiles_SyncFileCompleted);
            syncedFiles.GetValidUploadLocationsCompleted += new EventHandler<GetValidUploadLocationsCompletedEventArgs>(syncedFiles_GetValidUploadLocationsCompleted);

            //local event listeners
            SyncButton.Click += new RoutedEventHandler(SyncButton_Click);
            LocalFileList.EmptyDirectoryEncountered += new EventHandler(LocalFileList_EmptyDirectoryEncountered);
            LocalFileList.ParentDirectoryRequest += new EventHandler(LocalFileList_ParentDirectoryRequest);
            LocalFileTextBox.KeyUp += new KeyEventHandler(LocalFileTextBox_KeyUp);
            UploadLocation.SelectionChanged += new SelectionChangedEventHandler(UploadLocation_SelectionChanged);

            //get the remote server file list
            syncedFiles.GetValidUploadLocationsAsync(authToken);
            
            //get the local files
            LocalFileList.DataContext = BuildLocalDirectoryListing(LocalPath);
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


        void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            uploading.Show();

            SyncCurrentDirectories(this.LocalFileList.DataContext);
            SyncCurrentFile(this.LocalFileList.DataContext, 0);
        }

        void SyncCurrentDirectories(DirectoryListing level)
        {
            if (level.Directories.Count > 0)
            {
                syncedFiles.PrepCurrentPathAsync(level, course, authToken);
            }
        }

        void SyncCurrentFile(DirectoryListing level, int fileindex)
        {
            string newFilePath;
            newFilePath = level.Files[fileindex].Name;//Path.Combine(LocalPath, level.Files[fileindex].Name);
            // updating the textblock with current file uploading
            uploading.UploadingFile.Text = newFilePath;

            //update progres bar (ie add a tick)

            MemoryStream s = new MemoryStream();
            StreamWriter writer = new StreamWriter(s);
            writer.Write(newFilePath);
            writer.Flush();

            using (Stream stream = s)
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);

                syncedFiles.SyncFileAsync(newFilePath, data, fileindex, course, authToken);
            }
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
        }

        void syncedFiles_SyncFileCompleted(object sender, SyncFileCompletedEventArgs e)
        {
            //start the next file
            if(this.LocalFileList.DataContext.Files.Count > e.Result)
                SyncCurrentFile(this.LocalFileList.DataContext, e.Result);
        }

        /*

        private void SyncDirSelect(object sender, MouseButtonEventArgs e)
        {
            if (this.SyncedFileList.SelectedItem is FileListItem)
            {
                FileListItem selected = this.SyncedFileList.SelectedItem as FileListItem;
                if (selected.FileName == "..")
                {
                    Stack<string> path_pieces = new Stack<string>(localpath.Split('\\'));
                    path_pieces.Pop();
                    localpath = String.Join("\\", path_pieces.ToArray().Reverse());
                    dirList(localpath);
                }
                else if (selected.Image != LabelImage.File)
                {
                    if (wasClicked == true)
                    {
                        if (selected.DirName.Text == lastTarget)
                        {

                            dirList(localpath);
                        }
                    }
                    else
                    {
                        wasClicked = true;
                    }
                }
                lastTarget = selected.DirName.Text;

                // If it was a directory return out
                return;
            }

            // If it wasn't a directory reset the click variables
            lastTarget = "";
            wasClicked = false;
        }

        private void MoveUpbtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.SyncedFileList.SelectedItem is ListBoxItem)
            {
                //Swaps the listbox items based on their index's

                int index = this.SyncedFileList.SelectedIndex;
                object Swap = this.SyncedFileList.SelectedItem;
                if (index > 0)
                {
                    this.SyncedFileList.Items.RemoveAt(index);
                    this.SyncedFileList.Items.Insert(index - 1, Swap);
                    this.SyncedFileList.SelectedItem = Swap;
                }
            }
        }

        private void MoveDownbtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.SyncedFileList.SelectedItem is ListBoxItem)
            {
                //Swaps the listbox items based on their index's

                if (this.SyncedFileList.SelectedItem is ListBoxItem)
                {
                    int index = this.SyncedFileList.SelectedIndex;
                    object Swap = this.SyncedFileList.SelectedItem;
                    if (index != -1 && index < this.SyncedFileList.Items.Count - 1)
                    {
                        this.SyncedFileList.Items.RemoveAt(index);
                        this.SyncedFileList.Items.Insert(index + 1, Swap);
                        this.SyncedFileList.SelectedItem = Swap;
                    }
                }
            }
        }

        private void syncedFiles_GetFileListCompleted(object sender, GetFileListCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                /*
                // Assigning the list generated from the server's folder to a listbox for display
                for (int i = 0; i < e.Result.Count; ++i)
                {
                    FileListItem temp = new FileListItem();
                    temp.FileName = e.Result[i].Name;
                    if (e.Result[i] is FileListing)
                        temp.Image = LabelImage.File;
                    else
                        temp.Image = LabelImage.Folder;
                    temp.LastModified = e.Result[i].LastModified;

                    SyncedFileList.Items.Add(temp);
                }

                
            }
            else
            {
                lblStatus.Text = "Error contacting web service.";
            }
        }

        // recursive function called from Syncbtn_Click it will recursivly go through all the directories and popluate the 
        // contained directories and files
        private void traveseDirectories(string path, string dir)
        {
            foreach (string folder in Directory.EnumerateDirectories(path))
            {
                dir = folder.Substring(localpath.Length + 1);

                // does directory exist in sync already

                syncedFiles.createDirAsync(dir);

                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    try
                    {
                        MemoryStream s = new MemoryStream();
                        StreamWriter writer = new StreamWriter(s);
                        writer.Write(file);
                        writer.Flush();

                        string filepath = file.Substring(localpath.Length + 1);

                        using (Stream stream = s)
                        {
                            byte[] data = new byte[stream.Length];
                            stream.Read(data, 0, (int)stream.Length);

                            syncedFiles.SyncFileAsync(filepath, data);
                            lblStatus.Text = "Sync started";
                        }
                    }
                    catch
                    {
                        lblStatus.Text = "Error reading file.";
                    }
                }
                traveseDirectories(folder, dir);
            }
        }


        private void Syncbtn_Click(object sender, RoutedEventArgs e)
        {
            string path = localpath;
            traveseDirectories(path, "");

            // Populates the files in the home directory after all the directories have been created
            foreach (string file in Directory.EnumerateFiles(path))
            {
                try
                {
                    MemoryStream s = new MemoryStream();
                    StreamWriter writer = new StreamWriter(s);
                    writer.Write(file);
                    writer.Flush();

                    string filepath = file.Substring(localpath.Length + 1);

                    using (Stream stream = s)
                    {
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, (int)stream.Length);

                        syncedFiles.SyncFileAsync(filepath, data);
                        lblStatus.Text = "Sync started";
                    }
                }
                catch
                {
                    lblStatus.Text = "Error reading file.";
                }
            }

        }

        private void syncedFiles_SyncCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                lblStatus.Text = "Sync succeeded.";

            }
            else
            {
                lblStatus.Text = "Upload failed.";
            }
        }
         * */

    }
}
