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
    /// permissions. An "attribute" in this context is, for the most part, a key-value pair 
    /// of strings. Some attributes will have no associated value and just exist as a "key". 
    /// The file has a set of "system" attributes and "user" attributes. The system 
    /// attributes are designed to be things that have some potential meaning within the 
    /// OSBLE code whereas user attributes are meant to be solely user determined.
    /// Some system attributes have reserved meaning, and map to properties in this class. It 
    /// is recommended that attribute files ONLY ever get modified through the use of this 
    /// class, so as to keep attribute meaning consistent.
    /// All attributable files are meant to exist within an <see cref="OSBLEDirectory"/>.
    /// </summary>
    public class OSBLEFile
    {
        private const string c_emptyAttr = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
@"<osblefileattributes>
  <systemattributes></systemattributes>
  <userattributes></userattributes>
</osblefileattributes>";
        
        private string m_attrFileName = null;

        private string m_dataFileName = null;
        
        private XmlDocument m_doc = null;

        private bool m_modified = false;

        private XmlNode m_sys = null;

        /// <summary>
        /// Creates an AttributableFile from existing data and attribute files on disk. The 
        /// data file MUST exist, but the attribute file does not have to. If it does then 
        /// it is loaded as-is, otherwise it is created as an empty attribute file.
        /// </summary>
        /// <param name="dataFullPath">Full path and file name for the data file.</param>
        /// <param name="attrFullPath">Full path and file name for the attribute file.</param>
        private OSBLEFile(string dataFullPath, string attrFullPath)
        {
            m_dataFileName = dataFullPath;
            m_attrFileName = attrFullPath;

            if (!System.IO.File.Exists(m_attrFileName))
            {
                System.IO.File.WriteAllText(m_attrFileName, c_emptyAttr);
            }

            // Load the XML document into memory
            m_doc = new XmlDocument();
            m_doc.Load(attrFullPath);

            XmlElement root = m_doc.DocumentElement;
            if ("osblefileattributes" != root.LocalName.ToLower())
            {
                // Invalid attribute file
                throw new Exception(
                    "XML attribute file was invalid for file: " + dataFullPath);
            }

            // Find the system attributes node
            m_sys = null;
            foreach (XmlNode child in root.ChildNodes)
            {
                if ("systemattributes" == child.LocalName.ToLower())
                {
                    m_sys = child;
                    break;
                }
            }
            
            // We NEED the system attributes node to function properly
            if (null == m_sys)
            {
                throw new Exception(
                    "Could not find system attributes node in XML attribute file");
            }

            // Everything is in memory and nothing is modified yet
            m_modified = false;
        }

        /// <summary>
        /// Gets or sets the ABET proficiency level attribute for the file. Note that 
        /// setting this property will NOT update the file on disk. You must call 
        /// the <see cref="SaveAttrs"/> method to save changes after setting in order 
        /// for the changes to be written to disk.
        /// </summary>
        public string ABETProficiencyLevel
        {
            get
            {
                return GetSysAttr("ABETProficiencyLevel");
            }
            set
            {
                // Special case when setting to null: delete the attribute entirely
                if (null == value)
                {
                    DeleteSysAttrs("ABETProficiencyLevel");
                }
                else
                {
                    SetSysAttr("ABETProficiencyLevel", value);
                }
            }
        }

        /// <summary>
        /// Adds an attribute to the collection of system attributes, with the 
        /// possibility of creating a duplicate. No checks are made to see 
        /// whether or not an attribute with the name already exists. This 
        /// method adds the attribute regardless.
        /// If you wish to replace attributes that already exist, then use 
        /// methods that start with "Set" as opposed to methods that start 
        /// with "Add".
        /// </summary>
        public void AddSysAttr(string name, string value)
        {
            // We're about to modify
            m_modified = true;

            // Add a new node
            XmlElement elem = m_doc.CreateElement(name);
            elem.InnerText = value;
            m_sys.AppendChild(elem);
        }

        public string AttributeFileName
        {
            get { return m_attrFileName; }
        }

        public bool CanUserDownload(OSBLE.Models.Courses.CourseUser user)
        {
            // There's a system attribute that would make this public to 
            // any course user.
            XmlNodeList any = m_doc.GetElementsByTagName("any_course_user_can_download");
            if (any.Count > 0)
            {
                return true;
            }

            // The current security model is only course modifiers can get 
            // assignment solutions.
            if (ContainsSysAttr("assignment_solution"))
            {
                return user.AbstractRole.CanModify;
            }

            return true;
        }

        private bool ContainsAttribute(string category, string attributeName, string attributeValue)
        {
            // Find the system or user attributes first. The category is expected to be 
            // either "systemattributes" or "userattributes".
            XmlNodeList sys = m_doc.GetElementsByTagName(category);
            if (0 == sys.Count)
            {
                // This actually indicates a fairly large problem (corrupt 
                // attributes file)
                return false;
            }

            foreach (XmlNode node in sys[0].ChildNodes)
            {
                if (node.LocalName == attributeName &&
                    node.InnerText == attributeValue)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsSysAttr(string attributeName)
        {
            // Find the system attributes first
            XmlNodeList sys = m_doc.GetElementsByTagName("systemattributes");
            if (0 == sys.Count)
            {
                // This actually indicates a fairly large problem (corrupt 
                // attribute file)
                return false;
            }

            foreach (XmlNode node in sys[0].ChildNodes)
            {
                if (node.LocalName == attributeName)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsSysAttr(string attributeName, string attributeValue)
        {
            return ContainsAttribute("systemattributes", attributeName, attributeValue);
        }

        public bool ContainsUserAttr(string attributeName)
        {
            // Find the system attributes first
            XmlNodeList sys = m_doc.GetElementsByTagName("userattributes");
            if (0 == sys.Count)
            {
                // This actually indicates a fairly large problem (corrupt 
                // attribute file)
                return false;
            }

            foreach (XmlNode node in sys[0].ChildNodes)
            {
                if (node.LocalName == attributeName)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsUserAttr(string attributeName, string attributeValue)
        {
            return ContainsAttribute("userattributes", attributeName, attributeValue);
        }

        public static OSBLEFile CreateFromExisting(string dataFullPath, string attrFullPath)
        {
            return new OSBLEFile(dataFullPath, attrFullPath);
        }

        public string DataFileName
        {
            get { return m_dataFileName; }
        }

        /// <summary>
        /// Deletes all system attributes with the specified name.
        /// </summary>
        public void DeleteSysAttrs(string name)
        {
            for (int i = 0; i < m_sys.ChildNodes.Count; i++)
            {
                if (m_sys.ChildNodes[i].LocalName == name)
                {
                    m_sys.RemoveChild(m_sys.ChildNodes[i]);
                    i--;
                }
            }
        }

        /// <summary>
        /// Gets a system attribute by name. If the attribute does not exist then 
        /// null is returned. However, if the attribute does exist but has a null 
        /// value, then null is also returned. To check to see if a system 
        /// attribute exists when you don't care about the value, use the 
        /// <see cref="ContainsSysAttr(string attributeName)"/> function instead.
        /// IMPORTANT NOTE: This function is going to be made private. All system 
        /// attributes will be exposed through through properties in this class.
        /// </summary>
        public string GetSysAttr(string attrName)
        {
            // Search through all children and try to find a child node with 
            // a matching name
            foreach (XmlNode child in m_sys.ChildNodes)
            {
                if (child.LocalName == attrName)
                {
                    return child.InnerText;
                }
            }

            // Coming here implies that the attribute doesn't exist
            return null;
        }

        /// <summary>
        /// Gets a value indicating whether the set of attributes has been modified 
        /// since the last save. This value cannot be set directly. Use the 
        /// "SaveAttrs" method to write changes to disk and reset this value to 
        /// false.
        /// </summary>
        public bool Modified
        {
            get { return m_modified; }
        }

        public byte[] ReadAllBytes()
        {
            return System.IO.File.ReadAllBytes(m_dataFileName);
        }

        public string[] ReadAllLines()
        {
            return System.IO.File.ReadAllLines(m_dataFileName);
        }

        public void SaveAttrs()
        {
            m_doc.Save(m_attrFileName);
            m_modified = false;
        }

        /// <summary>
        /// Sets a system attribute with the specified name and value. If an 
        /// attribute with the specified name already exists then it will 
        /// be overwritten.
        /// </summary>
        public void SetSysAttr(string attrName, string attrValue)
        {
            // We're about to modify
            m_modified = true;

            // Search through all children and try to find a child node with 
            // a matching name
            foreach (XmlNode child in m_sys.ChildNodes)
            {
                if (child.LocalName == attrName)
                {
                    // Replace existing value
                    child.InnerText = attrValue;
                    return;
                }
            }
            
            // Coming here implies that we need to add a new node
            XmlElement elem = m_doc.CreateElement(attrName);
            elem.InnerText = attrValue;
            m_sys.AppendChild(elem);
        }
    }
}
