﻿// Created 5-15-13 by Evan Olds for the OSBLE project at WSU

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace OSBLE.Models.FileSystem
{
    /// <summary>
    /// There are two folders within an AttributableFiles folder: data and attr
    /// data:
    ///   There will be files in this folder that were uploaded by the instructor (or 
    ///   potentially the administrator). Each file will have an accompanying XML file 
    ///   for attributes for this file, but this file is in the attr folder.
    /// attr:
    ///   Contains XML files for file attributes. There should be a one-to-one correspondence 
    ///   of data files to XML attribute files, with the exception of the possibility of a 
    ///   data file that has no accompanying XML attribute file and thus implicitly has no 
    ///   attributes.
    /// </summary>
    public class AttributableFilesFilePath : FileSystemBase
    {
        private const string c_emptyAttr = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
@"<osblefileattributes>
  <systemattributes></systemattributes>
  <userattributes></userattributes>
</osblefileattributes>";
        
        public AttributableFilesFilePath(IFileSystem pathBuilder)
            : base(pathBuilder)
        {
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
        /// Adds a file from the specified stream data. No user attributes are created 
        /// and a few auto-system attributes are added.
        /// </summary>
        public override bool AddFile(string fileName, Stream data)
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

        public override FileCollection AllFiles()
        {
            return new AttributableFileCollection(DataFilesPath, AttrFilesPath, true);
        }

        private string AttrFilesPath
        {
            get
            {
                return Path.Combine(PathBuilder.GetPath(), "AttributableFiles", "attr");
            }
        }

        private string DataFilesPath
        {
            get
            {
                return Path.Combine(PathBuilder.GetPath(), "AttributableFiles", "data");
            }
        }

        /// <summary>
        /// Gets a collection of files whose names satisfy the predicate.
        /// </summary>
        public override FileCollection File(Func<string, bool> predicate)
        {
            return new AttributableFileCollection(DataFilesPath, AttrFilesPath, predicate);
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

                // Try to load the attribute file as an XmlDocument
                XmlDocument doc = new XmlDocument();
                try { doc.Load(attrFileName); }
                catch (Exception) { continue; }

                XmlElement root = doc.DocumentElement;
                if ("osblefileattributes" != root.LocalName.ToLower())
                {
                    // Invalid attribute file
                    continue;
                }

                // Find the child under the root with a name that matches the 
                // requested attribute class
                XmlNode parentAttr = null;
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    if (attrClass == root.ChildNodes[i].LocalName.ToLower())
                    {
                        parentAttr = root.ChildNodes[i];
                        break;
                    }
                }

                if (null == parentAttr)
                {
                    // Didn't find it, so move on
                    continue;
                }

                // Look through all the attributes
                for (int i = 0; i < parentAttr.ChildNodes.Count; i++)
                {
                    XmlNode temp = parentAttr.ChildNodes[i];
                    if (attrName == temp.LocalName.ToLower())
                    {
                        // We've found the attribute, now we just need to compare the 
                        // value. If the attrValue passed to this function is null then 
                        // we always take it
                        if (null == attrValue || attrValue == temp.InnerText)
                        {
                            files.Add(file);
                        }
                        break;
                    }
                }
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
        
        public override string GetPath()
        {
            return DataFilesPath;
        }

        /// <summary>
        /// Builds and returns an XML listing for all the files in the path. All attributes 
        /// are included with each file.
        /// Format example:
        /// &lt;file_list&gt;
        ///   &lt;file name=&quot;whatever.dat&quot;&gt;
        ///     (all attributes in here)
        ///   &lt;/file&gt;
        /// &lt;/file_list&gt;
        /// </summary>
        public string GetXMLListing()
        {
            StringBuilder sb = new StringBuilder("<file_list>");
            string[] dataFiles = System.IO.Directory.GetFiles(DataFilesPath);
            foreach (string file in dataFiles)
            {
                // Get the name for the attribute file
                string attrFileName = AttributableFileCollection.GetAttrFileName(AttrFilesPath, file);
                if (string.IsNullOrEmpty(attrFileName) ||
                    !System.IO.File.Exists(attrFileName))
                {
                    continue;
                }

                string xml = System.IO.File.ReadAllText(attrFileName);
                sb.AppendFormat("<file name=\"{0}\">{1}</file>",
                    System.IO.Path.GetFileName(file), xml);
            }
            sb.Append("</file_list>");
            return sb.ToString();
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

            public static string GetAttrFileName(string attrFileDir, string fileName)
            {
                int i = fileName.LastIndexOf(Path.DirectorySeparatorChar);
                if (-1 == i)
                {
                    return Path.Combine(attrFileDir, "attr_" + fileName + ".xml");
                }

                return Path.Combine(attrFileDir,
                    "attr_" + fileName.Substring(i + 1) + ".xml");
            }
        }
    }
}