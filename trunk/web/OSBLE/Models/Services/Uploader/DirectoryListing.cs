using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace OSBLE.Models.Services.Uploader
{
    [DataContract]
    public class DirectoryListing : AbstractListing
    {
        [DataMember]
        public IList<FileListing> Files
        {
            get;
            set;
        }

        [DataMember]
        public IList<DirectoryListing> Directories
        {
            get;
            set;
        }

        /*
        [DataMember]
        public DirectoryListing ParentDirectory
        {
            get;
            set;
        }
         * */

        public DirectoryListing()
            : base()
        {
            Directories = new List<DirectoryListing>();
            Files = new List<FileListing>();
        }
    }
}