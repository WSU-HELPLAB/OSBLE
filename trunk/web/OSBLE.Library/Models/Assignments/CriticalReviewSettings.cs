using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class CriticalReviewSettings
    {
        [Key]
        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        public CriticalReviewSettings()
        {
            AnonymizeAuthorToReviewer = false;
            AnonymizeReviewerToAuthor = false;
            AnonymizeReviewerToReviewers = false;
        }
        [Display(Name="Anonymize Author to Reviewers")]
        public bool AnonymizeAuthorToReviewer;

        [Display(Name = "Anonymize Reviewers to Author")]
        public bool AnonymizeReviewerToAuthor;

        [Display(Name = "Anonymize Reviewers to other Reviewers for merged document")]
        public bool AnonymizeReviewerToReviewers;

        [Display(Name = "Allow reviewers to download the reviewed document after publish")]
        public bool AllowReviewersToDownload;
        

    }
}
