// Created 5-15-13 by Evan Olds for the OSBLE project at WSU

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Web.Mvc;
using System.Web;
using System.Xml;
using OSBLE.Models.Courses;
using Microsoft.WindowsAzure.StorageClient;
using Ionic.Zip;


namespace OSBLE.Models.FileSystem
{
    // Information about the two directories used in representing an OSBLEDirectory:
    // There are two folders associated with a collection of attributable files: data and attr
    // data:
    //   This folder contains the actual data files that were uploaded.
    //   Each file can have an accompanying XML file for attributes for this file, 
    //   but this file is in the attr folder.
    // attr:
    //   Contains XML files for file attributes. There should be a one-to-one correspondence 
    //   of data files to XML attribute files, with the exception of the possibility of a 
    //   data file that has no accompanying XML attribute file and thus implicitly has no 
    //   attributes.
    
    /// <summary>
    /// Represents a virtual directory of files and subdirectories in the OSBLE file system. 
    /// An OSBLE directory is usually designated for a specific purpose, see FileSystem.cs 
    /// for the details about what types of folders are in the file system hierarchy.
    /// </summary>
    public class OSBLEDirectory
    {
        private string m_attrDir;
        
        private string m_dataDir;

        protected string m_path;

        /// <summary>
        /// Constructs an OSBLEDirectory object from an existing folder on disk. The 
        /// specified folder must exist or else an exception will be thrown.
        /// </summary>
        public OSBLEDirectory(string dataPath)
        {
            m_dataDir = m_path = dataPath;
            m_attrDir = Path.Combine(
                Path.GetDirectoryName(dataPath),
                Path.GetFileName(dataPath) + "Attr");
        }
        
        public OSBLEDirectory(string dataPath, string attrPath)
        {
            m_dataDir = dataPath;
            m_attrDir = attrPath;
            m_path = dataPath;

            m_dataDir = BlobFileSystem.FixPath(m_dataDir);
            m_attrDir = BlobFileSystem.FixPath(m_attrDir);
            m_path = BlobFileSystem.FixPath(m_path); 
        }

        /// <summary>
        /// Adds a file to the directory and writes all the bytes in the 
        /// array to its contents.
        /// </summary>
        public bool AddFile(string fileName, byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(data, 0, data.Length);
            ms.Position = 0;
            return AddFile(fileName, ms);
        }

        /// <summary>
        /// Adds a file from the specified stream data. No user attributes are created 
        /// and a few auto-system attributes are added.
        /// </summary>
        public virtual bool AddFile(string fileName, Stream data)
        {
            string filenamePath = m_path;
            filenamePath += "/" + fileName;
            string remove = "filesystem/";
            filenamePath = filenamePath.Replace(remove, "");

            BlobFileSystem.UploadFile(filenamePath, data, fileName);
            return true;
        }

        /// <summary>
        /// Get the submission time of a assignment submission in a current
        /// OSBLE directory.
        /// </summary>
        /// <returns></returns>
        public DateTime? GetSubmissionTime()
        {
            DateTime? time = null;

            string pathToDir = m_path;
            pathToDir = pathToDir.Replace("filesystem/", "");
            string truePath = "filesystem/" + pathToDir + "/";

            CloudBlobContainer container = BlobFileSystem.GetBlobContainer();
            List<IListBlobItem> BlobDataFiles = BlobFileSystem.GetBlobClient().ListBlobsWithPrefix(truePath).ToList();
            int i = 0;
            foreach (CloudBlob blob in BlobDataFiles)
            {
                if (i == 0)
                {
                    time = blob.Properties.LastModifiedUtc;
                    i++;
                }
                else
                {
                    if(time < blob.Properties.LastModifiedUtc)
                    {
                        time = blob.Properties.LastModifiedUtc;
                    }
                }
                
            }

            if (time != null)
            {
                DateTime converted = DateTime.SpecifyKind(DateTime.Parse(time.ToString()), DateTimeKind.Utc);
                var kind = converted.Kind;
                time = converted.ToLocalTime();
            }

            return time;
        }

        /// <summary>
        /// Adds a file, along with its attributes. If the file already exists then it will 
        /// be overwritten and its attributes will be replaced.
        /// </summary>
        /// <param name="sysAttrs">Collection of system attributes to be associated with the 
        /// file. This can be null if desired to create the file with an empty list of 
        /// system attributes.</param>
        /// /// <param name="usrAttrs">Collection of user attributes to be associated with the 
        /// file. This can be null if desired to create the file with an empty list of 
        /// user attributes.</param>
        public bool AddFile(string fileName, Stream data,
            Dictionary<string, string> sysAttrs,
            Dictionary<string, string> usrAttrs)
        {
            // We don't allow subdirectories or absolute paths. We need just a file 
            // name with no slashes.
            if (fileName.Contains('\\') || fileName.Contains('/'))
            {
                return false;
            }
          
            bool retVal = true;

            string filenamePath = m_path;
            filenamePath += "/" + fileName;
            string remove = "filesystem/";
            filenamePath = filenamePath.Replace(remove, "");
            filenamePath = BlobFileSystem.FixPath(filenamePath);
           
            try
            {
                BlobFileSystem.UploadFile(filenamePath, data, fileName, sysAttrs, usrAttrs);
            }
            catch (Exception)
            {
                retVal = false;
            }

            // If we wrote the attribute data but not the file data then we want 
            // to delete the attribute file
            if (!retVal)
            {

            }

            return retVal;
        }

        /// <summary>
        /// Returns the path for a specific file in the current osble directory.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string DownloadFile(string fileName)
        {
            string filePath = m_path;
            fileName = fileName.Replace("//", "/");
            filePath += fileName;
            filePath = filePath.Replace("filesystem/", "");
            return filePath;
        }

        /// <summary>
        /// Get's this Osble directories blobs and downloads them as a zip.
        /// A directory's contents can specifically be provided to download as well.
        /// </summary>
        /// <returns></returns>
        public Stream DownloadDirectory()
        {
            string BlobDirectory = BlobFileSystem.FixPath(m_path);

            List<IListBlobItem> BlobDataFiles = new List<IListBlobItem>();

            BlobDataFiles = BlobFileSystem.GetDirectoryList(BlobDirectory);
                      
            MemoryStream stream = new MemoryStream();
               
            //add all files (except other zips) in our list to the zip files
            using (ZipFile zip = new ZipFile())
            {
                bool filesFound = false;
                foreach (var dir in BlobDataFiles.OfType<CloudBlobDirectory>())
                {
                    string tmpDirPath = dir.Uri.LocalPath;
                    tmpDirPath = tmpDirPath.Replace("/filesystem/", "filesystem/");
                    List<IListBlobItem> TmpItems = BlobFileSystem.GetDirectoryList(tmpDirPath);
                    foreach (var blob in TmpItems.OfType<CloudBlob>())
                    {
                        byte[] fileBytes = blob.DownloadByteArray();
                        try
                        {
                            blob.FetchAttributes();
                            zip.AddEntry(blob.Metadata["FileName"].ToString(), fileBytes);
                            filesFound = true;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                foreach (var blob in BlobDataFiles.OfType<CloudBlob>())
                {                 
                    byte[] fileBytes = blob.DownloadByteArray();
                    try
                    {
                        blob.FetchAttributes();
                        zip.AddEntry(blob.Metadata["FileName"].ToString(), fileBytes);
                        filesFound = true;
                    }
                    catch (Exception)
                    {
                    }
                }

                if (filesFound == true)
                {
                    try
                    {
                        zip.Save(stream);
                        stream.Position = 0;
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    stream = null;
                }
            }
            return stream;
        }

        public virtual FileCollection AllFiles()
        {
            return new AttributableFileCollection(DataFilesPath, AttrFilesPath, true);
        }

        public string AttrFilesPath
        {
            get
            {
                return m_attrDir;
            }
        }

        public bool CreateDir(string localDirName)
        {
            if (localDirName.Contains("../") || localDirName.Contains("..\\"))
            {
                // We won't allow going up a directory
                return false;
            }

            // Create the data directory
            string fullDirName = Path.Combine(m_dataDir, localDirName);
            if (!System.IO.Directory.Exists(fullDirName))
            {
                System.IO.Directory.CreateDirectory(fullDirName);
            }

            // Create the directory in the attribute path too
            fullDirName = Path.Combine(m_attrDir, localDirName);
            if (!System.IO.Directory.Exists(fullDirName))
            {
                System.IO.Directory.CreateDirectory(fullDirName);
            }

            return true;
        }

        private string DataFilesPath
        {
            get
            {
                return m_dataDir;
            }
        }

        /// <summary>
        /// Deletes either all files and/or all folders within this directory, potentially 
        /// leaving it empty when finished. Use with caution, there are only a few instances 
        /// in the OSBLE code where this should really be needed.
        /// </summary>
        public void DeleteContents(bool deleteFiles, bool deleteDirectories)
        {
            // Files first
            if (deleteFiles)
            {
                string[] files = Directory.GetFiles(m_path);
                foreach (string file in files)
                {
                    System.IO.File.Delete(file);
                }
            }

            // Now directories
            if (deleteDirectories)
            {
                string[] dirs = Directory.GetDirectories(m_path);
                foreach (string dir in dirs)
                {
                    // Perform a recursive deletion
                    Directory.Delete(dir, true);
                }
            }
        }

        /// <summary>
        /// Deletes the specified directory and all its contents. The directory 
        /// must exist within this file path.
        /// </summary>
        public bool DeleteDir(string localDirName)
        {
            string truePath;
            //Check if a specific dir name is provided
            if (localDirName == null)
            {
                string pathToDir = m_path;
                pathToDir = pathToDir.Replace("filesystem/", "");
                truePath = "filesystem/" + pathToDir;
                truePath += "/";
            }
            else
            {
                if (localDirName.Contains("../") || localDirName.Contains("..\\"))
                {
                    // We won't allow going up a directory
                    return false;
                }

                string pathToDir = m_path;
                pathToDir += "/" + localDirName + "/";
                pathToDir = pathToDir.Replace("filesystem/", "");
                truePath = "filesystem/" + pathToDir;
            }
            truePath = BlobFileSystem.FixPath(truePath);
            CloudBlobContainer container = BlobFileSystem.GetBlobContainer();
            List<IListBlobItem> BlobDataFiles = BlobFileSystem.GetBlobClient().ListBlobsWithPrefix(truePath).ToList();

            foreach (var BlobFile in BlobDataFiles.OfType<CloudBlob>())
            {
                BlobFile.Delete();
            }

            foreach (var BlobDir in BlobDataFiles.OfType<CloudBlobDirectory>())
            {
                DeleteDir(localDirName + "/" + BlobFileSystem.GetDirectoryPrefix(BlobDir));
            }

            return true;
        }

        public bool DeleteFile(string localFileName)
        {
            if (localFileName.Contains("../") || localFileName.Contains("..\\"))
            {
                // We won't allow going up a directory
                return false;
            }

            string filePath = m_path;
            filePath += "/";
            filePath += localFileName;
            filePath = filePath.Replace("filesystem/", "");

            CloudBlobContainer blobContainer = BlobFileSystem.GetBlobContainer();
            var blobContainerUri = blobContainer.Uri.AbsoluteUri;

            var sourceBlockBlob = blobContainer.GetBlobReference(filePath);
            if (BlobFileSystem.Exists(sourceBlockBlob))
            {
                sourceBlockBlob.Delete();
            }
            return true;
        }

        /// <summary>
        /// Gets a collection of files whose names satisfy the predicate.
        /// </summary>
        public virtual FileCollection File(Func<string, bool> predicate)
        {
            return new AttributableFileCollection(m_dataDir, AttrFilesPath, predicate);
        }

        public FileCollection File(string name)
        {
            return File(s => s == name);
        }

        /// <summary>
        /// Gets the first file in the path or null if there are no files.
        /// </summary>
        public OSBLEFile FirstFile
        {
            get
            {
                string[] dfNames = System.IO.Directory.GetFiles(m_dataDir);
                if (null == dfNames || 0 == dfNames.Length)
                {
                    return null;
                }

                string firstName = Path.GetFileName(dfNames[0]);
                return OSBLEFile.CreateFromExisting(
                    Path.Combine(m_dataDir, firstName),
                    AttributableFileCollection.GetAttrFileName(m_attrDir, firstName));
            }
        }

        /// <summary>
        /// Gets a subdirectory. Returns null if not found.
        /// </summary>
        public virtual OSBLEDirectory GetDir(string subdirName)
        {
            // Despite its simple implementation, the details of getting a subdirectory 
            // are very important. Each OSBLE "directory" is two directories in reality. 
            // What we DON'T want to do is have extra attribute folders pop up inside 
            // places where they shouldn't be. For example:
            //   Assume folders exists: /A and /A/B
            //   If this object represents the 'A' folder and this method is called to 
            //   get the 'B' subdirectory, then there should only be ONE attribute
            //   folder: /AAttr and that will have within it: /AAttr/B. If the default 
            //   constructor were used, then it would create /A/BAttr, which is NOT 
            //   what we want.

            string filenamePath = m_dataDir;
            filenamePath += "/" + subdirName;
            string remove = "filesystem/";
            filenamePath = filenamePath.Replace(remove, "");
            
            return new OSBLEDirectory(filenamePath);
        }
        
        public OSBLEFile GetFile(string fileName)
        {
            // If the file doesn't exist then we'll assume it's a relative path and 
            // try combining it with the data files path.
            if (!System.IO.File.Exists(fileName))
            {
                fileName = Path.Combine(DataFilesPath, fileName);
                if (!System.IO.File.Exists(fileName))
                {
                    return null;
                }
            }
            return OSBLEFile.CreateFromExisting(
                fileName,
                AttributableFileCollection.GetAttrFileName(
                    AttrFilesPath, Path.GetFileName(fileName)));

        }

        public ActionResult GetFile(string fileName, string Test)
        {            
            //Get the file
            HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName); // force download
            BlobFileSystem.GetBlobContainer().GetBlobReference(this.DownloadFile("/"+fileName)).DownloadToStream(HttpContext.Current.Response.OutputStream);
            HttpContext.Current.Response.End();         
            return new EmptyResult();
        }

        private FileCollection GetFilesWithAttribute(string attrClass, string attrName, string attrValue)
        {
            if (!System.IO.Directory.Exists(DataFilesPath))
            {
                return new AttributableFileCollection(DataFilesPath, AttrFilesPath,
                    new List<string>());
            }

            // Compare everything in lower case except the value
            attrClass = attrClass.ToLower();
            attrName = attrName.ToLower();

            string[] dataFiles = System.IO.Directory.GetFiles(DataFilesPath);
            List<string> files = new List<string>();
            foreach (string file in dataFiles)
            {
                // Get the name for the attribute file
                string attrFileName = AttributableFileCollection.GetAttrFileName(AttrFilesPath, file);
                if (string.IsNullOrEmpty(attrFileName) ||
                    !System.IO.File.Exists(attrFileName))
                {
                    continue;
                }

                OSBLEFile af = OSBLEFile.CreateFromExisting(file, attrFileName);

                if ("systemattributes" == attrClass)
                {
                    if (!af.ContainsSysAttr(attrName, attrValue))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!af.ContainsUserAttr(attrName, attrValue))
                    {
                        continue;
                    }
                }

                files.Add(file);
            }
            return new AttributableFileCollection(DataFilesPath, AttrFilesPath, files);
        }

        /// <summary>
        /// Gets a collection of files that all have the specified attribute name and 
        /// corresponding attribute value among the system attributes. If the 
        /// <paramref name="attrValue"/> parameter is null, then the collection will 
        /// contain any files that have the specified attribute, regardless of the 
        /// attribute's actual value.
        /// </summary>
        /// <param name="attrName">Name of attribute to search for</param>
        /// <param name="attrValue">Name of attribute value, or null to use only the 
        /// attribute name as the criteria</param>
        public FileCollection GetFilesWithSystemAttribute(string attrName, string attrValue)
        {
            return GetFilesWithAttribute("systemattributes", attrName, attrValue);
        }

        /// <summary>
        /// Gets a collection of blobs that are associated with this assignment
        /// </summary>
        public List<IListBlobItem> GetAssignmentBlobs()
        {
            List<IListBlobItem> BlobDataFiles = BlobFileSystem.GetDirectoryList(this.m_path + "/" + "AssignmentDocs");
            return BlobDataFiles;
        }

        /// <summary>
        /// Gets a collection of blobs in a specific directory relative to this OSBLEDirectory
        /// </summary>
        public List<IListBlobItem> GetBlobs(string path)
        {
            List<IListBlobItem> BlobDataFiles = new List<IListBlobItem>();
            if (path != null)
            {
                BlobDataFiles = BlobFileSystem.GetDirectoryList(this.m_path + "/" + path);
            }
            else
            {
                BlobDataFiles = BlobFileSystem.GetDirectoryList(this.m_path);
            }
            return BlobDataFiles;
        }

        /// <summary>
        /// Gets a collection the total collection of blob files in this directory
        /// </summary>
        public List<IListBlobItem> GetAllBlobs(string path)
        {
            
            List<IListBlobItem> BlobDataFiles = new List<IListBlobItem>();
            List<IListBlobItem> TotalBlobFiles = new List<IListBlobItem>();
            
            if(path == null)
            {
                BlobDataFiles = BlobFileSystem.GetDirectoryList(this.m_path);
            }
            else
            {
                BlobDataFiles = BlobFileSystem.GetDirectoryList(path);
            }

            foreach (var blob in BlobDataFiles.OfType<CloudBlobDirectory>())
            {
                string prefix = BlobFileSystem.GetDirectoryPrefix(blob);
                if (path == null)
                {
                    TotalBlobFiles.AddRange(GetAllBlobs(this.m_path + "/" + prefix));
                }
                else
                {
                    TotalBlobFiles.AddRange(GetAllBlobs(path+"/"+prefix));
                }
            }

            foreach (var blob in BlobDataFiles.OfType<CloudBlob>())
            {
                TotalBlobFiles.Add(blob);
            }

            return TotalBlobFiles;
        }

        /// <summary>
        /// Gets a collection of files that all have the specified attribute name and 
        /// corresponding attribute value among the user attributes. If the 
        /// <paramref name="attrValue"/> parameter is null, then the collection will 
        /// contain any files that have the specified attribute, regardless of the 
        /// attribute's actual value.
        /// </summary>
        /// <param name="attrName">Name of attribute to search for</param>
        /// <param name="attrValue">Name of attribute value, or null to use only the 
        /// attribute name as the criteria</param>
        public FileCollection GetFilesWithUserAttribute(string attrName, string attrValue)
        {
            return GetFilesWithAttribute("userattributes", attrName, attrValue);
        }

        [Obsolete("Unsafe. The file system is being redesigned so that the \"outside world\" isn't aware of any paths on disk and can perform all operations without even knowing the path.")]
        public string GetPath()
        {
            return m_path;
        }

        /// <summary>
        /// Builds and returns an XML listing for all the files in the path. All attributes 
        /// are included with each file.
        /// Format example:
        /// &lt;file_list&gt;
        ///   &lt;file name=&quot;whatever.ext&quot;&gt;
        ///     (all attributes in here)
        ///   &lt;/file&gt;
        /// &lt;/file_list&gt;
        /// </summary>
        public string GetXMLListing(CourseUser courseUser, bool recurse)
        {
            // Determine what permissions this user has, since permission attributes 
            // will be put in the listing.
            bool canDeleteFolders;
            if (null == courseUser)
            {
                canDeleteFolders = false;
            }
            else
            {
                canDeleteFolders = courseUser.AbstractRole.CanModify;
            }
            
            StringBuilder sbTest = new StringBuilder("<file_list>");
            sbTest.AppendFormat("<folder name=\"/\" can_delete=\"{0}\" can_upload_to=\"{0}\">",
                canDeleteFolders.ToString());

            // We'll have a folder node for the root
            // Get the data directory to the OSBLE storage on Azure
            string BlobDirectory = BlobFileSystem.FixPath(m_dataDir);
            BlobDirectory += "/";

            GetXMLListing(courseUser, m_dataDir, recurse, sbTest);

            sbTest.Append("</folder></file_list>");
            return sbTest.ToString();
        }

        private void GetXMLListing(CourseUser courseUser, string dir, bool recurse, StringBuilder sbTest)
        {            
            // Determine what permissions this user has, since permission attributes 
            // will be put in the listing.
            bool canDeleteFolders;
            if (null == courseUser)
            {
                canDeleteFolders = false;
            }
            else
            {
                canDeleteFolders = courseUser.AbstractRole.CanModify;
            }

            //Get a list of BlotItems(files) in the given directory path
            string BlobDirectory = BlobFileSystem.FixPath(dir);
            List<IListBlobItem> BlobDataFiles = BlobFileSystem.GetDirectoryList(BlobDirectory);
           
            // Do directories first if we've been asked to recurse
            if (recurse)
            {              
                foreach (var BlobDir in BlobDataFiles.OfType<CloudBlobDirectory>())
                {                 
                    sbTest.AppendFormat("<folder name=\"{0}\" can_delete=\"{1}\" can_upload_to=\"{1}\">",
                        BlobFileSystem.GetDirectoryPrefix(BlobDir),
                        canDeleteFolders.ToString());
                    GetXMLListing(courseUser, BlobFileSystem.FixURIPath(BlobFileSystem.GetDirectoryPrefix(BlobDir), dir), recurse, sbTest);
                    sbTest.Append("</folder>");
                }
            }

            bool BlobDirPlaceHolder = false;

            //Do the actual files(blobs) next after looking through the directories.
            foreach (var BlobFile in BlobDataFiles.OfType<CloudBlob>())
            {
                BlobFile.FetchAttributes();
                if (BlobFile.Metadata["FileName"] != "dir.osble")
                {
                    if (BlobFile.Metadata["assignment_description"] != null || BlobFile.Metadata["assignment_solution"] != null)
                    {
                        string xml = "";
                        foreach (var metadataKey in BlobFile.Metadata.Keys)
                        {
                            xml += "<" + metadataKey.ToString() + ">" + BlobFile.Metadata.Get(metadataKey.ToString()) + "</" + metadataKey.ToString() + ">";
                        }
                        sbTest.AppendFormat("<file name=\"{0}\" can_delete=\"{1}\">{2}</file>",
                           BlobFile.Metadata["FileName"].ToString(),
                           canDeleteFolders.ToString(), xml);
                    }
                    else
                    {
                        if (BlobFile.Metadata["FileName"] != null)
                        {
                            sbTest.AppendFormat("<file name=\"{0}\" can_delete=\"{1}\"></file>",
                                BlobFile.Metadata["FileName"].ToString(),
                                canDeleteFolders.ToString());
                        }
                        else
                        {
                            string tmpName = BlobFile.Name.ToString();
                            string[] tmpWordsSplit = tmpName.Split('/');
                           sbTest.AppendFormat("<file name=\"{0}\" can_delete=\"{1}\"></file>",
                            tmpWordsSplit.Last(),
                            canDeleteFolders.ToString());
                        }
                    }
                }
                else if (BlobFile.Metadata["FileName"] == "dir.osble")
                {
                    BlobDirPlaceHolder = true;
                }
            }

            //If there is no blob place holder in the directory create one
            if (BlobDirPlaceHolder != true)
            {
                string tmpName = BlobDirectory;
                string[] tmpWordsSplit = tmpName.Split('/');
                string dirName = tmpWordsSplit[tmpWordsSplit.Count() - 1];
                BlobDirectory = BlobDirectory.Replace("filesystem/", "");
                BlobDirectory += "/";
                BlobFileSystem.CreateFolder(BlobDirectory, dirName);
            }
        
        }

        /// <summary>
        /// Moves all data and attribute files from one location to another. The 
        /// empty folders remaining from the source storage can optionally be 
        /// removed when the move is completed.
        /// </summary>
        public static int MoveAll(OSBLEDirectory from,
            int to, string oldName)
        {
            int moved = 0;

            string toString = to.ToString();

            from.RenameBlobDir(null, oldName, toString);

            return moved;
        }

        public Stream OpenFileRead(string fileName)
        {
            string path = Path.Combine(DataFilesPath, fileName);
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            }
            catch (Exception) { return null; }

            return fs;
        }

        /// <summary>
        /// Reads all the lines from a text file and returns them as an array of 
        /// strings. If the file is not found then null is returned.
        /// </summary>
        public string[] ReadFileLines(string fileName)
        {
            string path = System.IO.Path.Combine(m_path, fileName);
            if (!System.IO.File.Exists(path))
            {
                return null;
            }

            return System.IO.File.ReadAllLines(path);
        }

        /// <summary>
        /// Rename's a blob by creating a new blob with the new name and copy the contents of the old blob
        /// data over to the new copy.
        /// </summary>
        public void RenameBlobDir(string folderPath, string trueOldName, string newName)
        {
            string pathToDir = m_path;
            
            if (folderPath == null)
            {
                pathToDir = pathToDir.Replace("filesystem/", "");
                pathToDir += "/";
            }
            else
            {
                pathToDir += "/" + folderPath + "/";
                pathToDir = pathToDir.Replace("filesystem/", "");
            }

            string truePath = "filesystem/" + pathToDir;

            CloudBlobContainer container = BlobFileSystem.GetBlobContainer();
            List<IListBlobItem> BlobDataFiles = BlobFileSystem.GetBlobClient().ListBlobsWithPrefix(truePath).ToList();

            foreach (var BlobFile in BlobDataFiles.OfType<CloudBlob>())
            {
                var sourceBlockBlob = BlobFile as CloudBlob;
                string newBlobName = sourceBlockBlob.Name.ToString().Replace(trueOldName, newName);
                var newBlob = container.GetBlockBlobReference(newBlobName);
                newBlob.CopyFromBlob(sourceBlockBlob);
                sourceBlockBlob.Delete();
            }

            foreach (var BlobDir in BlobDataFiles.OfType<CloudBlobDirectory>())
            {
                RenameBlobDir(folderPath + "/" + BlobFileSystem.GetDirectoryPrefix(BlobDir), trueOldName, newName);
            }
        }

        /// <summary>
        /// Checks to see if a blob directory exists
        /// </summary>
        public bool CheckIfBlobExists(string pathtofolder)
        {
            string filePath = m_path;
            pathtofolder = pathtofolder.Replace("//", "/");
            filePath += "/";
            filePath += pathtofolder;

            return BlobFileSystem.CheckDirExists(filePath);
        }

        /// <summary>
        /// Renames a file and its corresponding attribute file. The existing 
        /// file name must be just a file name with no subdirs and not an 
        /// absolute (rooted) path. The same goes for the new file name.
        /// </summary>
        public bool RenameFile(string fileName, string newFileName)
        {
            // Quick security check
            if (fileName.Contains('/') || fileName.Contains('\\') ||
                newFileName.Contains('/') || newFileName.Contains('\\'))
            {
                return false;
            }

            string filePath = m_path;
            fileName = fileName.Replace("//", "/");
            filePath += "/";
            filePath += fileName;
            filePath = filePath.Replace("filesystem/", "");

            CloudBlobContainer blobContainer = BlobFileSystem.GetBlobContainer();
            var blobContainerUri = blobContainer.Uri.AbsoluteUri;

            var sourceBlockBlob = blobContainer.GetBlobReference(filePath);

            string newBlobName = m_path + "/" + newFileName;
            newBlobName = newBlobName.Replace("filesystem/", "");

            var newBlob = blobContainer.GetBlockBlobReference(newBlobName);

            newBlob.CopyFromBlob(sourceBlockBlob);
            newBlob.Metadata["FileName"] = newFileName;
            newBlob.SetMetadata();
            sourceBlockBlob.Delete();

            return true;
        }

        /// <summary>
        /// Writes all key-value pairs in the dictionary as XML elements. If the 
        /// dictionary is null, then nothing is written.
        /// </summary>
        private static void WriteElements(XmlWriter writer, Dictionary<string, string> elements)
        {
            if (null == elements)
            {
                return;
            }

            foreach (KeyValuePair<string, string> kvp in elements)
            {
                if (null != kvp.Value)
                {
                    writer.WriteElementString(kvp.Key, kvp.Value);
                }
            }
        }

        private class AttributableFileCollection : FileCollection
        {
            private string m_dataDir, m_attrDir;
            
            public AttributableFileCollection(string dataDir, string attrDir,
                bool addDataFileNamesToList)
                : base(dataDir)
            {
                m_dataDir = dataDir;
                m_attrDir = attrDir;
                if (addDataFileNamesToList)
                {
                    _fileNames.AddRange(System.IO.Directory.GetFiles(dataDir));
                }
            }

            public AttributableFileCollection(string dataDir, string attrDir,
                Func<string, bool> predicate)
                : base(dataDir)
            {
                m_dataDir = dataDir;
                m_attrDir = attrDir;

                if (!System.IO.Directory.Exists(dataDir))
                {
                    // Leave the file list empty and return
                    return;
                }

                // Add only files that match the predicate
                foreach (string file in System.IO.Directory.GetFiles(dataDir))
                {
                    string fileName = Path.GetFileName(file);
                    if (predicate(fileName))
                    {
                        _fileNames.Add(file);
                    }
                }
            }

            public AttributableFileCollection(string dataDir, string attrDir,
                IList<string> files)
                : base(dataDir)
            {
                m_dataDir = dataDir;
                m_attrDir = attrDir;
                _fileNames.AddRange(files.ToArray());
            }
            
            /// <summary>
            /// Because the attributable files have an extra XML file associated with 
            /// them, we need to override the deletion method so that we get rid of 
            /// those too.
            /// </summary>
            /// <returns>The number of files deleted, not including any attribute XML 
            /// files (as these "don't exist" to the outside world).</returns>
            public override int Delete()
            {
                int removeCounter = 0;
                foreach (string name in _fileNames)
                {
                    try
                    {
                        System.IO.File.Delete(name);
                        
                        // Delete the accompanying attribute file, if it exists
                        string attrFile = GetAttrFileName(m_attrDir, name);
                        if (System.IO.File.Exists(attrFile))
                        {
                            System.IO.File.Delete(attrFile);
                        }
                        removeCounter++;
                    }
                    catch (Exception)
                    {
                    }
                }
                return removeCounter;
            }

            public static string GetAttrFileName(string attrFileDir, string dataFileName)
            {
                if (!Path.IsPathRooted(dataFileName))
                {
                    return Path.Combine(attrFileDir, "attr_" + dataFileName + ".xml");
                }

                return Path.Combine(attrFileDir,
                    "attr_" + Path.GetFileName(dataFileName) + ".xml");
            }
        }
    }
}
