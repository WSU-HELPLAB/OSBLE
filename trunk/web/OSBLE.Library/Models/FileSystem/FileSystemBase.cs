using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Linq.Expressions;

namespace OSBLE.Models.FileSystem
{
    public abstract class FileSystemBase : IFileSystem
    {
        protected IFileSystem PathBuilder { get; set; }

        public FileSystemBase(IFileSystem pathBuilder)
        {
            PathBuilder = pathBuilder;
        }

        public abstract string GetPath();

        /// <summary>
        /// Adds a file to file system
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool AddFile(string fileName, byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(data, 0, data.Length);
            ms.Position = 0;
            return AddFile(fileName, ms);
        }

        public virtual bool AddFile(string fileName, Stream data)
        {
            string path = GetPath();
            string filePath = Path.Combine(path, fileName);
            if (System.IO.Directory.Exists(path) == false)
            {
                System.IO.Directory.CreateDirectory(path);
            }
            bool retVal = true;
            try
            {
                FileStream output = System.IO.File.Open(filePath, FileMode.Create);
                data.CopyTo(output);
                output.Close();
            }
            catch (Exception)
            {
                retVal = false;
            }
            return retVal;
        }

        public virtual FileCollection AllFiles()
        {
            string path = GetPath();
            FileCollection collection = new FileCollection(path);
            List<string> files = new List<string>();
            if (System.IO.Directory.Exists(path))
            {
                files = System.IO.Directory.GetFiles(path).ToList();
                foreach (string file in files)
                {
                    collection.Add(file);
                }
            }
            return collection;
        }

        public FileCollection File(string name)
        {
            return File(s => s == name);
        }

        public virtual FileCollection File(Func<string, bool> predicate)
        {
            string path = GetPath();
            FileCollection collection = new FileCollection(path);
            if (System.IO.Directory.Exists(path))
            {
                string[] allFiles = System.IO.Directory.GetFiles(path);
                foreach (string file in allFiles)
                {
                    string fileName = Path.GetFileName(file);
                    if (predicate(fileName) == true)
                    {
                        collection.Add(file);
                    }
                }
                
            }
            return collection;
        }

        public virtual IFileSystem Directory(string name)
        {
            return new GenericFilePath(this, name);
        }


        private class GenericFilePath : FileSystemBase
        {
            public string _localPath = "";

            public GenericFilePath(IFileSystem pathBuilder, string localPath)
                : base(pathBuilder)
            {
                _localPath = localPath;
            }

            public override string GetPath()
            {
                string returnPath = Path.Combine(PathBuilder.GetPath(), _localPath);
                return returnPath;
            }
        }
    }
}
