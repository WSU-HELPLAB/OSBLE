using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.FileSystem
{
    class EmptyFilePathDecorator : IFileSystem
    {
        string IFileSystem.GetPath()
        {
            return "";
        }

        public FileCollection AllFiles()
        {
            throw new NotImplementedException();
        }

        public FileCollection File(string name)
        {
            throw new NotImplementedException();
        }

        public FileCollection File(Func<string, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public IFileSystem Directory(string name)
        {
            throw new NotImplementedException();
        }

        public bool AddFile(string fileName, byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool AddFile(string fileName, System.IO.Stream data)
        {
            throw new NotImplementedException();
        }
    }
}
