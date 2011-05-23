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

namespace FileUploader
{
    
    public partial class MainPage : UserControl
    {
       string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
       string home = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

       string lastTarget = "";
       bool wasClicked = false;
 
        public MainPage()
        {
            InitializeComponent();
         
            dirList(path, LocalFileList);
            dirList(path, RemoteFileList);
        
        }

        private void dirList(string path, ListBox filelist)
        {
            filelist.Items.Clear();
            if (path != home)
            {
                filelist.Items.Add("...");
            }

            foreach (string folder in Directory.EnumerateDirectories(path))
            {
                DirectoryLabel label = new DirectoryLabel();
                label.DirName.Text = folder.Substring(folder.LastIndexOf('\\') + 1);
                label.Image = LabelImage.Folder;
                filelist.Items.Add(label);
            }

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
            DirectoryLabel selected = this.LocalFileList.SelectedItem as DirectoryLabel;
            if (selected != null && selected.Image != LabelImage.File)
            {
                if (wasClicked == true)
                {
                    if (selected.DirName.Text == lastTarget)
                    {
                        path += "\\" + selected.DirName.Text;
                        dirList(path, LocalFileList);
                    }
                }
                else
                {
                    wasClicked = true;
                }
                lastTarget = selected.DirName.Text;

            }
            else if (this.LocalFileList.SelectedItem.ToString() == "...")
            {
                Stack<string> path_pieces = new Stack<string>(path.Split('\\'));
                path_pieces.Pop();
                path = String.Join("\\", path_pieces.ToArray().Reverse());
                dirList(path, LocalFileList);
                lastTarget = "";
                wasClicked = false;
            }
            else
            {
                lastTarget = "";
                wasClicked = false;
            }

            
          
            
        }

        private void RemoteDirSelect(object sender, MouseButtonEventArgs e)
        {
            DirectoryLabel selected = this.RemoteFileList.SelectedItem as DirectoryLabel;
            if (selected != null && selected.Image != LabelImage.File)
            {
                path += "\\" + selected.DirName.Text;
                dirList(path, RemoteFileList);
            }
            else if (this.RemoteFileList.SelectedItem.ToString() == "...")
            {
                Stack<string> path_pieces = new Stack<string>(path.Split('\\'));
                path_pieces.Pop();
                path = String.Join("\\", path_pieces.ToArray().Reverse());
                dirList(path, RemoteFileList);
            }

        }

        private void Syncbtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
