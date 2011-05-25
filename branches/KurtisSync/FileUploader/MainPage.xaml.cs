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
using System.Windows.Shapes;
using System.IO;
using System.Windows.Media.Imaging;
using FileUploader.OsbleServices;


namespace FileUploader
{
    
    public partial class MainPage : UserControl
    {
        // Local Directory path
       string localpath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
       string localhome = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        // Synced Directory path
       FileSyncServiceClient syncedFiles = new FileSyncServiceClient();

       string syncpath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
       string syncpathhome = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        // Double Click variables
       string lastTarget = "";
       bool wasClicked = false;
 
        public MainPage()
        {
            InitializeComponent();
         
            // Populate the ListBox's LocalFileList and SyncedFileList with the contents of the directory path
            dirList(localpath, LocalFileList);
            dirList(syncpath, SyncedFileList);
        
        }

        private void dirList(string path, ListBox filelist)
        {
            filelist.Items.Clear();
            //If you have entered a subdirectory of home (Back button so to speak)
            if ((path != localhome && path == localpath)  || (path != syncpathhome && path == syncpath))
            {
                filelist.Items.Add("..");
            }

            // Assigns all the directories to the ListBox
            foreach (string folder in Directory.EnumerateDirectories(path))
            {
                DirectoryLabel label = new DirectoryLabel();
                label.DirName.Text = folder.Substring(folder.LastIndexOf('\\') + 1);
                label.Image = LabelImage.Folder;
                filelist.Items.Add(label);
            }
             
            // Assigns all the files to the ListBox
            foreach (string file in Directory.EnumerateFiles(path))
            {
                DirectoryLabel label = new DirectoryLabel();
                label.DirName.Text = file.Substring(file.LastIndexOf('\\') + 1);
                label.Image = LabelImage.File;
                filelist.Items.Add(label);
            }

            InitializeComponent();
        }


        private void LocalDirSelect(object sender, MouseButtonEventArgs e)
        {
            //If selected is a file or directory
            if (this.LocalFileList.SelectedItem is DirectoryLabel)
            {
                DirectoryLabel selected = this.LocalFileList.SelectedItem as DirectoryLabel;
                // if selected is a directory
                if (selected.Image != LabelImage.File)
                {
                    if (wasClicked == true)
                    {
                        if (selected.DirName.Text == lastTarget)
                        {
                            // appends the selected directory to the current path to open directory
                            localpath += "\\" + selected.DirName.Text;
                            dirList(localpath, LocalFileList);
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
                        dirList(localpath, LocalFileList);
                    }
                }
            }
          
            // If it wasn't a directory reset the click variables
            lastTarget = "";
            wasClicked = false;
        }

        private void SyncDirSelect(object sender, MouseButtonEventArgs e)
        {
            if(this.SyncedFileList.SelectedItem is DirectoryLabel)
            {
                DirectoryLabel selected = this.SyncedFileList.SelectedItem as DirectoryLabel;
                if (selected.Image != LabelImage.File)
                {
                    if (wasClicked == true)
                    {
                        if (selected.DirName.Text == lastTarget)
                        {
                            syncpath += "\\" + selected.DirName.Text;
                            dirList(syncpath, SyncedFileList);
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
            else
            {
                if (this.SyncedFileList.SelectedItem != null)
                {
                    string selected = this.SyncedFileList.SelectedItem.ToString();
                    if (selected == "..")
                    {
                        Stack<string> path_pieces = new Stack<string>(syncpath.Split('\\'));
                        path_pieces.Pop();
                        syncpath = String.Join("\\", path_pieces.ToArray().Reverse());
                        dirList(syncpath, SyncedFileList);
                    }
                }
            }
            
            // If it wasn't a directory reset the click variables
            lastTarget = "";
            wasClicked = false;
        }

        private void MoveUpbtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.SyncedFileList.SelectedItem is ListBoxItem)
            {
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            syncedFiles.SyncFileCompleted += syncedFiles_SyncCompleted;

            syncedFiles.GetFileListCompleted += syncedFiles_GetFileListCompleted;
            syncedFiles.GetFileListAsync();
        }

        private void syncedFiles_GetFileListCompleted(object sender, GetFileListCompletedEventArgs e)
        {
            try
            {
                SyncedFileList.ItemsSource = e.Result;
            }
            catch
            {
                lblStatus.Text = "Error contacting web service.";
            }
        }

        private void traveseDirectories(string path)
        {
            foreach (string folder in Directory.EnumerateDirectories(path))
            {
                // does directory exist in sync already
                // 
               
                syncedFiles.createDirAsync(folder);
                traveseDirectories(folder);

            }

            foreach (string file in Directory.EnumerateFiles(path))
            {
                try
                {
                    MemoryStream s = new MemoryStream();
                    StreamWriter writer = new StreamWriter(s);
                    writer.Write(file);
                    writer.Flush();

                    //byte[] byteArray = Encoding.ASCII.GetBytes(file);
                    //Stream filestream = new Stream(byteArray);
                    using (Stream stream = s)
                    {
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, (int)stream.Length);

                        syncedFiles.SyncFileAsync(file, data);
                        lblStatus.Text = "Sync started";
                    }
                }
                catch
                {
                    lblStatus.Text = "Error reading file.";
                }
            }

        }


        private void Syncbtn_Click(object sender, RoutedEventArgs e)
        {
            string path = localpath;
            
            traveseDirectories(path);

        }

        private void syncedFiles_SyncCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                lblStatus.Text = "Sync succeeded.";

                // refresh the file list
                syncedFiles.GetFileListAsync();
           } 
            else
            {
                lblStatus.Text = "Upload failed.";
            }
        }
    }
}
