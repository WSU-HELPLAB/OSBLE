using System.Collections.Generic;

namespace OSBLE.Models
{
    public class SubmissionActivitySetting : AssignmentActivity
    {
        public class Submission
        {
            /*
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
            }*/
        }

        public ICollection<string> RequiredSubbmisions { get; set; }
    }
}