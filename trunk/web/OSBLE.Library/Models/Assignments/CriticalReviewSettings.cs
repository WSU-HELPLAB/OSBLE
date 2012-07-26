using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace OSBLE.Models.Assignments
{
    public class CriticalReviewSettings
    {
        [Key]
        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        public CriticalReviewSettings(CriticalReviewSettings other)
        {
            if (other == null)
            {
                return;
            }

            this.AssignmentID = other.AssignmentID;
            this.AnonymizeAuthorToReviewer = other.AnonymizeAuthorToReviewer;
            this.AnonymizeReviewerToAuthor = other.AnonymizeReviewerToAuthor;
            this.AnonymizeReviewerToReviewers = other.AnonymizeReviewerToReviewers;
            this.AllowReviewersToDownload = other.AllowReviewersToDownload;
        }

        public CriticalReviewSettings()
        {
            AnonymizeAuthorToReviewer = false;
            AnonymizeReviewerToAuthor = false;
            AnonymizeReviewerToReviewers = false;
            AllowReviewersToDownload = false;
        }
        [Display(Name = "Anonymize Author to Reviewers")]
        public bool AnonymizeAuthorToReviewer { get; set; }

        [Display(Name = "Anonymize Reviewers to Author")]
        public bool AnonymizeReviewerToAuthor { get; set; }

        [Display(Name = "Anonymize Reviewers to other Reviewers in reviewed document")]
        public bool AnonymizeReviewerToReviewers { get; set; }

        [Display(Name = "Allow reviewers to access the reviewed document after publish")]
        public bool AllowReviewersToDownload { get; set; }
        

    }
}
