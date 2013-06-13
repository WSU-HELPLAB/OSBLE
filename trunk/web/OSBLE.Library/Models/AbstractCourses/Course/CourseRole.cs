using System.Collections;
using System;
using System.Collections.Generic;
namespace OSBLE.Models.Courses
{
    public class CourseRole : AbstractRole
    {
        public CourseRole()
            : base()
        {
        }

        public CourseRole(string Name, bool CanModify, bool CanSeeAll, bool CanGrade, bool CanSubmit, bool CanUploadFiles, bool Anonymized)
            : base()
        {
            this.Name = Name;
            this.CanModify = CanModify;
            this.CanSeeAll = CanSeeAll;
            this.CanGrade = CanGrade;
            this.CanSubmit = CanSubmit;
            this.CanUploadFiles = CanUploadFiles;
            this.Anonymized = Anonymized;
        }

        public CourseRole(AbstractRole copyRole)
        {
            this.Anonymized = copyRole.Anonymized;
            this.CanGrade = copyRole.CanGrade;
            this.CanModify = copyRole.CanModify;
            this.CanSeeAll = copyRole.CanSeeAll;
            this.CanSubmit = copyRole.CanSubmit;
            this.CanUploadFiles = copyRole.CanUploadFiles;
            this.ID = copyRole.ID;
            this.Name = copyRole.Name;
        }


        /// <summary>
        /// Compares class roles for ordering. Order is: Instructor -> TA -> Moderator -> Observer -> Student
        /// This function is used rather than changing the enumeration below in order to avoid database ordering issues
        /// </summary>
        public class CourseRolesInOrder : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                int studentRole = (int)CourseRole.CourseRoles.Student;
                if (x == studentRole && y != studentRole) //x is a student, so x is less.
                {
                    return 1;
                }
                else if (y == studentRole && x != studentRole) //y is a student, so x is greater.
                {
                    return -1;
                }
                else //neither x or y is a student, so normal comparer will work.
                    return x - y;
            }
        }

        public enum CourseRoles : int
        {
            //Instructor being the first one is used in RosterController so any new roles add at the end.
            //Also Note that community members magically start 6 which is one past the end of this and they will
            //need to update if you add a new role here.  (Also update any old data)
            Instructor = 1,
            TA,
            Student,
            Moderator,
            Observer,
            Withdrawn 
        }
    }
}