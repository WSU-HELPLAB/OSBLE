using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLE.Models.FileSystem;
namespace OSBLE.UnitTests
{

    [TestClass]
    public class FileSystemTest
    {
        [TestMethod]
        public void FileSystemTest_Traverse1()
        {
            OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem("d:\\");
            IFileSystem subDirectory = fs.Directory("acarter").Directory("temp");
            FileCollection fc = subDirectory.AllFiles();
        }
    }
}
