using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Web;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace FileUploader.Web
{
    public class OsbleSyncInfo : IComparable
    {
        public string FileName
        {
            get;
            set;
        }

        public DateTime LastModified
        {
            get;
            set;
        }

        //public enum LabelImage { File, Folder };

        private int image;
        public int Image
        {
            get
            {
                return image;
            }
            set
            {
                image = value;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj is OsbleSyncInfo)
            {
                OsbleSyncInfo other = obj as OsbleSyncInfo;
                return FileName.CompareTo(other.FileName);
            }
            else
            {
                return -1;
            }
        }
    }

    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class FileSyncService 
    {
        public string filePath;
        public string currentpath;

        public FileSyncService()
        {
            filePath = HttpContext.Current.Server.MapPath("Files");
            currentpath = filePath;
        }
        
        [OperationContract]
        public IEnumerable<OsbleSyncInfo> GetFileList(string relativepath)
        {
            List<OsbleSyncInfo> files = new List<OsbleSyncInfo>();
            string currentpath = Path.Combine(filePath, relativepath);
            if (filePath != currentpath)
            {
                OsbleSyncInfo b = new OsbleSyncInfo();
                b.FileName = "..";
                files.Add(b);
            }

            // Assigns all the directories to the ListBox
            foreach (string folder in Directory.EnumerateDirectories(currentpath))
            {
                OsbleSyncInfo d  = new OsbleSyncInfo();
                d.FileName = folder.Substring(folder.LastIndexOf('\\') + 1);
                d.Image = 1;
                d.LastModified = File.GetLastWriteTime(folder);
                files.Add(d);
            }

            foreach (string file in Directory.GetFiles(currentpath))
            {
                OsbleSyncInfo f = new OsbleSyncInfo();
                f.FileName = Path.GetFileName(file);
                f.Image = 0;
                f.LastModified = File.GetLastWriteTime(file);
                files.Add(f);
            }
            return files;
        }

        [OperationContract]
        public string GetFileUrl(string fileName)
        {
            //probably need to make sure that this string is web-accessible, but this is fine for testing
            string file = Path.Combine(filePath, Path.GetFileName(fileName));
            return file;
        }

        [OperationContract]
        public void SyncFile(string fileName, byte[] data)
        {
            string file = Path.Combine(filePath, fileName);
            using (FileStream fs = new FileStream(file, FileMode.Create))
            {
                fs.Write(data, 0, (int)data.Length);
            }
        }

        [OperationContract]
        public void createDir(string folderName)
        {
            string file = Path.Combine(filePath, folderName);
            Directory.CreateDirectory(file);

            
           
        }
    }
}
