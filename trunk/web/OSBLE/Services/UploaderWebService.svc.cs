using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Web;

using OSBLE.Models.Services.Uploader;

namespace OSBLE.Services
{
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class UploaderWebService
    {
        public string filePath;
        public string currentpath;

        public UploaderWebService()
        {
            filePath = HttpContext.Current.Server.MapPath("Files");
            currentpath = filePath;
        }

        /// <summary>
        /// A hack-ish way to get clients to recognize the FileListing class
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        public FileListing GetFakeFileListing()
        {
            return new FileListing();
        }

        /// <summary>
        /// A hack-ish way to get clients to recognize the DirectoryListing class
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        public DirectoryListing GetFakeDirectoryListing()
        {
            return new DirectoryListing();
        }

        /// <summary>
        /// Returns a list of files and directories for the given path
        /// </summary>
        /// <param name="relativepath"></param>
        /// <returns></returns>
        [OperationContract]
        public DirectoryListing GetFileList(string relativepath) //IEnumerable<AbstractListing> GetFileList(string relativepath)
        {

            //build a new listing, set some initial values
            DirectoryListing listing = new DirectoryListing();
            string currentpath = Path.Combine(filePath, relativepath);
            listing.Name = relativepath;
            listing.LastModified = File.GetLastWriteTime(currentpath);

            //handle files
            foreach (string file in Directory.GetFiles(currentpath))
            {
                FileListing fList = new FileListing();
                fList.Name = Path.GetFileName(file);
                fList.LastModified = File.GetLastWriteTime(file);
                listing.Files.Add(fList);
            }

            //handle other directories
            foreach (string folder in Directory.EnumerateDirectories(currentpath))
            {
                //recursively build the directory's subcontents.  Note that we have
                //to pass only the folder's name and not the complete path
                listing.Directories.Add(GetFileList(folder.Substring(folder.LastIndexOf('\\') + 1)));
            }

            //return the completed listing
            return listing;
        }

        [OperationContract]
        public string GetFileUrl(string fileName)
        {
            //probably need to make sure that this string is web-accessible, but this is fine for testing
            string file = Path.Combine(filePath, Path.GetFileName(fileName));
            return file;
        }

        [OperationContract]
        public void SyncFile(string fileName, byte[] data)
        {
            //uploads need to handle a check for lastmodified date
            string file = Path.Combine(filePath, fileName);
            using (FileStream fs = new FileStream(file, FileMode.Create))
            {
                fs.Write(data, 0, (int)data.Length);
            }
        }

        [OperationContract]
        public void createDir(string folderName)
        {
            // might need to check if the directory already exists
            string file = Path.Combine(filePath, folderName);
            Directory.CreateDirectory(file);
        }
    }
}
