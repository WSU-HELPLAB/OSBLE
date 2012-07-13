using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Linq.Expressions;

namespace OSBLE.Models.FileSystem
{
    public interface IFileSystem
    {
        IEnumerable<string> AllFiles();
        IEnumerable<string> File(string name);
        IEnumerable<string> File(Func<string, bool> predicate);
        IFileSystem Directory(string name);
        string GetPath();
    }
}
