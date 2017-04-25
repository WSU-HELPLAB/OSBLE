using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.FileSystem
{   
    public class MailAttachmentFilePath : OSBLEDirectory
    {
        public MailAttachmentFilePath(string path)
            : base(path) { }

        public override OSBLEDirectory GetDir(string subdirName)
        {
            throw new NotSupportedException(
                "Subdirectories are not supported within the MailAttachment directory.");
        }
    }
}
