﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using OSBLE.Interfaces;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.Helpers;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class UserProfilesController : ApiController
    {
        /// <summary>
        /// Logs the user in with the passed username and hashed password
        /// </summary>
        /// <param name="e">Username</param>
        /// <param name="hp">Hashed Password</param>
        /// <returns>HttpResponeMessage</returns>
        [HttpGet]
        public HttpResponseMessage Login(string e, string hp)
        {
            if (!UserDataAccess.ValidateUser(e, hp))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized };

            var user = UserDataAccess.GetByName(e);
            if (user == null)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized };

            var auth = new Authentication();
            var hash = auth.LogIn(user);
            UserDataAccess.LogUserTransaction(user.IUserId, DateTime.Now);

            return new HttpResponseMessage
            {
                Content = new StringContent(hash),
                StatusCode = HttpStatusCode.OK
            };
        }

        /// <summary>
        /// Get's the active user from an authentication key
        /// </summary>
        /// <param name="a">Authentication Key</param>
        /// <returns>IUser which has the following POCO format
        ///  int IUserId { get; set; }
        /// string Email { get; set; }
        /// string FirstName { get; set; }
        /// string LastName { get; set; }
        /// string FullName { get; }
        /// int ISchoolId { get; set; }
        /// string Identification { get; set; }
        /// bool IsAdmin { get; set; }
        /// bool EmailAllActivityPosts { get; set; }
        /// bool EmailSelfActivityPosts { get; set; }
        /// bool EmailAllNotifications { get; set; }
        /// bool EmailNewDiscussionPosts { get; set; }
        /// int IDefaultCourseId { get; set; }
        /// ICourse DefalutCourse { get; set; }
        /// string DisplayName(CourseUser viewingUser);
        /// </returns>
        public IUser GetActiveUser(string a)
        {
            return (new Authentication()).GetActiveUser(a);
        }

        /// <summary>
        /// Checks if the Authentication Key passed in is valid
        /// </summary>
        /// <param name="a">Authentication Key</param>
        /// <returns>bool</returns>
        [HttpGet]
        public bool IsValidKey(string a)
        {
            var auth = new Authentication();
            if (auth.IsValidKey(a))
            {
                UserDataAccess.LogUserTransaction(auth.GetActiveUserId(a), DateTime.Now);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the Courses the User is associated with in the database
        /// </summary>
        /// <param name="a">Authentication Key</param>
        /// <returns>List of ProfileCourse which has the following POCO format
        /// public int Id { get; set; }
        /// public int Number { get; set; }
        /// public string NamePrefix { get; set; }
        /// public string Description { get; set; }
        /// public string Name { get; set; }
        /// public string Semester { get; set; }
        /// public int Year { get; set; }
        /// public DateTime StartDate { get; set; }
        /// public DateTime EndDate { get; set; }
        /// </returns>
        public List<ProfileCourse> GetCoursesForUser(string a)
        {
            var auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                return UserDataAccess.GetProfileCoursesForUser(auth.GetActiveUserId(a), DateTime.Today);
            }

            return null;
        }

        /// <summary>
        /// Gets the most recent Social Activity
        /// </summary>
        /// <param name="a">Authentication Key</param>
        /// <returns>DateTime value</returns>
        [HttpGet]
        public DateTime MostRecentSocialActivity(string a)
        {
            var lastSocialActivity = DateTime.MinValue;

            var auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                return UserDataAccess.GetMostRecentSocialActivityForUser(auth.GetActiveUserId(a));
            }

            return lastSocialActivity;
        }

        /// <summary>
        /// Submits an error log from the local user's machine
        /// </summary>
        /// <param name="request">
        /// request is a LocalerrorLogRequest that has the following POCO format
        /// public string AuthToken { get; set; }
        /// public LocalErrorLog Log { get; set; }
        /// </param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage SubmitLocalErrorLog(LocalErrorLogRequest request)
        {
            var auth = new Authentication();

            if (!auth.IsValidKey(request.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            request.Log.SenderId = auth.GetActiveUserId(request.AuthToken);

            return new HttpResponseMessage
            {
                StatusCode = Posts.SubmitLocalErrorLog(request.Log) > 0
                           ? HttpStatusCode.OK
                           : HttpStatusCode.InternalServerError
            };
        }
    }
}