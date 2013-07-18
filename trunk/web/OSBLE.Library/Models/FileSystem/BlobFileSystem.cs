using System;
using System.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace OSBLE.Models.FileSystem
{
    public static class BlobFileSystem
    {
        public static void addFile(string path, Stream file)
        {
            ConnectionStringSettings mySetting = ConfigurationManager.ConnectionStrings["StorageConnectionString"];
            string connection = mySetting.ToString();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connection);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

            if (file != null)
            {
                //Save file stream to Blob Storage
                CloudBlockBlob blob = cloudBlobClient.GetContainerReference("filesystem").GetBlockBlobReference(path);
                blob.UploadFromStream(file);
            }
            else
            {

            }

        }
    }
}
