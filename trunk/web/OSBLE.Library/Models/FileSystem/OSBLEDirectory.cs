﻿// Created 5-15-13 by Evan Olds for the OSBLE project at WSU

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using OSBLE.Models.Courses;

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
            if (!System.IO.Directory.Exists(dataPath))
            {
                throw new System.IO.DirectoryNotFoundException(
                    "Directory not found: " + dataPath);
            }

            m_dataDir = m_path = dataPath;
            m_attrDir = Path.Combine(
                Path.GetDirectoryName(dataPath),
                Path.GetFileName(dataPath) + "Attr");

            // The data directory must already exist in order to use this constructor, 
            // but the attribute directory need not exist (before construction). It 
            // does have to exist after construction though so we create it here if 
            // we need to.
            if (!System.IO.Directory.Exists(m_attrDir))
            {
                System.IO.Directory.CreateDirectory(AttrFilesPath);
            }
        }
        
        public OSBLEDirectory(string dataPath, string attrPath)
        {
            m_dataDir = dataPath;
            m_attrDir = attrPath;
            m_path = dataPath;
            
            if (!System.IO.Directory.Exists(DataFilesPath))
            {
                System.IO.Directory.CreateDirectory(DataFilesPath);
            }
            if (!System.IO.Directory.Exists(AttrFilesPath))
            {
                System.IO.Directory.CreateDirectory(AttrFilesPath);
            }
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
            Dictionary<string, string> sys = new Dictionary<string, string>();
            sys.Add("created", DateTime.Now.ToString());

            return AddFile(fileName, data, sys, null);
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
            
            string path = DataFilesPath;
            string filePath = Path.Combine(path, fileName);
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            if (!System.IO.Directory.Exists(AttrFilesPath))
            {
                System.IO.Directory.CreateDirectory(AttrFilesPath);
            }

            // Get the file name for the attributes file
            string attrFileName = AttributableFileCollection.GetAttrFileName(AttrFilesPath, fileName);

            // Create the XML file for the attributes
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                CloseOutput = true,
                Indent = true,
                NewLineChars = "\n",
            };
            XmlWriter writer = null;
            try { writer = XmlWriter.Create(attrFileName, settings); }
            catch (Exception)
            {
                writer = null;
            }
            if (null == writer) { return false; }
            
            // Write the attribute data
            writer.WriteStartElement("osblefileattributes");
                // System attributes first
                writer.WriteStartElement("systemattributes");
                WriteElements(writer, sysAttrs);
                writer.WriteEndElement();
                // Then user attributes
                writer.WriteStartElement("userattributes");
                WriteElements(writer, usrAttrs);
                writer.WriteEndElement();
            writer.WriteEndElement();
            // Close
            writer.Close();

            bool retVal = true;
            try
            {
                FileStream output = System.IO.File.Open(filePath, FileMode.Create);
                data.CopyTo(output);
                output.Close();
                output.Dispose();
            }
            catch (Exception)
            {
                retVal = false;
            }

            // If we wrote the attribute data but not the file data then we want 
            // to delete the attribute file
            if (!retVal)
            {
                if (System.IO.File.Exists(attrFileName))
                {
                    System.IO.File.Delete(attrFileName);
                }
            }

            return retVal;
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
            if (localDirName.Contains("../") || localDirName.Contains("..\\"))
            {
                // We won't allow going up a directory
                return false;
            }

            string fullDirName = Path.Combine(m_dataDir, localDirName);
            if (!System.IO.Directory.Exists(fullDirName))
            {
                return false;
            }

            // Delete the data directory
            System.IO.Directory.Delete(fullDirName, true);

            // Delete the attribute directory too, if it exists
            fullDirName = Path.Combine(m_attrDir, localDirName);
            if (System.IO.Directory.Exists(fullDirName))
            {
                System.IO.Directory.Delete(fullDirName, true);
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
            
            string fullFileName = Path.Combine(m_dataDir, localFileName);
            if (!System.IO.File.Exists(fullFileName))
            {
                return false;
            }

            // Delete the data file
            System.IO.File.Delete(fullFileName);

            // Delete the attribute file too, if it exists
            fullFileName = AttributableFileCollection.GetAttrFileName(
                m_attrDir, localFileName);
            if (System.IO.File.Exists(fullFileName))
            {
                System.IO.File.Delete(fullFileName);
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
            
            string dataPath = Path.Combine(m_dataDir, subdirName);
            string attrPath = Path.Combine(m_attrDir, subdirName);

            // Data path must exist
            if (!Directory.Exists(dataPath))
            {
                return null;
            }

            return new OSBLEDirectory(dataPath, attrPath);
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
        public string GetXMLListing(CourseUser courseUser, bool recurse = false)
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

            StringBuilder sb = new StringBuilder("<file_list>");

            // We'll have a folder node for the root
            sb.AppendFormat("<folder name=\"/\" can_delete=\"{0}\" can_upload_to=\"{0}\">",
                canDeleteFolders.ToString());

            GetXMLListing(courseUser, m_dataDir, recurse, sb);
            sb.Append("</folder></file_list>");
            return sb.ToString();
        }

        private void GetXMLListing(CourseUser courseUser, string dir, bool recurse, StringBuilder sb)
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
            
            string[] dataFiles = System.IO.Directory.GetFiles(dir);

            // Do directories first if we've been asked to recurse
            if (recurse)
            {
                foreach (string folder in System.IO.Directory.GetDirectories(dir))
                {
                    sb.AppendFormat("<folder name=\"{0}\" can_delete=\"{1}\" can_upload_to=\"{1}\">", 
                        folder.Substring(folder.LastIndexOf(Path.DirectorySeparatorChar) + 1),
                        canDeleteFolders.ToString());
                    GetXMLListing(courseUser, folder, recurse, sb);
                    sb.Append("</folder>");
                }
            }

            // Determine the directory for the attribute XML files
            if (!dir.StartsWith(m_dataDir)) { return; }
            string attrDir = m_attrDir + dir.Substring(m_dataDir.Length);            

            // Now do the actual files
            foreach (string file in dataFiles)
            {
                // Get the file name for the attribute file
                string attrFileName = AttributableFileCollection.GetAttrFileName(attrDir, file);
                if (string.IsNullOrEmpty(attrFileName) ||
                    !System.IO.File.Exists(attrFileName))
                {
                    // Add a file with no attributes
                    sb.AppendFormat("<file name=\"{0}\" can_delete=\"{1}\"></file>",
                        System.IO.Path.GetFileName(file), canDeleteFolders);
                }
                else
                {
                    // Add a file with attributes from its attribute XML file
                    string xml = System.IO.File.ReadAllText(attrFileName);
                    if (xml.StartsWith("<?xml"))
                    {
                        int i = xml.IndexOf("?>");
                        xml = xml.Substring(i + 2);
                    }
                    sb.AppendFormat("<file name=\"{0}\" can_delete=\"{2}\">{1}</file>",
                        System.IO.Path.GetFileName(file), xml, canDeleteFolders);
                }
            }
        }

        /// <summary>
        /// Moves all data and attribute files from one location to another. The 
        /// empty folders remaining from the source storage can optionally be 
        /// removed when the move is completed.
        /// </summary>
        public static int MoveAll(OSBLEDirectory from,
            OSBLEDirectory to, bool deleteFromFoldersWhenDone)
        {
            int moved = 0;
            string[] srcFiles = System.IO.Directory.GetFiles(from.m_dataDir);
            foreach (string srcData in srcFiles)
            {
                string srcAttr = AttributableFileCollection.GetAttrFileName(
                    from.m_attrDir, srcData);
                
                // Move both files
                System.IO.File.Move(srcData, Path.Combine(to.m_dataDir, Path.GetFileName(srcData)));
                System.IO.File.Move(srcAttr, Path.Combine(to.m_attrDir, Path.GetFileName(srcAttr)));

                moved++;
            }

            // Delete the directories from the source, if it was requested
            if (deleteFromFoldersWhenDone)
            {
                System.IO.Directory.Delete(from.m_dataDir, true);
                System.IO.Directory.Delete(from.m_attrDir, true);
            }

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

            // First rename data file
            string dataFile = Path.Combine(m_dataDir, fileName);
            string dataFileNew = Path.Combine(m_dataDir, newFileName);
            if (!System.IO.File.Exists(dataFile))
            {
                return false;
            }
            System.IO.File.Move(dataFile, dataFileNew);

            // Now the attribute file
            string attrFile = AttributableFileCollection.GetAttrFileName(
                m_attrDir, fileName);
            string attrFileNew = AttributableFileCollection.GetAttrFileName(
                m_attrDir, newFileName);
            if (System.IO.File.Exists(attrFile))
            {
                System.IO.File.Move(attrFile, attrFileNew);
            }

            return true;
        }

        public void ReplaceSysAttrAll(string name, string oldValue, string newValue)
        {
            string[] files = System.IO.Directory.GetFiles(m_dataDir);
            foreach (string dataFile in files)
            {
                OSBLEFile of = GetFile(dataFile);
                if (of.ContainsSysAttr(name, oldValue))
                {
                    of.SetSysAttr(name, newValue);
                    of.SaveAttrs();
                }
            }
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
