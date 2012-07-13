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

        public IEnumerable<string> AllFiles()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> File(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> File(Func<string, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public IFileSystem Directory(string name)
        {
            throw new NotImplementedException();
        }
    }
}
