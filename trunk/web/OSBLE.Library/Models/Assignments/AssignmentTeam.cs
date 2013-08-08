using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.FileSystem;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.Assignments
{
    public class AssignmentTeam : IAssignmentTeam
    {
        private DateTime? _submissionTime;
        private bool _submissionTimeAccessed = false;

        [Key]
        [Column(Order=0)]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        [Key]
        [Column(Order=1)]
        public int TeamID { get; set; }

        [ForeignKey("TeamID")]
        public virtual Team Team { get; set; }

        /// <summary>
        /// Gets the last time the AssignmentTeam submitted a file.  Note that
        /// receiving a non-null value does not guarantee that all deliverables 
        /// have been submitted.
        /// </summary>
        /// <param name="renew">Defaults to false.  If true, will return a fresh value.  
        /// Otherwise, will return a cached value if possible.
        /// </param>
        /// <returns></returns>
        public DateTime? GetSubmissionTime(bool renew = false)
        {
            //do we need an update?
            if (renew == true || _submissionTimeAccessed == false)
            {
                _submissionTimeAccessed = true;
                OSBLEDirectory submission = Directories.GetAssignmentSubmission((int)this.Assignment.CourseID, this.AssignmentID, this.TeamID);
                _submissionTime = submission.GetSubmissionTime();
            }
            return _submissionTime;
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            
        }
    }
}