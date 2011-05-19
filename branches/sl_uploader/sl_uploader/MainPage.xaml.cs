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
using sl_uploader.OsbleServices;

namespace sl_uploader
{
    public partial class MainPage : UserControl
    {
        private UploaderFileServiceClient uploaderClient = new UploaderFileServiceClient();
        private IEnumerable<OsbleFileInfo> serverFileList = new List<OsbleFileInfo>();
        public MainPage()
        {
            InitializeComponent();

            //set up our event handlers
            uploaderClient.GetFileListCompleted += new EventHandler<GetFileListCompletedEventArgs>(uploaderClient_GetFileListCompleted);
            uploaderClient.UploadFileCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(uploaderClient_UploadFileCompleted);

            //get the most up to date listing of files on the server
            uploaderClient.GetFileListAsync();
        }

        void uploaderClient_GetFileListCompleted(object sender, GetFileListCompletedEventArgs e)
        {
            openFileButton.IsEnabled = true;
            serverFileList = e.Result;
        }

        void uploaderClient_UploadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            
        }

        private void openFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "All Files (*.*)|*.*";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                //disable the upload button
                openFileButton.IsEnabled = false;

                foreach (FileInfo file in dialog.Files)
                {
                    bool needsUpload = false;

                    //see if we already have a copy of the file on the server
                    var result = from item in serverFileList where item.FileName == file.Name select item;
                    if (result.Count() > 0)
                    {
                        OsbleFileInfo serverFile = result.First();

                        //see if the local copy is newer than the server copy
                        MessageBox.Show(file.LastAccessTime.ToShortDateString());
                        /*
                        if (file.LastAccessTime > serverFile.LastModified)
                        {
                            needsUpload = true;
                        }
                         * */
                    }
                    else
                    {
                        needsUpload = true;
                    }

                    //continue if we need to upload
                    if (needsUpload)
                    {
                        //open the current file for reading
                        using (Stream stream = file.OpenRead())
                        {
                            //current cap at 5MB, will probably need to increaes to 10MB or so.
                            //According to the book, the cap needs to be increased in the web.config
                            //as well.
                            if (stream.Length < 5120000)
                            {
                                byte[] data = new byte[stream.Length];
                                stream.Read(data, 0, (int)stream.Length);
                                uploaderClient.UploadFileAsync(file.Name, data);
                            }
                        }
                    }
                }

                //refresh the server file list
                uploaderClient.GetFileListAsync();
            }
        }
    }
}
