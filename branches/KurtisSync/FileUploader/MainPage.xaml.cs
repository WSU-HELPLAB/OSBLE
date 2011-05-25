using System;
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
       string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
       string home = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Synced Directory path
       private FileSyncServiceClient syncedFiles = new FileSyncServiceClient();
       string syncpath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
       string syncpathhome = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
       

        // Double Click variables
       string lastTarget = "";
       bool wasClicked = false;
 
        public MainPage()
        {
            InitializeComponent();
         
            // Populate the ListBox's LocalFileList and SyncedFileList with the contents of the directory path
            dirList(path, LocalFileList);
            dirList(syncpath, SyncedFileList);
        
        }

        private void dirList(string path, ListBox filelist)
        {
            filelist.Items.Clear();
            //If you have entered a subdirectory of home (Back button so to speak)
            if (path != home)
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
                            path += "\\" + selected.DirName.Text;
                            dirList(path, LocalFileList);
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
                string selected = this.LocalFileList.SelectedItem.ToString();
                if (selected == "..")
                {
                    Stack<string> path_pieces = new Stack<string>(path.Split('\\'));
                    path_pieces.Pop();
                    path = String.Join("\\", path_pieces.ToArray().Reverse());
                    dirList(path, LocalFileList);
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
                string selected = this.SyncedFileList.SelectedItem.ToString();
                if (selected == "..")
                {
                    Stack<string> path_pieces = new Stack<string>(syncpath.Split('\\'));
                    path_pieces.Pop();
                    syncpath = String.Join("\\", path_pieces.ToArray().Reverse());
                    dirList(syncpath, SyncedFileList);
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

        private void Syncbtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            
            if (openDialog.ShowDialog() == true)
            {
                
                try
                {
                    using (Stream stream = openDialog.File.OpenRead())
                    {
                        //dont all really big files (more than 5mb).
                        if (stream.Length < 5120000)
                        {
                            byte[] data = new byte[stream.Length];
                            stream.Read(data, 0, (int)stream.Length);

                            syncedFiles.SyncFileAsync(openDialog.File.Name, data);
                            lblStatus.Text = "Sync started";
                        }
                        else
                        {
                            lblStatus.Text = "Files must be less than 5mb.";
                        }
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
