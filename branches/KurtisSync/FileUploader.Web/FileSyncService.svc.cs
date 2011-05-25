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

        public FileSyncService()
        {
            filePath = HttpContext.Current.Server.MapPath("Files");
        }

        [OperationContract]
        public IEnumerable<OsbleSyncInfo> GetFileList()
        {
            List<OsbleSyncInfo> files = new List<OsbleSyncInfo>();
            foreach (string item in Directory.GetFiles(filePath))
            {
                OsbleSyncInfo f = new OsbleSyncInfo();
                f.FileName = Path.GetFileName(item);
                f.LastModified = File.GetLastWriteTimeUtc(item);
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

        //[OperationContract]
        //public byte[] GetFile(string fileName)
        //{
        //    //make sure the file has no path information
        //    string file = Path.Combine(filePath, Path.GetFileName(fileName));
        //    using (FileStream fs = new FileStream(file, FileMode.Open))
        //    {
        //        byte[] data = new byte[fs.Length];
        //        fs.Read(data, 0, (int)fs.Length);
        //        return data;
        //    }
        //}

        [OperationContract]
        public void SyncFile(string fileName, byte[] data)
        {
            string file = Path.Combine(filePath, Path.GetFileName(fileName));
            using (FileStream fs = new FileStream(file, FileMode.Create))
            {
                fs.Write(data, 0, (int)data.Length);
            }
        }



        [OperationContract]
        public void DoWork()
        {
            // Add your operation implementation here
            return;
        }

        // Add more operations here and mark them with [OperationContract]
    }
}
