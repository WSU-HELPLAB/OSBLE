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
using System.Windows.Data;
using FileUploader.OsbleServices;
using System.ComponentModel;

namespace FileUploader.Controls
{
    public partial class UploadingModal : ChildWindow
    {
        UploaderThread uploader = new UploaderThread();

        public DirectoryListing Listing
        {
            get
            {
                return uploader.Listing;
            }
            set
            {
                uploader.Listing = value;
                UploadProgressBar.Maximum = uploader.NumberOfUploads;
            }
        }

        public string AuthToken
        {
            get
            {
                return uploader.AuthToken;
            }
            set
            {
                uploader.AuthToken = value;
            }
        }

        public int CourseId
        {
            get
            {
                return uploader.CourseId;
            }
            set
            {
                uploader.CourseId = value;
            }
        }

        public UploadingModal()
        {
            InitializeComponent();

            //make sure that Listing is never a null reference
            Listing = new DirectoryListing();

            //event listeners
            uploader.UploadComplete += new EventHandler(UploadComplete);
            uploader.FileUploadBegin += new EventHandler<FileUploadBegineArgs>(FileUploadStart);
            uploader.Cancelled += new EventHandler(uploader_Cancelled);
            uploader.Failed += new EventHandler(uploader_Failed);
            OkButton.Click += new RoutedEventHandler(OkButton_Click);
            CancelButton.Click += new RoutedEventHandler(CancelButton_Click);
            this.Closed += new EventHandler(UploadingModal_Closed);

            UploadProgressBar.Maximum = uploader.NumberOfUploads;
        }

        void UploadingModal_Closed(object sender, EventArgs e)
        {
            //make sure that we stop uploading
            uploader.RequestCancel();
        }

        void uploader_Failed(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                UploadingFile.Text = "An error was encountered.  Sync failed.";
            });
        }

        void uploader_Cancelled(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                UploadingFile.Text = "Upload Canceled";
                OkButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
            }
            );
        }

        public void BeginUpload()
        {
            OkButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            uploader.Start();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            uploader.RequestCancel();
        }

        private void FileUploadStart(object sender, FileUploadBegineArgs e)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                string uploadName = "";
                if (e.FileToUpload.Length > 35)
                {
                    uploadName = "..." + e.FileToUpload.Substring(e.FileToUpload.Length - 35);
                }
                else
                {
                    uploadName = e.FileToUpload;
                }
                UploadingFile.Text = uploadName;
                UploadProgressBar.Value = (sender as UploaderThread).NumberOfUploadsCompleted;
            }
            );
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void UploadComplete(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                UploadingFile.Text = "Upload Complete";
                OkButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
            }
            );
        }
    }
}

