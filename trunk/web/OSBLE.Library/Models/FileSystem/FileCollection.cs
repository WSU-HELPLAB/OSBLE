﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Ionic.Zip;
using System.IO;

namespace OSBLE.Models.FileSystem
{
    public class FileCollection : IEnumerable<string>, ICollection<string>
    {
        // 5-15-13 E.O.
        // Changed this from private to protected. If another developer has a 
        // convincing reason why this shouldn't be done then they need to 
        // contact me.
        protected List<string> _fileNames = new List<string>();
        
        public string Directory { get; private set; }


        public FileCollection(string directoryRoot)
        {
            Directory = directoryRoot;
        }

        /// <summary>
        /// Converts the collection of files into a zip stream
        /// </summary>
        /// <returns></returns>
        public Stream ToZipStream()
        {
            MemoryStream stream = new MemoryStream();

            //add all files (except other zips) in our list to the zip files
            using (ZipFile zip = new ZipFile())
            {
                foreach (string name in _fileNames)
                {
                    //ignore zip files
                    if (Path.GetExtension(name) != "zip")
                    {
                        try
                        {
                            zip.AddFile(name, "");
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                try
                {
                    zip.Save(stream);
                }
                catch (Exception)
                {
                }
            }
            
            //reset stream position
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Returns a byte[] for each individual file.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, byte[]> ToBytes()
        {
            Dictionary<string, byte[]> bytes = new Dictionary<string, byte[]>();
            foreach (string name in _fileNames)
            {
                byte[] fileBytes = File.ReadAllBytes(name);
                string rootName = Path.GetFileName(name);
                bytes[rootName] = fileBytes;
            }
            return bytes;
        }

        /// <summary>
        /// Returns a stream for each individual file
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, Stream> ToStreams()
        {
            Dictionary<string, Stream> streams = new Dictionary<string, Stream>();
            foreach (string name in _fileNames)
            {
                FileStream fs = File.OpenRead(name);
                MemoryStream ms = new MemoryStream();
                fs.CopyTo(ms);
                fs.Close();
                ms.Position = 0;
                streams[name] = ms;
            }
            return streams;
        }

        /// <summary>
        /// Deletes the collection of files from the file system.
        /// </summary>
        /// <returns>The number of files deleted</returns>
        public virtual int Delete()
        {
            int removeCounter = 0;
            foreach (string name in _fileNames)
            {
                try
                {
                    File.Delete(name);
                    removeCounter++;
                }
                catch (Exception)
                {
                }
            }
            return removeCounter;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _fileNames.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fileNames.GetEnumerator();
        }

        public void Add(string item)
        {
            _fileNames.Add(item);
        }

        public void Clear()
        {
            _fileNames.Clear();
        }

        public bool Contains(string item)
        {
            return _fileNames.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _fileNames.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                return _fileNames.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Remove(string item)
        {
            return _fileNames.Remove(item);
        }
    }
}
