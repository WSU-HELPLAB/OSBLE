using System;
using System.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Web;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Courses;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Users;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.FileSystem
{
    public static class BlobFileSystem
    {
        public static void UploadFile(string path, Stream file, string name)
        {
            if (file != null)
            {
                //Save file stream to Blob Storage
                CloudBlockBlob blob = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);      
                blob.UploadFromStream(file);
                blob.Metadata["FileName"] = name;
                blob.Metadata["Link"] = blob.Uri.ToString();
                blob.SetMetadata();
            }
            else
            {

            }
        }

        public static string FixPath(string path)
        {
            string Drop = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            string Path = path.Replace(Drop, "");
            Path = Path.Replace("//", "/");
            Path = Path.Replace("\\", "/");
            return Path;
        }

        public static CloudBlobClient GetBlobClient()
        {
            ConnectionStringSettings mySetting = ConfigurationManager.ConnectionStrings["StorageConnectionString"];
            string connection = mySetting.ToString();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connection);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            return cloudBlobClient;
        }

        public static bool CheckDirExists(string path)
        {
            List<IListBlobItem> BlobList = new List<IListBlobItem>();
            CloudBlobDirectory directory =
                BlobFileSystem.GetBlobClient().GetBlobDirectoryReference(path);

            foreach (var blobItem in directory.ListBlobs())
            {
                return true;
            }

           return false;
        }

        //Get the prefix of a blob directory
        public static string GetDirectoryPrefix(CloudBlobDirectory Dir)
        {
            List<char> list = Dir.Uri.Segments.Last().ToList();
            string Prefix = "";

            for (int i = 0; i < (list.Count() - 1); i++)
            {
                Prefix += list[i];
            }
            return Prefix;       
        }

        //Have to fix the Cloud Blob Directory uri for enumerating over it
        //There cannot be any leading '/'
        public static string FixURIPath(string[] uri, CloudBlobDirectory dir)
        {
            string finalUri = "";
            for (int i = 0; i < uri.Count(); i++)
            {
                if (i == 0)
                {
                }
                else if (i == uri.Count())
                {
                    finalUri += BlobFileSystem.GetDirectoryPrefix(dir);
                }
                else
                {
                    finalUri += uri[i];
                }
            }
            return finalUri;
        }

        //Gets the directory listing for a specified directory
        //Lists all files as well as directories
        public static List<IListBlobItem>
          GetDirectoryList(string directoryName)
        {
            //Ensures the path starts with 'filesystem/' which is needed
            string truePath = "filesystem/";
            directoryName = directoryName.Replace("filesystem/", "");
            truePath += directoryName;

            List<IListBlobItem> BlobList = new List<IListBlobItem>();
            CloudBlobDirectory directory =
                BlobFileSystem.GetBlobClient().GetBlobDirectoryReference(truePath);
            
            foreach (var blobItem in directory.ListBlobs())
            {
                BlobList.Add(blobItem);
            }
            
            return BlobList;
        }

        //Creates a "folder" with an "invisible" file as a placeholder so there can be
        //"empty" directories
        public static void CreateFolder(string path, string dirName)
        {
            path += "dir.osble";
            //Save file stream to Blob Storage
            using (MemoryStream ms = new MemoryStream())
            {
                CloudBlockBlob blob = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);
                blob.UploadFromStream(ms);
                blob.Metadata["FileName"] = "dir.osble";
                blob.Metadata["DirName"] = dirName;
                blob.SetMetadata();
            }
        }

        //Copy blob callback
        public static void CopyBlobCallback(IAsyncResult result)
        {
            CloudBlob blobDest = (CloudBlob)result.AsyncState;

            // End the operation.
            blobDest.EndCopyFromBlob(result);
        }

        //Return the blob container reference
        public static CloudBlobContainer GetBlobContainer ()
        {
            return BlobFileSystem.GetBlobClient().GetContainerReference("filesystem");
        }

    }
}
