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
        private AbstractListing dataContext;
        public AbstractListing DataContext
        {
            get
            {
                return dataContext;
            }
            set
            {
                dataContext = value;
                this.DirName.Text = dataContext.Name;
                BitmapImage bmp;
                if (dataContext is DirectoryListing)
                {
                    bmp = BitmapFromLabel(LabelImage.Folder);
                }
                else
                {
                    bmp = BitmapFromLabel(LabelImage.File);
                }
                this.DirImage.Source = bmp;
            }
        }

        public FileListItem()
        {
            InitializeComponent();
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
    }
}
