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

namespace FileUploader.Controls
{
    public partial class FileListItem : ListBoxItem
    {/*
        public AbstractListing DataContext
        {
            get;
            set;
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
      * */

        public FileListItem()
        {
            InitializeComponent();
        }
    }
}
