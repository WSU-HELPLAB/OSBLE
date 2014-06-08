using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace OSBLE.Models.Services.Uploader
{
    [DataContract]
    public class FileListing : AbstractListing
    {
        [DataMember]
        public string FileUrl
        {
            get;
            set;
        }
    }
}