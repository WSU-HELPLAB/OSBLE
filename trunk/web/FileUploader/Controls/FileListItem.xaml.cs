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
using System.Windows.Media.Imaging;

using FileUploader.OsbleServices;
namespace FileUploader.Controls
{
    public enum LabelImage { File, Folder };

    public partial class FileListItem : ListBoxItem
    {
        public AbstractListing DataContext
        {
            get;
            set;
        }

        public FileListItem()
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

        /// <summary>
        /// Converts from the LabelImage enum to an actual bitmap
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public BitmapImage BitmapFromLabel(LabelImage label)
        {
            string imagePath = "";
            switch (label)
            {
                case LabelImage.File:
                    imagePath = "/Images/file.png";
                    break;

                case LabelImage.Folder:
                    imagePath = "/Images/folder.png";
                    break;

                default:
                    imagePath = "/Images/file.png";
                    break;
            }
            BitmapImage bmp = new BitmapImage();
            bmp.UriSource = new Uri(imagePath, UriKind.Relative);
            return bmp;
        }

        private LabelImage image = LabelImage.Folder;
        public LabelImage Image
        {
            get
            {
                return image;
            }
            set
            {
                image = value;
                this.DirImage.Source = BitmapFromLabel(image);
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is FileListItem)
            {
                FileListItem other = obj as FileListItem;
                return FileName.CompareTo(other.FileName);
            }
            else
            {
                return -1;
            }
        }


    }
}
