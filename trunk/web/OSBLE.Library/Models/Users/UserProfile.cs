using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;
using System.Linq;
using System.Security.Cryptography;
using System;
using OSBLE.Models.Courses;
using System.Runtime.Serialization;
using System.IO;

namespace OSBLE.Models.Users
{
    [Serializable]
    [DataContract]
    public class UserProfile : IModelBuilderExtender
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

        private ProfileImage _profileImage { get; set; }

        [IgnoreDataMember]
        public virtual ProfileImage ProfileImage
        {
            get
            {
                return _profileImage;
            }
            set
            {
                _profileImage = value;
            }
        }

        // User E-mail Notification Settings

        [DataMember]
        public bool EmailAllNotifications { get; set; }

        /// <summary>
        /// If set, will email all activity feed posts to the users
        /// </summary>
        [DataMember]
        public bool EmailAllActivityPosts { get; set; }

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
            this.CanCreateCourses = up.CanCreateCourses;
            this.DefaultCourse = up.DefaultCourse;
            this.EmailAllNotifications = up.EmailAllNotifications;
            this.FirstName = up.FirstName;
            this.ID = up.ID;
            this.Identification = up.Identification;
            this.IsAdmin = up.IsAdmin;
            this.IsApproved = up.IsApproved;
            this.LastName = up.LastName;
            this.Password = up.Password;
            this.School = up.School;
            this.SchoolID = up.SchoolID;
            this.UserName = up.UserName;
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
        public string DisplayName(int AbstractRoleId, bool? FirstThenLast = null, bool? AssignmentHasAnonymousPosts = null)
        {
            string returnValue = "";
            if ((AssignmentHasAnonymousPosts != null && AssignmentHasAnonymousPosts == true)
                || AbstractRoleId == (int)CourseRole.CourseRoles.Observer)
            {
                returnValue = string.Format("Anonymous {0}", this.ID);
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

        public void SetProfileImage(System.Drawing.Bitmap bmp)
        {
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
        public static bool ValidateUser(string userName, string password)
        {
            int count = 0;
            using (OSBLEContext db = new OSBLEContext())
            {
                string hashedPassword = UserProfile.GetPasswordHash(password);
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

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
        }
    }
}