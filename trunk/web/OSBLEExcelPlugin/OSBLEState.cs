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

        /// <summary>
        /// Dictionary that maps a course ID to an array of users for that course. This 
        /// collection is empty by default and rosters have to be refreshed individually 
        /// per course through the <see cref="RefreshRosterAsync"/> function.
        /// </summary>
        private Dictionary<int, CourseUser[]> m_rosters = new Dictionary<int, CourseUser[]>();
        
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

        /*
        /// <summary>
        /// Synchronous roster retrieval for a course with the specified ID.
        /// </summary>
        private CourseUser[] GetCourseRoster(int courseID)
        {
            // First login
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            string authToken = null;
            try
            {
                authToken = authClient.ValidateUser(m_user, m_pass);
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                authClient.Close();
                m_onComplete(this, new OSBLEStateEventArgs(false,
                    "Could not connect to the OSBLE server. " +
                    "Please contact support if this problem persists."));
                return null;
            }
            authClient.Close();
            authClient = null;

            if (string.IsNullOrEmpty(authToken))
            {
                m_onComplete(this, new OSBLEStateEventArgs(false,
                    "Could not log in to OSBLE. " +
                    "Please check your user name and password."));
                return null;
            }

            // Now get the roster list of courses
            OsbleServiceClient osc = new OsbleServiceClient();
            m_courses = osc.GetCourses(authToken);

            return osc.GetCourseRoster(courseID, authToken);
        }
        */

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
            string authToken = null;
            try
            {
                authToken = authClient.ValidateUser(m_user, m_pass);
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                authClient.Close();
                m_onComplete(this, new OSBLEStateEventArgs(false,
                    "Could not connect to the OSBLE server. " +
                    "Please contact support if this problem persists."));
                return;
            }
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

    internal class OSBLEStateEventArgs : EventArgs
    {
        private bool m_success = false;

        private string m_message;

        private OSBLEState m_state = null;

        public OSBLEStateEventArgs(bool success, string message)
        {
            m_message = message;
            m_success = success;
        }

        public OSBLEStateEventArgs(bool success, string message, OSBLEState state)
        {
            m_message = message;
            m_success = success;
            m_state = state;
        }

        public static readonly OSBLEStateEventArgs Empty = new OSBLEStateEventArgs(true, string.Empty);

        public string Message
        {
            get
            {
                return m_message;
            }
        }

        public OSBLEState State
        {
            get
            {
                return m_state;
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
