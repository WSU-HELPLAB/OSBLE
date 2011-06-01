using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace OSBLE.Models.Services.Uploader
{
    //the kitchen sink approach to making sure that objects will transfer correctly over the
    //wire.
    [DataContract]
    [ServiceContract]
    [ServiceKnownType(typeof(FileListing))]
    [ServiceKnownType(typeof(DirectoryListing))]
    [KnownType(typeof(FileListing))]
    [KnownType(typeof(DirectoryListing))]
    public abstract class AbstractListing
    {
        /// <summary>
        /// The file's name
        /// </summary>
        [DataMember]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// The last time that the file was modified
        /// </summary>
        [DataMember]
        public DateTime LastModified
        {
            get;
            set;
        }

        /// <summary>
        /// The order in which the file should be displayed
        /// </summary>
        [DataMember]
        public int SortOrder
        {
            get;
            set;
        }
    }
}