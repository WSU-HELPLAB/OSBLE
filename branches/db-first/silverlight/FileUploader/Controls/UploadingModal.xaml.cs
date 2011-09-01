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

        /// <summary>
        /// Gets or sets the listing of files to be uploaded
        /// </summary>
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

        /// <summary>
        /// Gets or sets the token needed to make service requests
        /// </summary>
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

        /// <summary>
        /// Where we should start uploading files.  For example, if we want to upload the file
        /// "foobar.txt" to the folder "foo/bar", this would be "foo/bar"
        /// </summary>
        public string RelativePath
        {
            get
            {
                return uploader.RelativePath;
            }
            set
            {
                uploader.RelativePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the ID of the course whose documents we're syncing
        /// </summary>
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

        /// <summary>
        /// Constructor method
        /// </summary>
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

        /// <summary>
        /// Called automatically when the user tries to close the window.  In this case,
        /// make sure that we stop uploading files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UploadingModal_Closed(object sender, EventArgs e)
        {
            //make sure that we stop uploading
            uploader.RequestCancel();
        }

        /// <summary>
        /// Handles error messages received from our uploader thread.  Right now, pretty basic.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void uploader_Failed(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                UploadingFile.Text = "An error was encountered.  Sync failed.";
            });
        }

        /// <summary>
        /// This event is raised when the uploader thread finishes its canceling process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Starts the uploading process.  Be sure to set Listing, AuthToken, and CourseId
        /// before calling, or you'll have a very short upload!
        /// </summary>
        public void BeginUpload()
        {
            OkButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            uploader.Start();
        }

        /// <summary>
        /// Called whenever the user clicks the "cancel" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            uploader.RequestCancel();
        }

        /// <summary>
        /// Listens for the beginning of a new file upload.  Used to update
        /// the user on what file is being uploaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Call to close the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        /// <summary>
        /// Called when all files have been sent to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

