// Created by Evan Olds for the OSBLE project at WSU
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OSBLE.Models.FileSystem
{
    /// <summary>
    /// Represents a file with attributes that can describe many things, including access 
    /// permissions. An "attribute" in this context is a key-value pair of strings. The 
    /// file has a set of "system" attributes and "user" attributes. The system attributes 
    /// are designed to be things that have some potential meaning within the OSBLE code 
    /// whereas user attributes are meant to be solely user determined.
    /// Some system attributes have reserved meaning, and map to properties in this class. It 
    /// is recommended that attribute files ONLY ever get modified through the use of this 
    /// class, so as to keep attribute meaning consistent.
    /// All attributable files are meant to exist within an attributed file storage location.
    /// See the <see cref="AttributableFilesFilePath"/> class.
    /// </summary>
    public class AttributableFile
    {
        private string m_attrFileName = null;

        private string m_dataFileName = null;

        private bool m_modified = false;

        private Dictionary<string, string> m_sys = new Dictionary<string, string>();

        private Dictionary<string, string> m_usr = new Dictionary<string, string>();

        /// <summary>
        /// Creates an AttributableFile from existing data and attribute files on disk.
        /// </summary>
        /// <param name="dataFullPath">Full path and file name for the data file.</param>
        /// <param name="attrFullPath">Full path and file name for the attribute file.</param>
        private AttributableFile(string dataFullPath, string attrFullPath)
        {
            m_dataFileName = dataFullPath;
            m_attrFileName = attrFullPath;

            // Load the attributes into memory
            XmlDocument doc = new XmlDocument();
            doc.Load(attrFullPath);

            XmlElement root = doc.DocumentElement;
            if ("osblefileattributes" != root.LocalName.ToLower())
            {
                // Invalid attribute file
                throw new Exception(
                    "XML attribute file was invalid for file: " + dataFullPath);
            }

            // Find the system attributes first
            XmlNodeList sys = doc.GetElementsByTagName("systemattributes");
            if (sys.Count > 0)
            {
                foreach (XmlNode node in sys[0].ChildNodes)
                {
                    m_sys.Add(node.LocalName, node.InnerText);
                }
            }

            // Now the user attributes
            // Find the system attributes first
            XmlNodeList usr = doc.GetElementsByTagName("userattributes");
            if (usr.Count > 0)
            {
                foreach (XmlNode node in usr[0].ChildNodes)
                {
                    m_usr.Add(node.LocalName, node.InnerText);
                }
            }

            // Everything is in memory and nothing is modified yet
            m_modified = false;
        }

        public string AttributeFileName
        {
            get { return m_attrFileName; }
        }

        public bool CanUserDownload(OSBLE.Models.Courses.CourseUser user)
        {
            // There's a system attribute that would make this public to 
            // any course user.
            if (m_sys.ContainsKey("any_course_user_can_download"))
            {
                bool b;
                if (!bool.TryParse(m_sys["any_course_user_can_download"], out b))
                {
                    // We take the attribute without any value to mean true
                    b = true;
                }
                return b;
            }

            // The current security model is only course modifiers can get 
            // assignment solutions.
            if (m_sys.ContainsKey("assignment_solution"))
            {
                return user.AbstractRole.CanModify;
            }

            return true;
        }

        public bool ContainsSystemAttribute(string attributeName)
        {
            return m_sys.ContainsKey(attributeName);
        }

        public bool ContainsSystemAttribute(string attributeName, string attributeValue)
        {
            if (m_sys.ContainsKey(attributeName))
            {
                return (attributeValue == m_sys[attributeName]);
            }
            return false;
        }

        public bool ContainsUserAttribute(string attributeName)
        {
            return m_usr.ContainsKey(attributeName);
        }

        public bool ContainsUserAttribute(string attributeName, string attributeValue)
        {
            if (m_usr.ContainsKey(attributeName))
            {
                return (attributeValue == m_usr[attributeName]);
            }
            return false;
        }

        public static AttributableFile CreateFromExisting(string dataFullPath, string attrFullPath)
        {
            return new AttributableFile(dataFullPath, attrFullPath);
        }

        // TODO: Decide how I want to implement this (auto-create system attributes?)
        /*
        public static AttributableFile CreateNew(System.IO.Stream fileData, string dataFullPath,
            string attrFullPath)
        {
            return new AttributableFile(dataFullPath, attrFullPath);
        }
        */

        public string DataFileName
        {
            get { return m_dataFileName; }
        }

        /// <summary>
        /// Gets a value indicating whether the set of attributes has been modified since 
        /// the last save. This value cannot be set directly. Use the "Save" (TODO) method to 
        /// write changes to disk and reset this value to false.
        /// </summary>
        public bool Modified
        {
            get { return m_modified; }
        }
    }
}
