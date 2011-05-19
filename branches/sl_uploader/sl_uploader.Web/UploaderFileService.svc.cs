using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Web;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace sl_uploader.Web
{
    public class OsbleFileInfo : IComparable
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
            OsbleFileInfo other = obj as OsbleFileInfo;
            if (other != null)
            {
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
    public class UploaderFileService
    {
        private string filePath;
        public UploaderFileService()
        {
            filePath = HttpContext.Current.Server.MapPath("Files");
        }
        
        [OperationContract]
        public IEnumerable<OsbleFileInfo> GetFileList()
        {
            List<OsbleFileInfo> files = new List<OsbleFileInfo>();
            foreach (string item in Directory.GetFiles(filePath))
            {
                OsbleFileInfo f = new OsbleFileInfo();
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

        [OperationContract]
        public void UploadFile(string fileName, byte[] data)
        {
            string file = Path.Combine(filePath, Path.GetFileName(fileName));
            using (FileStream fs = new FileStream(file, FileMode.Create))
            {
                fs.Write(data, 0, (int)data.Length);
            }
        }
    }
}
