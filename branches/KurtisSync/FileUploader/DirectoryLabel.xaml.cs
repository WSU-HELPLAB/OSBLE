﻿using System;
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
using System.Windows.Media.Imaging;

namespace FileUploader
{
    public enum LabelImage { File, Folder };
    
    public partial class DirectoryLabel : ListBoxItem, IComparable
    {
        public DirectoryLabel()
        {
            InitializeComponent();
        }

        public string FileName
        {
            get
            {
                return this.DirName.Text;
            }
            set
            {
                this.DirName.Text = value;
            }
        }

        public DateTime LastModified
        {
            get;
            set;
        }

        private LabelImage image = new LabelImage();
        public LabelImage Image
        {
            get 
            {
                return image;
            }
            set
            {
                image = value;
                string imagePath = "/Images/file.png";

                //figure out the correct image to use based on the supplied image parameter
                if (value == LabelImage.File)
                {
                    imagePath = "/Images/file.png";
                }
                else if (value == LabelImage.Folder)
                {
                    imagePath = "/Images/folder.png";
                }

                //set the new image
                BitmapImage bmp = new BitmapImage();
                bmp.UriSource = new Uri(imagePath, UriKind.Relative);
                this.DirImage.Source = bmp;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is DirectoryLabel)
            {
                DirectoryLabel other = obj as DirectoryLabel;
                return FileName.CompareTo(other.FileName);
            }
            else
            {
                return -1;
            }
        }
    }
}
