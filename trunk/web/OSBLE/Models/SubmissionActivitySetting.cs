using System;
using System.Collections.Generic;

namespace OSBLE.Models
{
    public class SubmissionActivitySetting : AssignmentActivity
    {
        public enum SubmissionType
        {
            [FileExtensions(new string[] { ".c", ".cpp", ".h", ".cs", ".py", ".java" })]
            Code,

            [FileExtensions(new string[] { ".txt" })]
            Text,

            [FileExtensions(new string[] { ".cpml" })]
            ChemProV,

            [FileExtensions(new string[] { ".xps" })]
            XPS,

            [FileExtensions(new string[] { ".wmv", ".mp4" })]
            Video,

            /// <summary>
            /// This file type is not supported by the Osble Review process and thus a Review process will not be allowed for this
            /// file
            /// </summary>
            [FileExtensions(new string[] { ".*" })]
            Other
        }

        public class FileExtensions : Attribute
        {
            private List<string> extensions;

            public List<string> Extensions
            {
                get
                {
                    return extensions;
                }
            }

            public FileExtensions(string[] fileExtensions)
            {
                this.extensions = new List<string>(fileExtensions);
            }
        }

        public class Submission
        {
            private string fileName;
            private SubmissionType submissionType;

            public string FileName
            {
                get
                {
                    return fileName;
                }
            }

            public SubmissionType SubmissionType
            {
                get
                {
                    return submissionType;
                }
            }

            public Submission(string fileName, SubmissionType submissionType)
            {
                this.fileName = fileName;
                this.submissionType = submissionType;
            }
        }

        public ICollection<string> RequiredSubbmisions { get; set; }
    }
}