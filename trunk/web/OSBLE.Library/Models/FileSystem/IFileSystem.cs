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
        FileCollection AllFiles();
        FileCollection File(string name);
        FileCollection File(Func<string, bool> predicate);
        IFileSystem Directory(string name);
        string GetPath();
    }
}
