using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using FileUploader.OsbleServices;
using System.Threading;
using System.IO;

namespace FileUploader
{
    public class UploaderThread : ThreadWrapperBase
    {
        public EventHandler<FileUploadBegineArgs> FileUploadBegin = delegate { };
        public EventHandler UploadComplete = delegate { };

        private UploaderWebServiceClient client = new UploaderWebServiceClient();

        private DirectoryListing uploadListing = new DirectoryListing() { Directories = new System.Collections.ObjectModel.ObservableCollection<DirectoryListing>(), Files = new System.Collections.ObjectModel.ObservableCollection<FileListing>()};
        public DirectoryListing Listing
        {
            get
            {
                return uploadListing;
            }
            set
            {
                uploadListing = value;
            }
        }

        public string AuthToken
        {
            get;
            set;
        }

        public int CourseId
        {
            get;
            set;
        }

        public int NumberOfUploadsCompleted
        {
            get;
            set;
        }

        public int NumberOfUploads
        {
            get
            {
                return GetFileCount(Listing) - 1;
            }
        }

        private bool fileSyncCompleted = false;
        private bool fileLastModifiedCompleted = false;
        private DateTime fileLastModified;

        public UploaderThread()
        {
            client.PrepCurrentPathCompleted += new EventHandler<PrepCurrentPathCompletedEventArgs>(client_PrepCurrentPathCompleted);
            client.SyncFileCompleted += new EventHandler<SyncFileCompletedEventArgs>(client_SyncFileCompleted);
            client.GetLastModifiedDateCompleted += new EventHandler<GetLastModifiedDateCompletedEventArgs>(client_GetLastModifiedDateCompleted);
            client.IsValidKeyCompleted += new EventHandler<IsValidKeyCompletedEventArgs>(client_IsValidKeyCompleted);
        }

        /// <summary>
        /// Step 0: Start the process
        /// </summary>
        protected override void DoTask()
        {
            NumberOfUploadsCompleted = 0;
            client.IsValidKeyAsync(AuthToken);
        }

        /// <summary>
        /// Step 1: Validate key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void client_IsValidKeyCompleted(object sender, IsValidKeyCompletedEventArgs e)
        {
            //if we got a bad auth key, stop
            if (!e.Result)
            {
                OnFailed();
                return;
            }

            //give the web service a map of what we're planning to upload
            client.PrepCurrentPathAsync(Listing, CourseId, AuthToken);
        }

        /// <summary>
        /// Step 2: Prep file path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void client_PrepCurrentPathCompleted(object sender, PrepCurrentPathCompletedEventArgs e)
        {
            //begin directory sync
            SyncListing(Listing);

            //tell everyone that we're done
            OnCompleted();
        }        

        /// <summary>
        /// Step 3: Finally begin uploading files to the web service
        /// </summary>
        /// <param name="listing"></param>
        /// <param name="basePath"></param>
        private void SyncListing(DirectoryListing listing, string basePath = "")
        {
            //send over the files
            foreach (FileListing fl in listing.Files)
            {
                //reset our sync status
                fileSyncCompleted = false;
                fileLastModifiedCompleted = false;

                //check for a cancel request
                if (CancelRequested)
                {
                    return;
                }

                //tell interested parties that we're about to upload
                FileUploadBegin(this, new FileUploadBegineArgs(fl.AbsolutePath));

                //build the file name
                Stream stream = File.OpenRead(fl.AbsolutePath);
                string fileName = basePath + fl.Name;

                //check to see if the file needs to be updated
                client.GetLastModifiedDateAsync(fileName, CourseId, AuthToken);
                
                //wait for the call to complete
                while (!fileLastModifiedCompleted)
                {
                    Thread.Sleep(33);
                }

                //only send if newer
                if (fl.LastModified < fileLastModified)
                {
                    //update our status
                    NumberOfUploadsCompleted++;
                    continue;
                }


                //read into memory, prepare for sending
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);

                //send the file
                client.SyncFileAsync(fileName, data, CourseId, AuthToken);

                //always remember to close the stream
                stream.Close();

                //wait for the call to complete
                while (!fileSyncCompleted)
                {
                    Thread.Sleep(33);
                }

                //update our status
                NumberOfUploadsCompleted++;
            }

            foreach (DirectoryListing dl in listing.Directories)
            {
                //recursively send over directory files
                SyncListing(dl, basePath + dl.Name + "\\");
            }
        }

        protected override void OnCompleted()
        {
            UploadComplete(this, EventArgs.Empty);
        }

        /// <summary>
        /// Traverses the current Listing and determines the number of files present in the listing
        /// </summary>
        private int GetFileCount(DirectoryListing listing)
        {
            int count = 0;

            //try needed in order to catch null references
            try
            {
                count += listing.Files.Count;
                foreach (DirectoryListing dlisting in listing.Directories)
                {
                    count += GetFileCount(dlisting);
                }
            }
            catch (Exception ex)
            {
                // do nothing
            }
            return count;
        }

        void client_GetLastModifiedDateCompleted(object sender, GetLastModifiedDateCompletedEventArgs e)
        {
            fileLastModifiedCompleted = true;
            fileLastModified = e.Result;
        }

        void client_SyncFileCompleted(object sender, SyncFileCompletedEventArgs e)
        {
            fileSyncCompleted = true;
        }
    }

    public class FileUploadBegineArgs : EventArgs
    {
        public string FileToUpload
        {
            get;
            set;
        }
        public FileUploadBegineArgs(string fileToUpload)
        {
            FileToUpload = fileToUpload;
        }
    }

}
