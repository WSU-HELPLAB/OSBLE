using System;
using System.Text;
using System.Collections.Generic;
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

namespace FileUploader
{
    public partial class UploaderPage : Page
    {
        // Local Directory path
        string localpath = @"C:\Users\acarter\Dropbox\AdamCarter"; //Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string localhome = @"C:\Users\acarter\Dropbox\AdamCarter"; //Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        // Synced Proxy
        UploaderWebServiceClient syncedFiles = new UploaderWebServiceClient();

        // Double Click variables
        string lastTarget = "";
        bool wasClicked = false;

        public UploaderPage()
        {
            InitializeComponent();

            //listeners for our web service
            syncedFiles.GetFileListCompleted += syncedFiles_GetFileListCompleted;
            syncedFiles.SyncFileCompleted += syncedFiles_SyncCompleted;

            // Populate the ListBox with the contents of the directory path
            dirList(localpath);
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void dirList(string path)
        {

            LocalFileList.Items.Clear();
            SyncedFileList.Items.Clear();

            //If you have entered a subdirectory of home (Back button so to speak)
            if ((path != localhome && path == localpath))
            {
                LocalFileList.Items.Add("..");
            }

            // Assigns all the directories to the ListBox
            foreach (string folder in Directory.EnumerateDirectories(path))
            {
                FileListItem label = new FileListItem();
                label.FileName = folder.Substring(folder.LastIndexOf('\\') + 1);
                label.Image = LabelImage.Folder;
                //label.LastModified = File.GetLastWriteTime(folder);
                LocalFileList.Items.Add(label);
            }

            // Assigns all the files to the ListBox
            foreach (string file in Directory.EnumerateFiles(path))
            {
                FileListItem label = new FileListItem();
                label.FileName = file.Substring(file.LastIndexOf('\\') + 1);
                label.Image = LabelImage.File;
                //label.LastModified = File.GetLastWriteTime(file);
                LocalFileList.Items.Add(label);
            }

            // gets the synced directories contents
            if (localpath == localhome)
            {
                syncedFiles.GetFileListAsync("");
            }
            else
            {
                string d = localpath.Substring(localhome.Length + 1);
                syncedFiles.GetFileListAsync(d);
            }
        }

        private void LocalDirSelect(object sender, MouseButtonEventArgs e)
        {
            //If selected is a file or directory
            if (this.LocalFileList.SelectedItem is FileListItem)
            {
                FileListItem selected = this.LocalFileList.SelectedItem as FileListItem;
                // if selected is a directory
                if (selected.Image != LabelImage.File)
                {
                    if (wasClicked == true)
                    {
                        if (selected.FileName == lastTarget)
                        {
                            // appends the selected directory to the current path to open directory
                            localpath += "\\" + selected.FileName;
                            dirList(localpath);
                        }
                    }
                    else
                    {
                        wasClicked = true;
                    }
                }
                lastTarget = selected.FileName;

                // If it was a directory return out
                return;
            }
            else
            {
                if (this.LocalFileList.SelectedItem != null)
                {
                    string selected = this.LocalFileList.SelectedItem.ToString();
                    if (selected == "..")
                    {
                        Stack<string> path_pieces = new Stack<string>(localpath.Split('\\'));
                        path_pieces.Pop();
                        localpath = String.Join("\\", path_pieces.ToArray().Reverse());
                        dirList(localpath);
                    }
                }
            }

            // If it wasn't a directory reset the click variables
            lastTarget = "";
            wasClicked = false;
        }

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

                */
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

    }
}
