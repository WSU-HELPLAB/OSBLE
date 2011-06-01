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

using FileUploader.OsbleServices;
namespace FileUploader.Controls
{
    public partial class FileBrowserControl : UserControl
    {
        /// <summary>
        /// Whether or not the 
        /// </summary>
        public bool IsLocalFileList
        {
            get;
            set;
        }

        public FileBrowserControl()
        {
            InitializeComponent();
        }

    }
}
