using OSBLE.Models.Intervention;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility.Lookups;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Dapper;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class BuildErrorDocument
    {
        public BuildErrorDocument()
        {
            ProjectName = "";
            FileName = "";
            Line = 0;
            ContentLines = new List<string>();
            ErrorItemDescription = "";
        }

        public List<string> ContentLines { get; set; }
        public string ErrorItemDescription { get; set; }
        public string FileName { get; set; }
        public int Line { get; set; }
        public string ProjectName { get; set; }
    }

    public class InterventionController : ApiController
    {
        //TODO: move these out to a config file so they can be changed on the server without rebuilding code
        private const int BuildOrExceptionErrorThreshold = 5; //the number of errors in the last "NumberOfMinutesErrorThreshold" minutes before we create a new error intervention.
        private const int NumberOfMinutesErrorThreshold = 10; //number of minutes to look back when deciding error (build or exception) time threshold

        private const int CodeLineVarianceThreshold = 7; //number of lines +/- from the error to generate suggested code
        private const int ErrorFreeMinutesThreshold = 10; //Threshold in minutes before generating a 'unanswered questions' intervention
        private const int NumberOfDaysIdleThreshold = 1; //number of days to look back without any post/reply/askforhelp activity before generating intervention                
        private const int NumberOfMinutesRefreshThreshold = 1; //threshold in CheckInterventionStatus()
        private const int SubmitAssignmentEarlyTimeThreshold = -1; //dumber of days (negative == early) - threshold in ProcessMakeAPostSubmit()
        private const int UnansweredQuestionsDaysThreshold = 10; //number of days to look back for unanswered/marked helpful posts         
        private const int InterventionRefreshThresholdInMinutes = 10; //removing it from stringconstants for now as it will be easier to republish .services when needed...

        [HttpGet]
        public int InterventionRefreshThreshold()
        {
            //removing it from stringconstants for now as it will be easier to republish .services when needed...
            //return int.Parse(StringConstants.InterventionRefreshThreshold);
            return InterventionRefreshThresholdInMinutes;
        }

        [HttpGet]
        public int GetUserSetInterventionRefreshThreshold(string authToken)
        {
            //need to check the database for this users's interventions and return true if there are new interventions since the last processing.
            var auth = new Authentication();
            if (!auth.IsValidKey(authToken))
                return InterventionRefreshThresholdInMinutes;

            int userProfileId = auth.GetActiveUserId(authToken);

            return UserSetRefreshThreshold(userProfileId);
        }

        [HttpGet]
        public bool InterventionsEnabled()
        {
            return true;
        }
        public void ProcessActivityEvent(ActivityEvent log)
        {
            switch (log.EventTypeId)
            {
                case (int)EventType.BuildEvent:
                    BuildEvent(log);
                    break;
                case (int)EventType.SubmitEvent:
                    SubmitEvent(log);
                    break;
                case (int)EventType.EditorActivityEvent:
                    EditorActivityEvent(log);
                    break;
                case (int)EventType.ExceptionEvent: //currently not implemented
                    ExceptionEvent(log);
                    break;
                case (int)EventType.CutCopyPasteEvent:
                    CutCopyPasteEvent(log);
                    break;
                /* these events are not currently utilized for analysis here
                case (int)EventType.DebugEvent: //currently not implemented
                    DebugEvent(log);
                    break;                     
                case (int)EventType.HelpfulMarkGivenEvent:
                    HelpfulMarkGivenEvent(log);
                    break;                
                case (int)EventType.Null:
                    Null(log);
                    break;
                case (int)EventType.SaveEvent:
                    SaveEvent(log);
                    break;
                case (int)EventType.AskForHelpEvent:
                    AskForHelpEvent(log);
                    break;
                 
                case (int)EventType.FeedPostEvent: //doesn't get processed here... via feedcontroller
                    FeedPostEvent(log);
                    break;
                case (int)EventType.LogCommentEvent: //doesn't get processed here... via feedcontroller
                    LogCommentEvent(log);
                    break;
                 */
                default:
                    //do nothing
                    //DefaultEvent(log);
                    break;
            }
        }

        [HttpGet]
        public bool RefreshInterventions(string authToken)
        {
            if (InterventionsEnabled())
            {
                //need to check the database for this users's interventions and return true if there are new interventions since the last processing.
                var auth = new Authentication();
                if (!auth.IsValidKey(authToken))
                    return false;

                int userProfileId = auth.GetActiveUserId(authToken);

                //first check if the user has disabled in-IDE interventions
                if (!UserEnabledInterventions(userProfileId))
                {
                    return false;
                }

                if (!UserRefreshThesholdMet(userProfileId))
                {
                    return false;
                } //go ahead and continue checking. if the threshold is not met we don't want to refresh

                //now check if the user needs an intervention refreshed.
                bool refresh = CheckInterventionStatus(userProfileId);
                if (refresh)
                {
                    //disable the refresh flag (changed it to here because this is the only method that actually refreshes the interventions)
                    DisableRefreshFlag(userProfileId);
                }
                return refresh;
            }
            else
            {
                return false;
            }
        }

        public bool RefreshInterventionsOnDashboard(string authToken)
        {
            if (InterventionsEnabled())
            {
                //need to check the database for this users's interventions and return true if there are new interventions since the last processing.
                var auth = new Authentication();
                if (!auth.IsValidKey(authToken))
                    return false;

                int userProfileId = auth.GetActiveUserId(authToken);

                ////first check if the user has disabled in-IDE interventions
                //if (!UserEnabledInterventions(userProfileId))
                //{
                //    return false;
                //}

                if (!UserRefreshThesholdMet(userProfileId))
                {
                    return false;
                } //go ahead and continue checking. if the threshold is not met we don't want to refresh

                //now check if the user needs an intervention refreshed.
                bool refresh = CheckInterventionStatus(userProfileId);
                if (refresh)
                {
                    //disable the refresh flag (changed it to here because this is the only method that actually refreshes the interventions)
                    DisableRefreshFlag(userProfileId);
                }
                return refresh;
            }
            else
            {
                return false;
            }
        }

        private bool UserRefreshThesholdMet(int userProfileId)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionsStatus WHERE UserProfileId = @UserProfileId ";

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId }).SingleOrDefault();

                    sqlConnection.Close();

                    if (result != null)
                    {
                        DateTime lastRefreshDT = result.LastRefresh;
                        DateTime timeNow = DateTime.UtcNow;

                        TimeSpan difference = (timeNow - lastRefreshDT);

                        if (difference.TotalMinutes >= UserSetRefreshThreshold(userProfileId)) //TODO: check threshold for refreshing
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("UserRefreshThesholdMet() Failed", e);
            }
        }

        private bool UserEnabledInterventions(int userProfileId)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionSettings WHERE UserProfileId = @UserProfileId ";

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId }).SingleOrDefault();

                    sqlConnection.Close();

                    if (result != null)
                    {
                        return result.ShowInIDESuggestions;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("UserEnabledInterventions() Failed", e);
            }
        }

        private int UserSetRefreshThreshold(int userProfileId)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionSettings WHERE UserProfileId = @UserProfileId ";

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId }).SingleOrDefault();

                    sqlConnection.Close();

                    if (result != null)
                    {
                        return result.RefreshThreshold;
                    }
                    else
                    {
                        return InterventionRefreshThresholdInMinutes;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("UserSetRefreshThreshold() Failed", e);
            }
        }

        private void DisableRefreshFlag(int userProfileId)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionsStatus WHERE UserProfileId = @UserProfileId ";
                    string updateQuery = "UPDATE OSBLEInterventionsStatus SET RefreshInterventions = 0 WHERE Id = @Id ";
                    string insertQuery = "INSERT INTO OSBLEInterventionsStatus ([UserProfileId],[RefreshInterventions],[LastRefresh]) VALUES (@UserProfileId, '0', @LastRefresh) ";

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId }).FirstOrDefault();

                    if (result != null)
                    {
                        sqlConnection.Execute(updateQuery, new { Id = result.Id });
                    }
                    else //insert the user
                    {
                        sqlConnection.Execute(insertQuery, new { UserProfileId = userProfileId, LastRefresh = DateTime.UtcNow });
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("DisableRefreshFlag() failed", e);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdateUserStatus(UpdateUserStatus userStatus)
        {
            //check if the 'you're available exists'
            InterventionItem intervention = GetInterventionFromUserProfileAndType(userStatus.UserProfileId, "AvailableDetails");
            bool dismissSuccess = false;
            bool saveSuccess = false;

            if (intervention.Id == -1 && userStatus.IsAvailableToHelp) //no current intervention, just create and save
            {
                saveSuccess = SaveIntervention(GenerateAvailabilityDetailsIntervention(userStatus, "AvailableDetails", "UserStatusChanged"));
            }
            else //currently a live intervention exists, we want to keep only 1 'live' at a time to prevent spamming of the main page.
            {
                //dismiss out of date intervention and create a new one if they are changing their status from available to not available
                if (userStatus.WasAvailableToHelp && !userStatus.IsAvailableToHelp) //they were available and now are not.
                {
                    dismissSuccess = DismissIntervention(intervention.Id);
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        /// <summary>
        /// Checks the build intervention criteria.
        /// 1. build failure count  > BuildErrorThreshold within the last NumberOfMinutes
        /// 2. No build failure interventin within the last NumberOfMinutes
        /// 3. If build failure within last NumberOfMinutes, dismiss last intervention, create new with most up to date data
        /// </summary>
        /// <param name="userProfileId"></param>
        /// <returns></returns>
        private bool AnalyzeBuildErrorHistory(int userProfileId, BuildEvent buildEvent)
        {
            bool refreshInterventionsRequired = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string countQuery = "SELECT COUNT(*) AS 'Count' FROM " +
                    "(SELECT DISTINCT el.DateReceived, be.SolutionName, eli.[File], eli.Project, eli.Description FROM EventLogs el INNER JOIN BuildEvents be ON el.Id = be.EventLogId INNER JOIN BuildEventErrorListItems beeli " +
                    "ON be.Id = beeli.BuildEventId INNER JOIN ErrorListItems eli ON beeli.ErrorListItemId = eli.Id " +
                    "WHERE el.SenderId = @UserProfileId AND el.EventDate >= DATEADD(MINUTE, -@NumberOfMinutes, GETDATE())) as Count";

                    var buildErrorCount = sqlConnection.Query<int>(countQuery, new { UserProfileId = userProfileId, NumberOfMinutes = NumberOfMinutesErrorThreshold }).Single();

                    //1. error threshold/time check
                    if (buildErrorCount > BuildOrExceptionErrorThreshold)
                    {
                        //2. check if there are any existing build failure interventions
                        InterventionItem intervention = GetInterventionFromUserProfileAndType(userProfileId, "BuildFailure");
                        bool dismissSuccess = false;
                        bool saveSuccess = false;

                        if (intervention.Id == -1) //no current build intervention
                        {
                            //3a. generate a new build failure intervention and save it to the database
                            saveSuccess = SaveIntervention(GenerateBuildIntervention(userProfileId, buildEvent, "BuildFailure", "BuildErrorThreshold", GenerateSuggestedCodeBuildFailure(userProfileId, buildEvent)));
                        }
                        else //currently a live build failure intervention exists, we want to keep only 1 'live' at a time to prevent spamming of the main page.
                        {
                            //3b-1. dismiss out of date build intervention and create a new one
                            dismissSuccess = DismissIntervention(intervention.Id);
                            //3b-2. if we were able to dismiss, try to now generate a new intervention and update status.
                            if (dismissSuccess)
                            {
                                saveSuccess = SaveIntervention(GenerateBuildIntervention(userProfileId, buildEvent, "BuildFailure", "BuildErrorThreshold", GenerateSuggestedCodeBuildFailure(userProfileId, buildEvent)));
                            }
                        }
                        if ((dismissSuccess && saveSuccess) || (intervention.Id == -1 && saveSuccess))
                        {
                            refreshInterventionsRequired = true;
                        }
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //failure
                refreshInterventionsRequired = false;
                throw new Exception("AnalyzeBuildErrorHistory() failed", e);
            }
            return refreshInterventionsRequired;
        }

        private bool AnalyzeExceptionErrorHistory(int userProfileId, ExceptionEvent exceptionEvent)
        {
            bool refreshInterventionsRequired = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string countQuery = "SELECT Count(*) as 'Count' FROM EventLogs el INNER JOIN ExceptionEvents ee ON el.Id = ee.EventLogId " +
                                        "WHERE el.SenderId = @UserProfileId AND el.EventDate >= DATEADD(MINUTE, -@NumberOfMinutes, GETDATE())";

                    var count = sqlConnection.Query<int>(countQuery, new { UserProfileId = userProfileId, NumberOfMinutes = NumberOfMinutesErrorThreshold }).Single();

                    //1. error threshold check
                    if (count > BuildOrExceptionErrorThreshold)
                    {
                        //2. check if there are any existing build failure interventions
                        InterventionItem intervention = GetInterventionFromUserProfileAndType(userProfileId, "AskForHelp");
                        bool dismissSuccess = false;
                        bool saveSuccess = false;

                        if (intervention.Id == -1) //no current build intervention
                        {
                            //3a. generate a new build failure intervention and save it to the database
                            saveSuccess = SaveIntervention(GenerateExceptionIntervention(userProfileId, exceptionEvent, "AskForHelp", "ExceptionEventThreshold", GenerateSuggestedCodeExceptionEvent(userProfileId, exceptionEvent)));
                        }
                        else //currently a live build failure intervention exists, we want to keep only 1 'live' at a time to prevent spamming of the main page.
                        {
                            //3b-1. dismiss out of date build intervention and create a new one
                            dismissSuccess = DismissIntervention(intervention.Id);
                            //3b-2. if we were able to dismiss, try to now generate a new intervention and update status.
                            if (dismissSuccess)
                            {
                                saveSuccess = SaveIntervention(GenerateExceptionIntervention(userProfileId, exceptionEvent, "AskForHelp", "ExceptionEventThreshold", GenerateSuggestedCodeExceptionEvent(userProfileId, exceptionEvent)));
                            }
                        }
                        if ((dismissSuccess && saveSuccess) || (intervention.Id == -1 && saveSuccess))
                        {
                            refreshInterventionsRequired = true;
                        }
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //failure
                refreshInterventionsRequired = false;
                throw new Exception("AnalyzeExceptionErrorHistory() failed", e);
            }
            return refreshInterventionsRequired;
        }

        private void BuildEvent(ActivityEvent log)
        {
            int userProfileId = log.SenderId;
            var buildEvent = (BuildEvent)log;

            int errorCount = 0;
            //for some built events (not sure the case) the build event will have NO errorItems but individual documents will contain error items...
            foreach (var documents in buildEvent.Documents)
            {
                errorCount += documents.Document.ErrorItems.Count();
            }

            if (errorCount > 0) //there are errors, check if we need to generate/refresh interventions
            {
                AnalyzeBuildErrorHistory(userProfileId, buildEvent);
            }
        }

        private void CheckForUnansweredPosts(int courseId, int userProfileId, int numberOfDays, string trigger)
        {
            bool generateIntervention = false;
            string unansweredPostIds = "";
            bool alternateIntervention = false;

            if (courseId == 0)
            {
                courseId = GuessActiveCourseId(userProfileId);
            }

            if (courseId > 0) //double check that we were able to at least guess the course Id, if not there's nothing to do!
            {
                try
                {
                    using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        sqlConnection.Open();

                        //query should return a list of EventLogIds for posts that are 
                        //1) by non instructor/ta role 
                        //2) in the specified course (or null > askForHelp) 
                        //3) of type FeedPost or AskForHelp
                        //4) within the last numberOfDays
                        //5) MAY have replies, but does NOT have any Helpful marks
                        string query = "(SELECT Id FROM EventLogs WHERE (CourseId = @CourseId OR CourseId IS NULL) " +
                                       "AND EventTypeId IN (7, 1) AND EventDate >= DATEADD(day, -@NumberOfDays, GETDATE()) " +
                                       "AND SenderId IN  " +
                                       "(SELECT UserProfileID FROM CourseUsers WHERE AbstractCourseID = @CourseId AND AbstractRoleID NOT IN (1, 2)) " +
                                       "AND Id NOT IN  " +
                                       "(SELECT SourceEventLogId FROM LogCommentEvents WHERE Id IN  " +
                                       "(SELECT DISTINCT LogCommentEventId FROM HelpfulMarkGivenEvents))) " +
                                       "ORDER BY EventDate DESC";

                        var results = sqlConnection.Query<int>(query, new { CourseId = courseId, NumberOfDays = numberOfDays }).ToList();

                        if (results.Count() > 0)
                        {
                            generateIntervention = true;
                            //see if we need to get the alternate unanswered question form
                            //try
                            //{
                            //    List<InterventionTypes> types = new List<InterventionTypes>();
                            //    types.Add(InterventionTypes.UnansweredQuestions);
                            //    types.Add(InterventionTypes.UnansweredQuestionsAlternate);
                            //    InterventionItem lastUnansweredQuestions = GetInterventionFromUserProfileAndMultipleTypes(userProfileId, types, true);
                            //    if (lastUnansweredQuestions.InterventionType == InterventionTypesExtensions.Explode(InterventionTypes.UnansweredQuestions))
                            //    {
                            //        alternateIntervention = true;
                            //        if (!lastUnansweredQuestions.IsDismissed)
                            //        {
                            //            //if it's not dismissed, dismiss it because we are generating the alternate version
                            //            //handled here because SaveIntervention will only check to dismiss the specific type it is trying to save
                            //            DismissIntervention(lastUnansweredQuestions.Id);
                            //        }
                            //    }
                            //}
                            //catch (Exception)
                            //{
                            //    //do nothing...we'll just process the intervention without the alternate version.
                            //}
                        }

                        sqlConnection.Close();
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("CheckForUnansweredPosts() failed", e);
                }

                if (generateIntervention)
                {
                    //save intervention
                    SaveIntervention(GenerateUnansweredQuestionsIntervention(userProfileId, trigger));
                }
            }
        }

        private void CheckIdleThreshold(int userProfileId)
        {
            try
            {
                int courseId = GuessActiveCourseId(userProfileId);
                int count = 0;

                if (courseId > 0)
                {
                    using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        sqlConnection.Open();

                        string query = "SELECT COUNT(*) as 'Count' FROM EventLogs WHERE EventTypeId IN (1, 7, 9) " + //AskForHelp, FeedPost, or LogComment activity
                                       "AND CourseId = @CourseId AND EventDate >= DATEADD(DAY, -@NumberOfDays, GETDATE())";

                        count = sqlConnection.Query<int>(query, new { CourseId = courseId, NumberOfDays = NumberOfDaysIdleThreshold }).FirstOrDefault();

                        sqlConnection.Close();
                    }

                    if (count == 0) //no activity, generate intervention
                    {
                        //first check if there is already an 'active' MakeAPost intervention, if so... do not generate!
                        bool activeMakeAPost = ActiveMakeAPostExists(userProfileId);
                        if (!activeMakeAPost)
                        {
                            SaveIntervention(GenerateMakeAPostIntervention(userProfileId, "MakeAPost", "Idle/No Feed Activity"));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("CheckIdleThreshold() failed.", e);
            }
        }

        private bool ActiveMakeAPostExists(int userProfileId)
        {
            try
            {
                InterventionItem existingIntervention = GetInterventionFromUserProfileAndType(userProfileId, "MakeAPost");

                if (existingIntervention.Id == -1)
                {
                    return false;  //one doesn't exist, check if we need to create one!
                }
                else
                {
                    return true; //an active MakeAPost intervention already exists!
                }
            }
            catch (Exception e)
            {
                throw new Exception("CheckActiveMakeAPost() failed.", e);
            }
        }

        private bool CheckInterventionStatus(int userProfileId)
        {
            bool refreshInterventions = false;
            //first check if we need to add a "others offering help" intervention"
            refreshInterventions = UpdateClassmatesAvailable(userProfileId);

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionsStatus WHERE UserProfileId = @UserProfileId ";
                    //we no longer update the refresh status here, just check if we need to refresh
                    //string updateQuery = "UPDATE OSBLEInterventionsStatus SET RefreshInterventions = 0 WHERE Id = @Id ";
                    string insertQuery = "INSERT INTO OSBLEInterventionsStatus ([UserProfileId],[RefreshInterventions],[LastRefresh]) VALUES (@UserProfileId, '0', @LastRefresh) ";

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId }).FirstOrDefault();

                    if (result != null)
                    {
                        refreshInterventions = result.RefreshInterventions || refreshInterventions; //we want to refresh if either is true                        
                        //sqlConnection.Execute(updateQuery, new { Id = result.Id });
                    }
                    else //insert the user
                    {
                        sqlConnection.Execute(insertQuery, new { UserProfileId = userProfileId, LastRefresh = DateTime.UtcNow });
                        refreshInterventions = true; //we want to refresh at least once now.
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("CheckInterventionStatus() failed", e);
            }
            return refreshInterventions;
        }

        private void CheckNoErrorThreshold(int userProfileId, string trigger)
        {
            bool generateIntervention = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    //Get count of exception events in the last ErrorFreeMinutesThreshold number of minutes
                    string query = "SELECT COUNT(*) as 'Count' FROM EventLogs el INNER JOIN ExceptionEvents ee ON el.Id = ee.EventLogId " +
                                   "WHERE el.SenderId = @UserProfileId AND el.EventDate >= DATEADD(MINUTE, -@NumberOfMinutes, GETDATE()) " +
                        //Get count of build error events in the last ErrorFreeMinutesThreshold number of minutes
                                   "SELECT  COUNT(*) as 'Count' FROM EventLogs el INNER JOIN BuildEvents be ON el.Id = be.EventLogId " +
                                   "INNER JOIN BuildEventErrorListItems beeli ON be.Id = beeli.BuildEventId " +
                                   "WHERE SenderId = @UserProfileId AND el.EventDate >= DATEADD(MINUTE, -@NumberOfMinutes, GETDATE())";

                    int count = 0;
                    using (var queries = sqlConnection.QueryMultiple(query, new { UserProfileId = userProfileId, NumberOfMinutes = ErrorFreeMinutesThreshold }))
                    {
                        count += queries.Read<int>().SingleOrDefault();
                        count += queries.Read<int>().SingleOrDefault();
                    }

                    if (count == 0) //there have been no exception or build errors in the last ErrorFreeMinutesThreshold, generate intervention
                    {
                        generateIntervention = true;
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("CheckNoErrorThreshold() failed", e);
            }

            if (generateIntervention)
            {
                CheckForUnansweredPosts(0, userProfileId, UnansweredQuestionsDaysThreshold, trigger);
            }
        }

        private void CutCopyPasteEvent(ActivityEvent log)
        {
            CheckNoErrorThreshold(log.SenderId, "CutCopyPasteEvent");
            CheckIdleThreshold(log.SenderId);
            CheckInterventionStatus(log.SenderId);
        }

        private bool DismissIntervention(int interventionId)
        {
            bool isDismissed = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "UPDATE OSBLEInterventions SET IsDismissed = 1 WHERE Id = @InterventionId";

                    isDismissed = sqlConnection.Execute(query, new { InterventionId = interventionId }) != 0;

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("DismissIntervention() failed", e);
            }
            if (isDismissed)
            {
                LogInterventionInteraction("DismissIntervention() AUTOMATED INITIATION.", interventionId, -1, "", "", "Automatically dismissed to replace with updated intervention");
            }

            return isDismissed;
        }

        private void EditorActivityEvent(ActivityEvent log)
        {
            CheckNoErrorThreshold(log.SenderId, "EditorActivityEvent");
            CheckIdleThreshold(log.SenderId);
            CheckInterventionStatus(log.SenderId);
        }
        private void ExceptionEvent(ActivityEvent log)
        {
            int userProfileId = log.SenderId;
            var exceptionEvent = (ExceptionEvent)log;

            AnalyzeExceptionErrorHistory(userProfileId, exceptionEvent);
        }

        private InterventionItem GenerateAvailabilityDetailsIntervention(UpdateUserStatus userStatus, string interventionType, string trigger)
        {
            int userProfileId = userStatus.UserProfileId;
            var interventionTypeEnum = InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionType);
            Dictionary<int, string> icons = InterventionTypesExtensions.Icons(interventionTypeEnum);

            InterventionItem intervention = new InterventionItem();
            //intervention.Id generated on db insert
            intervention.UserProfileId = userProfileId;
            intervention.InterventionTrigger = trigger;
            intervention.InterventionMarkedHelpful = false;
            intervention.InterventionDateTime = DateTime.UtcNow;
            intervention.InterventionType = InterventionTypesExtensions.Explode(interventionTypeEnum);
            intervention.Icon1 = icons[1];
            intervention.Icon2 = icons[2];
            intervention.Title = InterventionTypesExtensions.Title(interventionTypeEnum);
            intervention.Link = intervention.InterventionType; //same as type, link is built later ::string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, interventionType, intervention.Id);
            intervention.LinkText = InterventionTypesExtensions.LinkText(interventionTypeEnum);
            //TODO: Populate the template text from the database, for now just use the canned response from the model.            
            intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(interventionTypeEnum);
            intervention.ListItemContent = InterventionTypesExtensions.ListItemContent(interventionTypeEnum);
            intervention.InterventionTemplateText = InterventionTypesExtensions.TemplateText(interventionTypeEnum);
            intervention.InterventionSuggestedCode = "";
            intervention.IsDismissed = false;
            return intervention;
        }

        private string GenerateBuildFailureTemplateText(InterventionTypes interventionTypeEnum, BuildEvent buildEvent)
        {
            //will want to split on '[... and then on ...]'
            string template = InterventionTypesExtensions.TemplateText(interventionTypeEnum);
            string start = template.Split(new string[] { "|||" }, StringSplitOptions.None).First();
            string end = template.Split(new string[] { "|||" }, StringSplitOptions.None).Last();
            string errorList = "";
            List<BuildErrorDocument> buildErrorDocuments = GenerateBuildItemDocuments(buildEvent);

            foreach (var item in buildErrorDocuments)
            {
                errorList += item.ErrorItemDescription + ": ";
            }

            return start + " \n[" + errorList + "]. \n\n" + end;
        }

        private InterventionItem GenerateBuildIntervention(int userProfileId, BuildEvent buildEvent, string interventionType, string trigger, string suggestedCode = "")
        {
            var interventionTypeEnum = InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionType);
            Dictionary<int, string> icons = InterventionTypesExtensions.Icons(interventionTypeEnum);

            InterventionItem intervention = new InterventionItem();
            //intervention.Id generated on db insert
            intervention.UserProfileId = userProfileId;
            intervention.InterventionTrigger = trigger;
            intervention.InterventionMarkedHelpful = false;
            intervention.InterventionDateTime = DateTime.UtcNow;
            intervention.InterventionType = InterventionTypesExtensions.Explode(interventionTypeEnum);
            intervention.Icon1 = icons[1];
            intervention.Icon2 = icons[2];
            intervention.Title = InterventionTypesExtensions.Title(interventionTypeEnum);
            intervention.Link = intervention.InterventionType; //same as type, link is built later ::string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, interventionType, intervention.Id);
            intervention.LinkText = InterventionTypesExtensions.LinkText(interventionTypeEnum);
            //TODO: Populate the template text from the database, for now just use the canned response from the model.            
            intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(interventionTypeEnum);
            intervention.ListItemContent = InterventionTypesExtensions.ListItemContent(interventionTypeEnum);
            intervention.InterventionTemplateText = GenerateBuildFailureTemplateText(interventionTypeEnum, buildEvent);
            intervention.InterventionSuggestedCode = suggestedCode;
            intervention.IsDismissed = false;
            return intervention;
        }

        private List<BuildErrorDocument> GenerateBuildItemDocuments(BuildEvent buildEvent)
        {
            List<BuildErrorDocument> buildErrorDocuments = new List<BuildErrorDocument>();
            foreach (var item in buildEvent.Documents)
            {
                if (item.Document.ErrorItems.Count() > 0)
                {
                    BuildErrorDocument bed = new BuildErrorDocument();
                    bed.ProjectName = item.Document.ErrorItems.First().ErrorListItem.Project.Split('\\').Last().Split('.').First();
                    bed.FileName = item.Document.FileName.Split('\\').Last();
                    bed.Line = item.Document.ErrorItems.First().ErrorListItem.Line;
                    bed.ContentLines = item.Document.Lines;
                    bed.ErrorItemDescription = item.Document.ErrorItems.First().ErrorListItem.Description;
                    buildErrorDocuments.Add(bed);
                }
            }
            return buildErrorDocuments;
        }

        private InterventionItem GenerateClassmatesAvailableIntervention(int userProfileId)
        {
            var interventionTypeEnum = InterventionTypesExtensions.ParseEnum<InterventionTypes>("ClassmatesAvailable");
            Dictionary<int, string> icons = InterventionTypesExtensions.Icons(interventionTypeEnum);

            InterventionItem intervention = new InterventionItem();
            //intervention.Id generated on db insert
            intervention.UserProfileId = userProfileId;
            intervention.InterventionTrigger = "Users are Available";
            intervention.InterventionMarkedHelpful = false;
            intervention.InterventionDateTime = DateTime.UtcNow;
            intervention.InterventionType = InterventionTypesExtensions.Explode(interventionTypeEnum);
            intervention.Icon1 = icons[1];
            intervention.Icon2 = icons[2];
            intervention.Title = InterventionTypesExtensions.Title(interventionTypeEnum);
            intervention.Link = intervention.InterventionType; //same as type, link is built later ::string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, interventionType, intervention.Id);
            intervention.LinkText = InterventionTypesExtensions.LinkText(interventionTypeEnum);
            //TODO: Populate the template text from the database, for now just use the canned response from the model.            
            intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(interventionTypeEnum);
            intervention.ListItemContent = InterventionTypesExtensions.ListItemContent(interventionTypeEnum);
            intervention.InterventionTemplateText = InterventionTypesExtensions.TemplateText(interventionTypeEnum);
            intervention.InterventionSuggestedCode = "";
            intervention.IsDismissed = false;
            return intervention;
        }

        private InterventionItem GenerateExceptionIntervention(int userProfileId, ExceptionEvent exceptionEvent, string interventionType, string trigger, string suggestedCode = "")
        {
            var interventionTypeEnum = InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionType);
            Dictionary<int, string> icons = InterventionTypesExtensions.Icons(interventionTypeEnum);

            InterventionItem intervention = new InterventionItem();
            //intervention.Id generated on db insert
            intervention.UserProfileId = userProfileId;
            intervention.InterventionTrigger = trigger;
            intervention.InterventionMarkedHelpful = false;
            intervention.InterventionDateTime = DateTime.UtcNow;
            intervention.InterventionType = InterventionTypesExtensions.Explode(interventionTypeEnum);
            intervention.Icon1 = icons[1];
            intervention.Icon2 = icons[2];
            intervention.Title = InterventionTypesExtensions.Title(interventionTypeEnum);
            intervention.Link = intervention.InterventionType; //same as type, link is built later ::string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, interventionType, intervention.Id);
            intervention.LinkText = InterventionTypesExtensions.LinkText(interventionTypeEnum);
            //TODO: Populate the template text from the database, for now just use the canned response from the model.            
            intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(interventionTypeEnum);
            intervention.ListItemContent = InterventionTypesExtensions.ListItemContent(interventionTypeEnum);
            intervention.InterventionTemplateText = GenerateExceptionTemplateText(interventionTypeEnum, exceptionEvent);
            intervention.InterventionSuggestedCode = suggestedCode;
            intervention.IsDismissed = false;
            return intervention;
        }

        private string GenerateExceptionTemplateText(InterventionTypes interventionTypeEnum, ExceptionEvent exceptionEvent)
        {
            //will want to split on '||| and then on |||' delimiters
            string template = InterventionTypesExtensions.TemplateText(interventionTypeEnum);
            string start = template.Split(new string[] { "|||" }, StringSplitOptions.None).First();
            string end = template.Split(new string[] { "|||" }, StringSplitOptions.None).Last();
            string errorList = "";

            errorList += exceptionEvent.ExceptionType + ": ";
            errorList += exceptionEvent.ExceptionName + ": \"";
            errorList += exceptionEvent.ExceptionDescription + "\": ";
            errorList += "on Line " + exceptionEvent.LineNumber;
            errorList += " Near: \"" + exceptionEvent.LineContent.Replace('\n', ' ') + "\"";

            return start + " \n[" + errorList + "]. \n\n" + end;
        }

        private InterventionItem GenerateMakeAPostIntervention(int userProfileId, string interventionType, string trigger)
        {
            var interventionTypeEnum = InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionType);
            Dictionary<int, string> icons = InterventionTypesExtensions.Icons(interventionTypeEnum);

            InterventionItem intervention = new InterventionItem();
            //intervention.Id generated on db insert
            intervention.UserProfileId = userProfileId;
            intervention.InterventionTrigger = trigger;
            intervention.InterventionMarkedHelpful = false;
            intervention.InterventionDateTime = DateTime.UtcNow;
            intervention.InterventionType = InterventionTypesExtensions.Explode(interventionTypeEnum);
            intervention.Icon1 = icons[1];
            intervention.Icon2 = icons[2];
            intervention.Title = InterventionTypesExtensions.Title(interventionTypeEnum);
            intervention.Link = intervention.InterventionType; //same as type, link is built later ::string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, interventionType, intervention.Id);
            intervention.LinkText = InterventionTypesExtensions.LinkText(interventionTypeEnum);
            //TODO: Populate the template text from the database, for now just use the canned response from the model.            
            intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(interventionTypeEnum);
            intervention.ListItemContent = InterventionTypesExtensions.ListItemContent(interventionTypeEnum);
            intervention.InterventionTemplateText = InterventionTypesExtensions.TemplateText(interventionTypeEnum);
            intervention.InterventionSuggestedCode = "";
            intervention.IsDismissed = false;
            return intervention;
        }

        private InterventionItem GenerateSubmitIntervention(int userProfileId, SubmitEvent submitEvent, string interventionType, string trigger, string templateText)
        {
            var interventionTypeEnum = InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionType);
            Dictionary<int, string> icons = InterventionTypesExtensions.Icons(interventionTypeEnum);

            InterventionItem intervention = new InterventionItem();
            //intervention.Id generated on db insert
            intervention.UserProfileId = userProfileId;
            intervention.InterventionTrigger = trigger;
            intervention.InterventionMarkedHelpful = false;
            intervention.InterventionDateTime = DateTime.UtcNow;
            intervention.InterventionType = InterventionTypesExtensions.Explode(interventionTypeEnum);
            intervention.Icon1 = icons[1];
            intervention.Icon2 = icons[2];
            intervention.Title = InterventionTypesExtensions.Title(interventionTypeEnum);
            intervention.Link = intervention.InterventionType; //same as type, link is built later ::string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, interventionType, intervention.Id);
            intervention.LinkText = InterventionTypesExtensions.LinkText(interventionTypeEnum);
            //TODO: Populate the template text from the database, for now just use the canned response from the model.            
            intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(interventionTypeEnum);
            intervention.ListItemContent = InterventionTypesExtensions.ListItemContent(interventionTypeEnum);
            intervention.InterventionTemplateText = templateText;
            intervention.InterventionSuggestedCode = "";
            intervention.IsDismissed = false;
            return intervention;
        }

        private string GenerateSubmitTemplateText(int userProfileId, SubmitEvent submitEvent)
        {
            string template = InterventionTypesExtensions.TemplateText(InterventionTypes.MakeAPostAssignmentSubmit);
            string start = template.Split(new string[] { "|||" }, StringSplitOptions.None).First();
            string end = template.Split(new string[] { "|||" }, StringSplitOptions.None).Last();
            return start + GetAssignmentName(submitEvent.AssignmentId) + end;
        }

        private string GenerateSuggestedCodeBuildFailure(int userProfileId, BuildEvent buildEvent)
        {
            int line = 0;
            string content = "";
            string suggestedCode = "";

            List<BuildErrorDocument> buildErrorDocuments = GenerateBuildItemDocuments(buildEvent);

            if (buildErrorDocuments.Count() > 0)
            {
                foreach (var item in buildErrorDocuments)
                {
                    line = item.Line;
                    int codeLineStart = line - CodeLineVarianceThreshold > 0 ? line - CodeLineVarianceThreshold : 0;
                    int codeLineEnd = line + CodeLineVarianceThreshold < item.ContentLines.Count() ? line + CodeLineVarianceThreshold : item.ContentLines.Count();

                    suggestedCode += "\n\n/**********  " + item.ProjectName + ":  " + item.FileName + "  **********/\n";

                    for (int i = codeLineStart; i < codeLineEnd; i++)
                    {
                        suggestedCode += item.ContentLines[i] + "\n";
                    }

                    suggestedCode += "\n/**********  end code excerpt  **********/";
                }
            }
            else
            {
                return "//Unable to generate code... \n\nPlease click edit, then copy -> paste the code surrounding your error, and then click 'Save Changes' before submitting your post.";
            }
            return suggestedCode;
        }

        private string GenerateSuggestedCodeExceptionEvent(int userProfileId, ExceptionEvent exceptionEvent)
        {
            List<string> codeDocument = GetExceptionCodeDocument(userProfileId); //we need to grab the code document from their last save event

            if (codeDocument.Count() == 0)
            {
                return "//Unable to generate code... \n\nPlease click edit, then copy -> paste the code surrounding your error, and then click 'Save Changes' before submitting your post.";
            }

            int line = exceptionEvent.LineNumber;
            string suggestedCode = "";

            int codeLineStart = line - CodeLineVarianceThreshold > 0 ? line - CodeLineVarianceThreshold : 0;
            int codeLineEnd = line + CodeLineVarianceThreshold < codeDocument.Count() ? line + CodeLineVarianceThreshold : codeDocument.Count();

            string solutionName = exceptionEvent.SolutionName.Split('\\').Last().Split('.').First(); //get the solution name from the filepath stored in the exception event

            suggestedCode += "\n\n/**********  " + solutionName + ":  " + exceptionEvent.DocumentName + "  **********/\n";

            for (int i = codeLineStart; i < codeLineEnd; i++)
            {
                suggestedCode += codeDocument[i] + "\n";
            }

            suggestedCode += "\n/**********  end code excerpt  **********/";

            return suggestedCode;
        }

        private InterventionItem GenerateUnansweredQuestionsIntervention(int userProfileId, string trigger, bool alternateTemplate = false)
        {
            string interventionType = "";

            if (alternateTemplate)
            {
                interventionType = InterventionTypesExtensions.Explode(InterventionTypes.UnansweredQuestionsAlternate);
            }
            else
            {
                interventionType = InterventionTypesExtensions.Explode(InterventionTypes.UnansweredQuestions);
            }

            var interventionTypeEnum = InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionType);
            Dictionary<int, string> icons = InterventionTypesExtensions.Icons(interventionTypeEnum);

            InterventionItem intervention = new InterventionItem();
            //intervention.Id generated on db insert
            intervention.UserProfileId = userProfileId;
            intervention.InterventionTrigger = trigger;
            intervention.InterventionMarkedHelpful = false;
            intervention.InterventionDateTime = DateTime.UtcNow;
            intervention.InterventionType = InterventionTypesExtensions.Explode(interventionTypeEnum);
            intervention.Icon1 = icons[1];
            intervention.Icon2 = icons[2];
            intervention.Title = InterventionTypesExtensions.Title(interventionTypeEnum);
            intervention.Link = intervention.InterventionType; //same as type, link is built later ::string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, interventionType, intervention.Id);
            intervention.LinkText = InterventionTypesExtensions.LinkText(interventionTypeEnum);
            //TODO: Populate the template text from the database, for now just use the canned response from the model.            
            intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(interventionTypeEnum);
            intervention.ListItemContent = InterventionTypesExtensions.ListItemContent(interventionTypeEnum);
            intervention.InterventionTemplateText = InterventionTypesExtensions.TemplateText(interventionTypeEnum);
            intervention.InterventionSuggestedCode = "";
            intervention.IsDismissed = false;
            return intervention;
        }

        private List<int> GetAllUserCourseIds(int userProfileId)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "";

                    //we're getting all active course Ids because ask for help events are visible in all courses
                    query = "SELECT DISTINCT ISNULL(ac.ID, 0) " +
                                "FROM AbstractCourses ac " +
                                "INNER JOIN CourseUsers cu " +
                                "ON ac.ID = cu.AbstractCourseID " +
                                "WHERE GETDATE() < ac.EndDate " +
                                "AND ac.Inactive = 0 " +
                                "AND cu.UserProfileID = @userProfileId";

                    List<int> activeCourseIds = sqlConnection.Query<int>(query, new { userProfileId = userProfileId }).ToList();

                    sqlConnection.Close();

                    return activeCourseIds;
                }
            }
            catch (Exception e)
            {
                throw new Exception("GetAllUserCourseIds() failed", e);
            }
            return new List<int>(); //failure, return empty list
        }

        private DateTime GetAssignmentDueDate(int assignmentId)
        {
            DateTime assignmentDueDate = DateTime.UtcNow;
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();
                string query = "SELECT ISNULL((SELECT DueDate FROM Assignments WHERE ID =@AssignmentId), GETDATE()) ";

                assignmentDueDate = sqlConnection.Query<DateTime>(query, new { AssignmentId = assignmentId }).SingleOrDefault();

                sqlConnection.Close();
            }
            return assignmentDueDate;
        }

        private string GetAssignmentName(int assignmentId)
        {
            string assignmentName = "";
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();
                string query = "SELECT ISNULL((SELECT AssignmentName FROM Assignments WHERE ID = @AssignmentId), ' [assignment # here] ') ";

                assignmentName = sqlConnection.Query<string>(query, new { AssignmentId = assignmentId }).SingleOrDefault();

                sqlConnection.Close();
            }
            return assignmentName;
        }

        private List<string> GetExceptionCodeDocument(int userProfileId)
        {
            List<string> codeDocument = new List<string>();
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "";

                    //we're getting all active course Ids because ask for help events are visible in all courses
                    query = "SELECT CodeDocuments.Content FROM CodeDocuments WHERE Id = " + //get the code document from the last save
                                "(SELECT DocumentId FROM SaveEvents WHERE EventLogId = " + //use the last saveEvent log id to get the document id
                                    "(SELECT TOP(1) Id FROM EventLogs WHERE EventTypeId = 10 AND SenderId = @userProfileId ORDER BY EventDate DESC))"; //get the last saveEvent

                    string result = sqlConnection.Query<string>(query, new { userProfileId = userProfileId }).FirstOrDefault(); //should only return 1 result

                    sqlConnection.Close();

                    //parse the string into lines
                    codeDocument = result.Split('\n').ToList();
                }
            }
            catch (Exception e)
            {
                throw new Exception("GetExceptionCodeDocument() failed", e);
            }
            return codeDocument;
        }
        private InterventionItem GetInterventionFromUserProfileAndMultipleTypes(int userProfileId, List<InterventionTypes> interventionTypes, bool includeDismissed = false)
        {
            InterventionItem intervention = new InterventionItem();
            try
            {
                List<string> types = new List<string>();
                foreach (InterventionTypes type in interventionTypes)
                {
                    types.Add(InterventionTypesExtensions.Explode(type));
                }

                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "";
                    if (includeDismissed)
                    {
                        query = "SELECT TOP 1 * FROM OSBLEInterventions WHERE UserProfileId = @UserProfileId AND InterventionType IN @InterventionTypes ORDER BY InterventionDateTime DESC";
                    }
                    else
                    {
                        query = "SELECT TOP 1 * FROM OSBLEInterventions WHERE UserProfileId = @UserProfileId AND InterventionType IN @InterventionTypes AND IsDismissed = 0 ORDER BY InterventionDateTime DESC";
                    }

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId, InterventionTypes = types }).SingleOrDefault();

                    if (result != null)
                    {
                        //Parse results
                        intervention.Id = result.Id;
                        intervention.UserProfileId = result.UserProfileId;
                        intervention.InterventionTrigger = result.InterventionTrigger;
                        intervention.InterventionMarkedHelpful = result.InterventionMarkedHelpful ?? false;
                        intervention.InterventionDateTime = result.InterventionDateTime;
                        intervention.InterventionType = result.InterventionType;
                        intervention.Icon1 = result.Icon1;
                        intervention.Icon2 = result.Icon2;
                        intervention.Title = result.Title;
                        intervention.Link = result.Link; //string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, result.InterventionType, result.Id);
                        intervention.LinkText = result.LinkText;
                        intervention.ContentFirst = result.ContentFirst;
                        intervention.ListItemContent = result.ListItemContent;
                        intervention.InterventionTemplateText = result.InterventionTemplateText;
                        intervention.InterventionSuggestedCode = result.InterventionSuggestedCode;
                        intervention.IsDismissed = result.IsDismissed ?? false;
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("GetInterventionFromUserProfileAndMultipleTypes() Failed", e);
            }
            return intervention;
        }

        private InterventionItem GetInterventionFromUserProfileAndType(int userProfileId, string interventionType, bool includeDismissed = false)
        {
            InterventionItem intervention = new InterventionItem();
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "";
                    if (includeDismissed)
                    {
                        query = "SELECT TOP 1 * FROM OSBLEInterventions WHERE UserProfileId = @UserProfileId AND InterventionType = @InterventionType ORDER BY InterventionDateTime DESC";
                    }
                    else
                    {
                        query = "SELECT TOP 1 * FROM OSBLEInterventions WHERE UserProfileId = @UserProfileId AND InterventionType = @InterventionType AND IsDismissed = 0 ORDER BY InterventionDateTime DESC";
                    }

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId, InterventionType = interventionType }).SingleOrDefault();

                    if (result != null)
                    {
                        //Parse results
                        intervention.Id = result.Id;
                        intervention.UserProfileId = result.UserProfileId;
                        intervention.InterventionTrigger = result.InterventionTrigger;
                        intervention.InterventionMarkedHelpful = result.InterventionMarkedHelpful ?? false;
                        intervention.InterventionDateTime = result.InterventionDateTime;
                        intervention.InterventionType = result.InterventionType;
                        intervention.Icon1 = result.Icon1;
                        intervention.Icon2 = result.Icon2;
                        intervention.Title = result.Title;
                        intervention.Link = result.Link; //string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, result.InterventionType, result.Id);
                        intervention.LinkText = result.LinkText;
                        intervention.ContentFirst = result.ContentFirst;
                        intervention.ListItemContent = result.ListItemContent;
                        intervention.InterventionTemplateText = result.InterventionTemplateText;
                        intervention.InterventionSuggestedCode = result.InterventionSuggestedCode;
                        intervention.IsDismissed = result.IsDismissed ?? false;
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("GetInterventionFromUserProfileAndType() Failed", e);
            }
            return intervention;
        }
        private bool GetUserIsAvailableStatus(UpdateUserStatus userStatus)
        {
            bool isAvailableToHelp = false;
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();
                string query = "SELECT IsAvailableToHelp FROM UserStatus WHERE UserProfileId = @UserProfileId AND CourseId = @CourseId ";

                isAvailableToHelp = sqlConnection.Query<bool>(query, new { UserProfileId = userStatus.UserProfileId, CourseId = userStatus.ActiveCourseId }).SingleOrDefault();

                sqlConnection.Close();
            }
            return isAvailableToHelp;
        }

        private int GuessActiveCourseId(int userProfileId)
        {
            int courseId = 0;
            //will need to do this for ask for help/exception events until a courseId is associated with them                
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();
                string query = "SELECT ISNULL( " +
                                "(SELECT TOP 1 CourseId " +
                                "FROM EventLogs " +
                                "WHERE SenderId = @UserProfileId " +
                                "AND CourseId IS NOT NULL " +
                                "ORDER BY EventDate DESC) " +
                                ", 0) AS CourseId ";

                var result = sqlConnection.Query(query, new { userProfileId = userProfileId }).AsList();

                courseId = result[0].CourseId;

                sqlConnection.Close();
            }
            return courseId;
        }

        private void LogInterventionInteraction(string interventionDetails = "", int interventionId = -10, int userProfileId = 0, string interventionDetailBefore = "", string interventionDetailAfter = "", string additionalActionDetails = "")
        { //details first so we can pass in just details if desired.
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string insertQuery = "INSERT INTO OSBLEInterventionInteractions " +
                                         "VALUES (@InterventionId, @UserProfileId, GETDATE(), @InterventionDetails, '', @InterventionDetailBefore, @InterventionDetailAfter, @AdditionalActionDetails) ";

                    sqlConnection.Execute(insertQuery, new { InterventionId = interventionId, UserProfileId = userProfileId, InterventionDetails = interventionDetails, InterventionDetailBefore = interventionDetailBefore, InterventionDetailAfter = interventionDetailAfter, AdditionalActionDetails = additionalActionDetails });

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("LogInterventionInteraction() Failed to insert automated interaction", e);
            }
        }

        private bool ProcessMakeAPostSubmit(SubmitEvent submitEvent, int userProfileId, string interventionType)
        {
            bool offerHelp = interventionType.Contains("OfferHelp");
            //if the submission time greater than the due date (minus early threshold), don't process the early submit intervention
            if (offerHelp && (DateTime.UtcNow > GetAssignmentDueDate(submitEvent.AssignmentId).AddDays(SubmitAssignmentEarlyTimeThreshold)))
            {
                return false;
            }

            bool refreshInterventionsRequired = false;
            try
            {
                //1. check if there are any existing submit early interventions
                InterventionItem intervention = GetInterventionFromUserProfileAndType(userProfileId, interventionType);
                bool dismissSuccess = false;
                bool saveSuccess = false;

                if (intervention.Id == -1) //no current submit intervention, just create and save a new intervention
                {
                    //3a. generate a new submit intervention and save it to the database
                    saveSuccess = SaveIntervention(GenerateSubmitIntervention(userProfileId, submitEvent, interventionType, "AssignmentSubmission", GenerateSubmitTemplateText(userProfileId, submitEvent)));
                }
                else //currently a livesubmit intervention exists, we want to keep only 1 'live' at a time to prevent spamming of the main page.
                {
                    //3b-1. dismiss out of date submit intervention and create a new one
                    dismissSuccess = DismissIntervention(intervention.Id);
                    //3b-2. if we were able to dismiss, try to now generate a new intervention and update status.
                    if (dismissSuccess)
                    {
                        saveSuccess = SaveIntervention(GenerateSubmitIntervention(userProfileId, submitEvent, interventionType, "AssignmentSubmission", GenerateSubmitTemplateText(userProfileId, submitEvent)));
                    }
                }
                if ((dismissSuccess && saveSuccess) || (intervention.Id == -1 && saveSuccess))
                {
                    refreshInterventionsRequired = true;
                }
            }
            catch (Exception e)
            {
                //failure
                refreshInterventionsRequired = false;
                throw new Exception("ProcessMakeAPostSubmit() Failed", e);
            }
            return refreshInterventionsRequired;
        }

        private void ProcessMakeAPostTopicSuggestion(ActivityEvent log, string trigger)
        {
            //first check if we already have a topical MakeAPost
            bool interventionExists = GetInterventionFromUserProfileAndType(log.SenderId, "MakeAPost").Id != -1;
            if (!interventionExists) //go ahead and process if we don't find a matching intervention
            {
                SaveIntervention(GenerateMakeAPostIntervention(log.SenderId, "MakeAPost", trigger));
            }
        }

        private bool SaveIntervention(InterventionItem intervention)
        {
            bool dismissSuccess = false;
            bool insertSuccess = false;
            //first make sure that no intervention of the same type exists for this user.
            InterventionItem existingIntervention = GetInterventionFromUserProfileAndType(intervention.UserProfileId, intervention.InterventionType);

            if (existingIntervention.Id != -1) //one already exists, dismiss it first before saving the new one
            {
                if (InterventionRequiresRefresh(existingIntervention)) //check for certain types that need to be refreshed if they exist e.g. build/runtime errors need the latest info while unanswered questions wont.
                {
                    dismissSuccess = DismissIntervention(existingIntervention.Id);
                }
                //else leave dismissSuccess as false so we wont save...
            }

            //if an existing intervention of this type exists and was successfully dismissed OR if one didn't exist before
            if ((existingIntervention.Id != -1 && dismissSuccess) || existingIntervention.Id == -1)
            {
                try
                {
                    using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        sqlConnection.Open();

                        string query = "INSERT INTO OSBLEInterventions ([UserProfileId],[InterventionTrigger],[InterventionMarkedHelpful],[InterventionDateTime],[InterventionType], " +
                                       "[Icon1],[Icon2],[Title],[Link],[LinkText],[ContentFirst],[ListItemContent],[InterventionTemplateText],[InterventionSuggestedCode],[IsDismissed]) " +
                                       "VALUES ( @UserProfileId, @InterventionTrigger, @InterventionMarkedHelpful, @InterventionDateTime, @InterventionType, @Icon1, @Icon2, @Title,  " +
                                       "@Link, @LinkText, @ContentFirst, @ListItemContent, @InterventionTemplateText, @InterventionSuggestedCode, @IsDismissed )";

                        insertSuccess = sqlConnection.Execute(query,
                            new
                            {
                                UserProfileId = intervention.UserProfileId,
                                InterventionTrigger = intervention.InterventionTrigger,
                                InterventionMarkedHelpful = intervention.InterventionMarkedHelpful,
                                InterventionDateTime = intervention.InterventionDateTime,
                                InterventionType = intervention.InterventionType,
                                Icon1 = intervention.Icon1,
                                Icon2 = intervention.Icon2,
                                Title = intervention.Title,
                                Link = intervention.Link,
                                LinkText = intervention.LinkText,
                                ContentFirst = intervention.ContentFirst,
                                ListItemContent = intervention.ListItemContent,
                                InterventionTemplateText = intervention.InterventionTemplateText,
                                InterventionSuggestedCode = intervention.InterventionSuggestedCode,
                                IsDismissed = intervention.IsDismissed
                            }) != 0;

                        sqlConnection.Close();
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("SaveIntervention() Failed to insert new intervention", e);
                }
            }

            if (insertSuccess)
            {
                TriggerInterventionRefresh(intervention.UserProfileId);
            }

            return insertSuccess;
        }

        /// <summary>
        /// Checks to see if a type of intervention needs the 'latest' data and should be refreshed if one already exists.
        /// returning true means the intervention needs to be dismissed and generated again (e.g. it's a build error and there may be new/different errors)
        /// returning false means do not dismiss or generate another intervention, leave the one that is still live up.
        /// </summary>
        /// <param name="existingIntervention"></param>
        /// <returns></returns>
        private bool InterventionRequiresRefresh(InterventionItem existingIntervention)
        {
            var interventionTypeEnum = InterventionTypesExtensions.ParseEnum<InterventionTypes>(existingIntervention.InterventionType);
            switch (interventionTypeEnum)
            {
                case InterventionTypes.AskForHelp: //run-time errors
                    return true;
                    break;
                case InterventionTypes.AvailableDetails:
                    return false;
                    break;
                case InterventionTypes.ClassmatesAvailable:
                    return false;
                    break;
                case InterventionTypes.MakeAPost:
                    return true;
                    break;
                case InterventionTypes.MakeAPostAssignmentSubmit:
                    return true;
                    break;
                case InterventionTypes.OfferHelp:
                    return true;
                    break;
                case InterventionTypes.UnansweredQuestions:
                    return false;
                    break;
                case InterventionTypes.BuildFailure:
                    return true;
                    break;
                case InterventionTypes.UnansweredQuestionsAlternate:
                    return false;
                    break;
                default:
                    return false;
                    break;
            }
        }

        private void SubmitEvent(ActivityEvent log)
        {
            SubmitEvent submitEvent = (SubmitEvent)log;
            //process intervention for unanswered questions and for MakeAPost
            bool interventionGenerated = ProcessMakeAPostSubmit(submitEvent, submitEvent.SenderId, "OfferHelp");
            if (!interventionGenerated) //only process the MakeAPostAssignmentSubmit if they do not have an early submit suggestion.
            {
                interventionGenerated = ProcessMakeAPostSubmit(submitEvent, submitEvent.SenderId, "MakeAPostAssignmentSubmit");
            }

            //check if there are 'unanswered' questions.
            CheckForUnansweredPosts(submitEvent.CourseId ?? 0, submitEvent.SenderId, UnansweredQuestionsDaysThreshold, "AssignmentSubmission");

            //process MakeAPost suggestion now
            ProcessMakeAPostTopicSuggestion(log, "Assignment Submitted");
        }
        private bool TriggerInterventionRefresh(int userProfileId)
        {
            bool updateSuccess = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionsStatus WHERE UserProfileId = @UserProfileId ";
                    string updateQuery = "UPDATE OSBLEInterventionsStatus SET RefreshInterventions = 1, LastRefresh = @LastRefresh WHERE Id = @Id ";
                    string insertQuery = "INSERT INTO OSBLEInterventionsStatus ([UserProfileId],[RefreshInterventions],[LastRefresh]) VALUES (@UserProfileId, '1', @LastRefresh) ";

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId }).FirstOrDefault();

                    if (result != null) //a row was found, update it to a 'ready to refresh' status
                    {
                        updateSuccess = sqlConnection.Execute(updateQuery, new { Id = result.Id, LastRefresh = DateTime.UtcNow }) != 0;
                    }
                    else //the user doesn't have a row yet, insert the user with 'ready to refresh' status
                    {
                        updateSuccess = sqlConnection.Execute(insertQuery, new { UserProfileId = userProfileId, LastRefresh = DateTime.UtcNow }) != 0;
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("TriggerInterventionRefresh() Failed", e);
            }
            return updateSuccess;
        }

        /// <summary>
        /// Checks if other users are offering help in any of this user's courses... if so, generate the others offering help intervention
        /// </summary>
        /// <param name="userProfileId"></param>
        private bool UpdateClassmatesAvailable(int userProfileId)
        {
            bool generateIntervention = false;
            //get users's courses
            List<int> userCourses = GetAllUserCourseIds(userProfileId);
            //check if any users status is set to 'isAvailable' for these courses.
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    //return a result if  exclude themselves
                    string query = "SELECT COUNT(*) AS Count FROM UserStatus WHERE CourseId IN @CourseIds AND AvailableEndTime > GETDATE()  AND UserProfileId != @UserProfileId ";

                    int count = sqlConnection.Query<int>(query, new { UserProfileId = userProfileId, CourseIds = userCourses }).Single();

                    if (count > 0)
                    {
                        InterventionItem currentIntervention = GetInterventionFromUserProfileAndType(userProfileId, "ClassmatesAvailable");
                        if (currentIntervention.Id != -1) //they are already seeing this intervention, just update the date
                        {
                            currentIntervention.InterventionDateTime = DateTime.UtcNow;
                            bool updateSuccess = UpdateIntervention(currentIntervention);
                        }
                        else //there are users available and the user doesn't already have a live ClassmatesAvailable suggestion so generate one
                        {
                            generateIntervention = true;
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("UpdateClassmatesAvailable() Failed", e);
            }
            //if so, generate the intervention
            if (generateIntervention)
            {
                SaveIntervention(GenerateClassmatesAvailableIntervention(userProfileId));
            }
            return generateIntervention;
        }

        private bool UpdateIntervention(InterventionItem intervention)
        {
            bool updateSuccess = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "UPDATE [dbo].[OSBLEInterventions] " +
                                   "SET UserProfileId = @UserProfileId, " +
                                        "InterventionTrigger = @InterventionTrigger, " +
                                        "InterventionMarkedHelpful = @InterventionMarkedHelpful, " +
                                        "InterventionDateTime = @InterventionDateTime, " +
                                        "InterventionType = @InterventionType, " +
                                        "Icon1 = @Icon1, " +
                                        "Icon2 = @Icon2, " +
                                        "Title = @Title, " +
                                        "Link = @Link, " +
                                        "LinkText = @LinkText, " +
                                        "ContentFirst = @ContentFirst, " +
                                        "ListItemContent = @ListItemContent, " +
                                        "InterventionTemplateText = @InterventionTemplateText, " +
                                        "InterventionSuggestedCode = @InterventionSuggestedCode, " +
                                        "IsDismissed = @IsDismissed " +
                                 "WHERE Id = @Id";

                    updateSuccess = sqlConnection.Execute(query, new
                    {
                        Id = intervention.Id,
                        UserProfileId = intervention.UserProfileId,
                        InterventionTrigger = intervention.InterventionTrigger,
                        InterventionMarkedHelpful = intervention.InterventionMarkedHelpful,
                        InterventionDateTime = intervention.InterventionDateTime,
                        InterventionType = intervention.InterventionType,
                        Icon1 = intervention.Icon1,
                        Icon2 = intervention.Icon2,
                        Title = intervention.Title,
                        Link = intervention.Link,
                        LinkText = intervention.LinkText,
                        ContentFirst = intervention.ContentFirst,
                        ListItemContent = intervention.ListItemContent,
                        InterventionTemplateText = intervention.InterventionTemplateText,
                        InterventionSuggestedCode = intervention.InterventionSuggestedCode,
                        IsDismissed = intervention.IsDismissed,
                    }) != 0;

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("UpdateIntervention() Failed", e);
            }
            return updateSuccess;
        }
    }

    public class UpdateUserStatus
    {
        public UpdateUserStatus()
        {
            UserProfileId = -1;
            ActiveCourseId = -1;
            IsUserStatusUpdate = false;
            WasAvailableToHelp = false;
            IsAvailableToHelp = false;
        }

        public int ActiveCourseId { get; set; }
        public bool IsAvailableToHelp { get; set; }
        public bool IsUserStatusUpdate { get; set; }
        public int UserProfileId { get; set; }
        public bool WasAvailableToHelp { get; set; }
    }
}
