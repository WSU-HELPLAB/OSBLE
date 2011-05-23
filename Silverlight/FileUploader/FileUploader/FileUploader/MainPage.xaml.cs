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
                DirectoryLabel Folder = new DirectoryLabel();
                Folder.DirName.Text = folder.Substring(folder.LastIndexOf('\\') + 1);
                BitmapImage bmp = new BitmapImage();
                bmp.UriSource = new Uri("/Images/folder.png", UriKind.Relative);
                Folder.DirImage.Source = bmp;
                filelist.Items.Add(Folder);
            }

            foreach (string file in Directory.EnumerateFiles(path))
            {
                DirectoryLabel File = new DirectoryLabel();
                File.DirName.Text = file.Substring(file.LastIndexOf('\\') + 1);
                //Folder.DirImage = 
                filelist.Items.Add(Name);
            }

            InitializeComponent();
        }

        

        private void LocalDirSelect(object sender, MouseButtonEventArgs e)
        {
            string selectedItem = this.LocalFileList.SelectedItem as string;
     
            if ( selectedItem == "...")
            {
                Stack<string> path_pieces = new Stack<string>(path.Split('\\'));
                path_pieces.Pop();
                path = String.Join("\\", path_pieces.ToArray().Reverse());
                dirList(path, LocalFileList);
            }
            else if (selectedItem[0] != '+')
            {
                path += "\\" + selectedItem;
                dirList(path, LocalFileList);
            }
            
        }

        private void RemoteDirSelect(object sender, MouseButtonEventArgs e)
        {
            string selectedItem = this.RemoteFileList.SelectedItem as string;

            if (selectedItem == "...")
            {
                Stack<string> path_pieces = new Stack<string>(path.Split('\\'));
                path_pieces.Pop();
                path = String.Join("\\", path_pieces.ToArray().Reverse());
                dirList(path, RemoteFileList);
            }
            else if (selectedItem[0] != '+')
            {
                path += "\\" + selectedItem;
                dirList(path, RemoteFileList);
            }

        }
    }
}
