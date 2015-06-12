using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace OSBLE.Models.Services.Uploader
{
    /// <summary>
    /// (AC):
    /// The motivation for this is to provide a representation of the "up one level" folder icon
    /// in a directory listing.  However, as this is sort of a client feature, I'm a little torn
    /// over where exactly this should be placed.  I decided to place it on the server so that 
    /// the client wouldn't have to modify the base collection, thus making client-side code
    /// much cleaner.
    /// </summary>
    [DataContract]
    public class ParentDirectoryListing : DirectoryListing
    {
        public ParentDirectoryListing()
        {
            this.Name = "...";
        }
    }
}