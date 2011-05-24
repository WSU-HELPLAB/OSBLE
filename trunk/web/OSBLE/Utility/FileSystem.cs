using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models;
using System.IO;

namespace OSBLE
{
    public static class FileSystem
    {
        private static string getRootPath()
        {
            return HttpContext.Current.Server.MapPath("\\FileSystem\\");
        }

        private static string getCoursePath(Course course)
        {
            return getRootPath() + "Courses\\" + course.ID + "\\";
        }

        private static string getUserPath(UserProfile userprofile)
        {
            return getRootPath() + "Users\\" + userprofile.ID + "\\";
        }

        private static string getProfilePicturePath(UserProfile userProfile)
        {
            return getUserPath(userProfile) + "profile.jpg";
        }

        public static FileStream GetDefaultProfilePicture()
        {
            return new FileStream(HttpContext.Current.Server.MapPath("\\Content\\images\\default.jpg"), FileMode.Open, FileAccess.Read);
        }

        public static FileStream GetProfilePictureOrDefault(UserProfile userProfile)
        {
            if (File.Exists(getProfilePicturePath(userProfile))) {
                return new FileStream(getProfilePicturePath(userProfile), FileMode.Open, FileAccess.Read);
            } else {
                return GetDefaultProfilePicture();
            }
        }

        public static FileStream GetProfilePictureForWrite(UserProfile userProfile)
        {
            if(!Directory.Exists(getUserPath(userProfile))) {
                Directory.CreateDirectory(getUserPath(userProfile));
            }
            return new FileStream(getProfilePicturePath(userProfile), FileMode.Create, FileAccess.Write);
        }

        public static void DeleteProfilePicture(UserProfile userProfile)
        {
            if (File.Exists(getProfilePicturePath(userProfile)))
            {
                File.Delete(getProfilePicturePath(userProfile));
            }
        }

        private static void emptyFolder(string path)
        {
            if(Directory.Exists(path)) {
                DirectoryInfo parent = new DirectoryInfo(path);

                foreach (FileInfo fi in parent.GetFiles())
                {
                    fi.Delete();
                }

                foreach (DirectoryInfo di in parent.GetDirectories())
                {
                    emptyFolder(di.FullName);
                    di.Delete();
                }
            }
        }

        /// <summary>
        /// Never, EVER use this function.
        /// Unless you want to wipe out the filesystem. Then by all means use it.
        /// (Used in Sample Data generation on model change.)
        /// </summary>
        public static void WipeOutFileSystem()
        {
            emptyFolder(getRootPath());
        }
    }
}