// Created 5-13-13 by Evan Olds for the OSBLE project at WSU
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using OSBLEExcelPlugin.OSBLEAuthService;
using OSBLEExcelPlugin.OSBLEServices;

namespace OSBLEExcelPlugin
{
    internal class OSBLEState
    {
        private Course[] m_courses = null;
        
        private string m_user, m_pass;

        private EventHandler m_onComplete = null;

        public OSBLEState(string userName, string password)
        {
            m_user = userName;
            m_pass = password;
        }

        public Course[] Courses
        {
            get { return m_courses; }
        }

        public string Password
        {
            get { return m_pass; }
        }

        public void RefreshAsync(EventHandler onComplete)
        {
            m_onComplete = onComplete;
            ThreadStart ts = new ThreadStart(RefreshProc);
            Thread t = new Thread(ts);
            t.Start();
        }

        private void RefreshProc()
        {
            // First login
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            string authToken = authClient.ValidateUser(m_user, m_pass);
            authClient.Close();
            authClient = null;

            if (string.IsNullOrEmpty(authToken))
            {
                m_onComplete(this, new OSBLEStateEventArgs(false,
                    "Could not log in to OSBLE. " + 
                    "Please check your user name and password."));
                return;
            }

            // Now get a list of courses
            OsbleServiceClient osc = new OsbleServiceClient();
            m_courses = osc.GetCourses(authToken);

            // Make sure we got some courses
            if (null == m_courses || 0 == m_courses.Length)
            {
                m_onComplete(this, new OSBLEStateEventArgs(false,
                    "No courses were found for this user."));
                return;
            }

            // Go through the courses and find out this user's role
            List<Course> canBeGraded = new List<Course>();
            foreach (Course c in m_courses)
            {
                CourseRole cr = osc.GetCourseRole(c.ID, authToken);
                if (cr.CanGrade)
                {
                    canBeGraded.Add(c);
                }
            }
            m_courses = canBeGraded.ToArray();

            // Success if we made it this far
            m_onComplete(this, new OSBLEStateEventArgs(true, string.Empty));
        }

        public string UserName
        {
            get { return m_user; }
        }
    }

    public class OSBLEStateEventArgs : EventArgs
    {
        private bool m_success = false;

        private string m_message;

        private Stream m_stream = null;

        public OSBLEStateEventArgs(bool success, string message)
        {
            m_message = message;
            m_success = success;
        }

        public OSBLEStateEventArgs(bool success, string message, Stream stream)
        {
            m_message = message;
            m_success = success;
            m_stream = stream;
        }

        public static readonly OSBLEStateEventArgs Empty = new OSBLEStateEventArgs(true, string.Empty);

        public string Message
        {
            get
            {
                return m_message;
            }
        }

        public Stream Stream
        {
            get
            {
                return m_stream;
            }
        }

        /// <summary>
        /// Indicates whether or not the action completed successfully
        /// </summary>
        public bool Success
        {
            get
            {
                return m_success;
            }
        }
    }
}
