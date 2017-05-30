using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;
using System.Linq;
using System.Security.Cryptography;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using OSBLE.Models.Courses;
using System.Runtime.Serialization;
using System.IO;
using OSBLE.Interfaces;

namespace OSBLE.Models.Users
{
    [Serializable]
    [DataContract]
    public class UserProfile : IModelBuilderExtender, IUser
    {
        [DataMember]
        [Required]
        [Key]
        public int ID { get; set; }

        [DataMember]
        public string UserName { get; set; }

        public string Password { get; set; }

        [DataMember]
        public string AuthenticationHash { get; set; }

        [DataMember]
        public bool IsApproved { get; set; }

        [DataMember]
        public int SchoolID { get; set; }

        [DataMember]
        public virtual School School { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public string Identification { get; set; }

        [DataMember]
        public bool IsAdmin { get; set; }

        [DataMember]
        public bool CanCreateCourses { get; set; }

        [DataMember]
        public int DefaultCourse { get; set; }

        [IgnoreDataMember]
        public virtual ProfileImage ProfileImage { get; set; }

        // User E-mail Notification Settings

        [DataMember]
        public bool EmailAllNotifications { get; set; }

        [DataMember]
        public bool EmailSelfActivityPosts { get; set; }

        /// <summary>
        /// If set, will email all activity feed posts to the users
        /// </summary>
        [DataMember]
        public bool EmailAllActivityPosts { get; set; }

        /// <summary>
        /// If set, will email any new discussion posts
        /// </summary>
        [DataMember]
        public bool EmailNewDiscussionPosts { get; set; }

        // needed by IModelBuilderExtender
        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
        }

        // extra items need by Dapper, and OSBLEPlus.Services, these are implemented as wrapper items

        /// <summary>
        /// Use ID, this is for the IUser interface
        /// </summary>
        [NotMapped]
        public int IUserId
        {
            get { return ID; }
            set { ID = value; }
        }

        /// <summary>
        /// Gets and sets the Abstract Course ID for the user. Necessary for the Create method in Roster Controller (will tell the user that Course Role is required even if there is a Course Role selected)
        /// </summary>
        [NotMapped]
        public CourseRole AbstractCourseID
        {
            get { return AbstractCourseID; }
            set { AbstractCourseID = value; }
        }

        /// <summary>
        /// Use UserName, this is for the IUser interface
        /// </summary>
        [NotMapped]
        public string Email
        {
            get { return UserName; }
            set { UserName = value; }
        }

        /// <summary>
        /// The returns the FirstName and the LastName of the user with a space between
        /// </summary>
        [NotMapped]
        public string FullName
        {
            get { return FirstName + " " + LastName; }
        }

        /// <summary>
        /// Use SchoolID, this is for the IUser interface
        /// </summary>
        [NotMapped]
        public int ISchoolId
        {
            get { return SchoolID; }
            set { SchoolID = value; }
        }

        /// <summary>
        /// Use DefaultCourse, this is for the IUser interface
        /// </summary>
        [NotMapped]
        public int IDefaultCourseId
        {
            get { return DefaultCourse; }
            set { DefaultCourse = value; }
        }

        /// <summary>
        /// This is for the IUser interface
        /// </summary>
        [NotMapped]
        public ICourse DefalutCourse { get; set; }   // Mis-spelled but leaving it for all the references in Services

        public enum sortEmailBy
        {
            POSTED = 0,
            CONTEXT = 1,
            FROM = 2,
            SUBJECT = 3
        }

        public UserProfile()
            : base()
        {
            IsAdmin = false;
            CanCreateCourses = false;
            DefaultCourse = 0;
            Password = "";
            AuthenticationHash = "";
            IsApproved = false;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="up"></param>
        public UserProfile(UserProfile up)
            : base()
        {
            CanCreateCourses = up.CanCreateCourses;
            DefaultCourse = up.DefaultCourse;
            EmailAllNotifications = up.EmailAllNotifications;
            EmailAllActivityPosts = up.EmailAllActivityPosts;
            EmailSelfActivityPosts = up.EmailSelfActivityPosts;
            EmailNewDiscussionPosts = up.EmailNewDiscussionPosts;
            FirstName = up.FirstName;
            ID = up.ID;
            Identification = up.Identification;
            IsAdmin = up.IsAdmin;
            IsApproved = up.IsApproved;
            LastName = up.LastName;
            Password = up.Password;
            School = up.School;
            SchoolID = up.SchoolID;
            UserName = up.UserName;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }

        public string LastAndFirst()
        {
            return string.Format("{0}, {1}", LastName, FirstName);
        }

        /// <summary>
        /// This is the only function that should be used to display users names. By default it displays the name as "FirstName LastName" 
        /// </summary>
        /// <param name="AbstractRoleId">The AbstroleRoleId of the current user who will see this name</param>
        /// <param name="LastThenFirst">This is an optional boolean parameter that should be sent in as true when you want names displayed in "LastName, FirstName" format.</param>
        /// <param name="AssignmentHasAnonymousPosts">This is an optional boolean parameter that should be sent in when DisplayName is used within a specific assignment scenario (to mask users if anonymous settings turned on) Simply pass in <see cref="Assignment.DiscussionSettings.HasAnonymousPosts"/></param>
        /// <returns></returns>
        public string DisplayName(int AbstractRoleId, bool? FirstThenLast = null, bool? AssignmentHasAnonymousPosts = null, int? ProfileId = 0)
        {
            string returnValue = "";
            bool isSelf = ProfileId == this.ID ? true : false;
            if ((AssignmentHasAnonymousPosts != null && AssignmentHasAnonymousPosts == true)
                || (AbstractRoleId == (int)CourseRole.CourseRoles.Observer && !isSelf))
            {
                Random number = new Random();
                returnValue = string.Format("Anonymous {0}", number.Next(2000)); //Possibly change the Random number to the event number, like it is on the Feed
            }
            else if (FirstThenLast != null && FirstThenLast == true)
            {
                returnValue = this.ToString();
            }
            else
            {
                returnValue = this.LastAndFirst();
            }
            return returnValue;
        }

        public string DisplayName(CourseUser viewingUser)
        {
            return DisplayName(viewingUser.AbstractRoleID, true, false, viewingUser.UserProfileID);
        }

        public void SetProfileImage(System.Drawing.Bitmap bmp)
        {
            if (null == bmp)
            {
                if (null == ProfileImage)
                {
                    ProfileImage = new ProfileImage();
                }
                ProfileImage.Picture = null;
                return;
            }

            if (ProfileImage == null)
            {
                ProfileImage = new ProfileImage();
            }
            MemoryStream stream = new MemoryStream();
            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            ProfileImage.Picture = stream.ToArray();
        }

        /// <summary>
        /// Converts a clear-text password into its encrypted form
        /// </summary>
        /// <param name="rawPassword"></param>
        /// <returns></returns>
        public static string GetPasswordHash(string rawPassword)
        {
            SHA256Managed algorithm = new SHA256Managed();

            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] unhashedPassword = System.Text.Encoding.UTF8.GetBytes(rawPassword);
            byte[] hashedBytes = sha.ComputeHash(unhashedPassword);
            string hashedPassword = System.Text.Encoding.UTF8.GetString(hashedBytes);
            return hashedPassword;
        }

        /// <summary>
        /// Validates the supplied user/pass combo.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns>True if the combo is valid, false otherwise</returns>
        public static bool ValidateUser(UserProfile profile)
        {
            return ValidateUser(profile.UserName, profile.Password);
        }

        /// <summary>
        /// Validates the supplied user/pass combo.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>True if the combo is valid, false otherwise</returns>
        public static bool ValidateUser(string userName, string password, bool encrypted = false)
        {
            int count = 0;
            using (OSBLEContext db = new OSBLEContext())
            {
                string hashedPassword = !encrypted ? UserProfile.GetPasswordHash(password) : password;
                count = (from user in db.UserProfiles
                         where
                         user.UserName.CompareTo(userName) == 0
                         &&
                         user.Password.CompareTo(hashedPassword) == 0
                         select user
                                 ).Count();
            }
            if (count == 1)
            {
                return true;
            }
            return false;
        }
    }
}