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

        // Double Click variables
       string lastTarget = "";
       bool wasClicked = false;
 
        public MainPage()
        {
            InitializeComponent();
         
            // Populate the ListBox's LocalFileList and SyncedFileList with the contents of the directory path
            dirList(path, LocalFileList);
            dirList(path, SyncedFileList);
        
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
            if ( this.LocalFileList.SelectedItem is DirectoryLabel)
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
            else if (this.LocalFileList.SelectedItem.ToString() == "..")
            {
                Stack<string> path_pieces = new Stack<string>(path.Split('\\'));
                path_pieces.Pop();
                path = String.Join("\\", path_pieces.ToArray().Reverse());
                dirList(path, LocalFileList);
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
                            path += "\\" + selected.DirName.Text;
                            //dirList(path, SyncedFileList);
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
            else if (this.SyncedFileList.SelectedItem.ToString() == "..")
            {
                Stack<string> path_pieces = new Stack<string>(path.Split('\\'));
                path_pieces.Pop();
                path = String.Join("\\", path_pieces.ToArray().Reverse());
                //dirList(path, SyncedFileList);
            }

            // If it wasn't a directory reset the click variables
            lastTarget = "";
            wasClicked = false;
        }

        private void Syncbtn_Click(object sender, RoutedEventArgs e)
        {

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
    }
}
