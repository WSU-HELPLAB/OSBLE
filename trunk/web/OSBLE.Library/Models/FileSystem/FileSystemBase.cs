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

        public IEnumerable<string> AllFiles()
        {
            string path = GetPath();
            List<string> files = new List<string>();
            if (System.IO.Directory.Exists(path))
            {
                files = System.IO.Directory.GetFiles(path).ToList();
            }
            return files;
        }

        public IEnumerable<string> File(string name)
        {
            return File(s => s == name);
        }

        public IEnumerable<string> File(Func<string, bool> predicate)
        {
            string path = GetPath();
            List<string> files = new List<string>();
            if (System.IO.Directory.Exists(path))
            {
                string[] allFiles = System.IO.Directory.GetFiles(path);
                foreach (string file in allFiles)
                {
                    string fileName = Path.GetFileName(file);
                    if (predicate(fileName) == true)
                    {
                        files.Add(file);
                    }
                }
                
            }
            return files;
        }

        public IFileSystem Directory(string name)
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
