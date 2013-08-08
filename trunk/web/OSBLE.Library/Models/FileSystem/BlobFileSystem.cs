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
        /// <summary>
        /// Upload a blob with no attributes
        /// </summary>
        /// <param name="path"></param>
        /// <param name="file"></param>
        /// <param name="name"></param>
        public static void UploadFile(string path, Stream file, string name)
        {
            if (file != null)
            {
                //Save file stream to Blob Storage
                CloudBlockBlob blob = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);

                if (BlobFileSystem.Exists(blob))
                {
                    blob.Delete();
                    blob = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);
                    blob.UploadFromStream(file);
                    blob.Metadata["UploadTime"] = DateTime.UtcNow.ToString();
                    blob.Metadata["FileName"] = name;
                    blob.Metadata["Link"] = blob.Uri.ToString();
                    blob.SetMetadata();
                }
                else
                {
                    blob.UploadFromStream(file);
                    blob.Metadata["UploadTime"] = DateTime.UtcNow.ToString();
                    blob.Metadata["FileName"] = name;
                    blob.Metadata["Link"] = blob.Uri.ToString();
                    blob.SetMetadata();
                }
            }
            else
            {

            }
        }

        /// <summary>
        /// Upload a blob file with a attributes
        /// </summary>
        /// <param name="path"></param>
        /// <param name="file"></param>
        /// <param name="name"></param>
        /// <param name="sys"></param>
        /// <param name="user"></param>
        public static void UploadFile(string path, Stream file, string name, Dictionary<string, string> sys, Dictionary<string, string> user)
        {
            if (file != null)
            {
                //Save file stream to Blob Storage
                CloudBlockBlob blob = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);

                //Replace the blob if it exists
                if (BlobFileSystem.Exists(blob))
                {
                    blob.Delete();
                    blob = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);
                    blob.UploadFromStream(file);
                    blob.Metadata["FileName"] = name;
                    blob.Metadata["UploadTime"] = DateTime.UtcNow.ToString();
                    
                    //Add the blob attributes
                    foreach (KeyValuePair<string, string> pair in sys)
                    {
                        if (pair.Value != null)
                        {
                            blob.Metadata[pair.Key] = pair.Value;
                        }
                    }
                    foreach (KeyValuePair<string, string> pair in user)
                    {
                        if (pair.Value != null)
                        {
                            blob.Metadata[pair.Key] = pair.Value;
                        }
                    }

                    blob.Metadata["Link"] = blob.Uri.ToString();
                    blob.SetMetadata();
                }
                else
                {
                    blob.UploadFromStream(file);
                    blob.Metadata["FileName"] = name;
                    blob.Metadata["UploadTime"] = DateTime.UtcNow.ToString();

                    //Add the blob attributes
                    if (sys != null)
                    {
                        foreach (KeyValuePair<string, string> pair in sys)
                        {
                            if (pair.Value != null)
                            {
                                blob.Metadata[pair.Key] = pair.Value.ToString();
                            }
                        }
                    }
                    if (user != null)
                    {
                        foreach (KeyValuePair<string, string> pair in user)
                        {
                            if (pair.Value != null)
                            {
                                blob.Metadata[pair.Key] = pair.Value.ToString();
                            }
                        }
                    }

                    blob.Metadata["Link"] = blob.Uri.ToString();
                    blob.SetMetadata();
                }
            }
            //If the file stream is empty
            else
            {

            }
        }

        /// <summary>
        /// Fix the path name from a long form that is relevant to the local server location
        /// to the blob form path based on how files are saved in the blob filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string FixPath(string path)
        {
            string Drop = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            string Path = path.Replace(Drop, "");
            Path = Path.Replace("//", "/");
            Path = Path.Replace("\\", "/");
            return Path;
        }

        /// <summary>
        /// Get the blob client that is needed for accessing the blob storage
        /// </summary>
        /// <returns></returns>
        public static CloudBlobClient GetBlobClient()
        {
            ConnectionStringSettings mySetting = ConfigurationManager.ConnectionStrings["StorageConnectionString"];
            string connection = mySetting.ToString();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connection);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            return cloudBlobClient;
        }

        /// <summary>
        /// Check to see if a blob directory exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the prefix of a directory
        /// </summary>
        /// <param name="Dir"></param>
        /// <returns></returns>
        public static string GetDirectoryPrefix(CloudBlobDirectory Dir)
        {
            string Uri = Dir.Uri.ToString();
            string[] UriList = Uri.Split('/');

            string Prefix = UriList.Last();
            if (Prefix == "/" || Prefix == "")
            {
                Prefix = UriList[UriList.Count() - 2];
            }

            return Prefix;       
        }

        /// <summary>
        /// Have to fix the Cloud Blob Directory uri for enumerating over it
        /// There cannot be any leading '/'
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string FixURIPath(string prefix, string path)
        {
            string finalUri = path + "/" + prefix;
            return finalUri;
        }

        /// <summary>
        /// Gets the directory listing for a specified directory
        /// Lists all files as well as directories
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check the existence of a blob
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        public static bool Exists(CloudBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a "folder" with an "invisible" file as a placeholder so there can be
        /// "empty" directories
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dirName"></param>
        public static void CreateFolder(string path, string dirName)
        {
            path += "dir.osble";
            //Save file stream to Blob Storage
            using (MemoryStream ms = new MemoryStream())
            {
                CloudBlockBlob blob = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);

                if (BlobFileSystem.Exists(blob))
                {
                    int i = 0;
                    string oldname = dirName + "(" + i + ")";
                    path = path.Replace(dirName, oldname);

                    while (BlobFileSystem.Exists(blob))
                    {
                        i++;
                        path = path.Replace(oldname, dirName + "(" + i + ")");
                        oldname = dirName + "(" + i + ")";
                        blob = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);
                    }

                    CloudBlockBlob blobRename = BlobFileSystem.GetBlobClient().GetContainerReference("filesystem").GetBlockBlobReference(path);
                    blobRename.UploadFromStream(ms);
                    blobRename.Metadata["FileName"] = "dir.osble";
                    dirName += "_Copy";
                    blobRename.Metadata["DirName"] = dirName;
                    blobRename.SetMetadata();
                }
                else
                {
                    blob.UploadFromStream(ms);
                    blob.Metadata["FileName"] = "dir.osble";
                    blob.Metadata["DirName"] = dirName;
                    blob.SetMetadata();
                }
            }
        }

        /// <summary>
        /// Get a blob container
        /// </summary>
        /// <returns></returns>
        public static CloudBlobContainer GetBlobContainer ()
        {
            return BlobFileSystem.GetBlobClient().GetContainerReference("filesystem");
        }

    }
}
