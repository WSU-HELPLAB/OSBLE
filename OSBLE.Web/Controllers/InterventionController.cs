using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.IO;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using OSBLE.Models.Assignments;
using OSBLE.Models;
using Dapper;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using OSBLEPlus.Logic;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using System.Configuration;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.Utility;
using OSBLE.Utility;
using OSBLE.Models.Intervention;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Services.Controllers;
using OSBLEPlus.Logic.Utility.Auth;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    public class InterventionController : OSBLEController
    {
        private const int InterventionExpirationInDays = -7; //expire interventions if they are one week old and not dismissed
        public InterventionController()
        {

        }

        public static InterventionItem GetInterventionItem(int interventionId)
        {
            InterventionItem intervention = new InterventionItem();

            if (interventionId > 0)
            {
                try
                {
                    using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        sqlConnection.Open();

                        string query = "SELECT * FROM OSBLEInterventions WHERE Id = @InterventionId";
                        var result = sqlConnection.Query(query, new { InterventionId = interventionId }).Single();

                        //Parse results
                        intervention.Id = result.Id;
                        intervention.UserProfileId = result.UserProfileId;
                        intervention.InterventionTrigger = result.InterventionTrigger;
                        intervention.InterventionMarkedHelpful = result.InterventionMarkedHelpful ?? false;
                        intervention.InterventionDateTime = result.InterventionDateTime;
                        intervention.InterventionType = InterventionTypesExtensions.Explode(InterventionTypesExtensions.ParseEnum<InterventionTypes>(result.InterventionType));
                        intervention.Icon1 = result.Icon1;
                        intervention.Icon2 = result.Icon2;
                        intervention.Title = result.Title;
                        intervention.Link = string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, result.InterventionType, result.Id);
                        intervention.LinkText = result.LinkText;
                        //TODO: Populate the template text from the database, for now just use the canned response from the model.
                        //intervention.ContentFirst = result.ContentFirst;
                        intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(InterventionTypesExtensions.ParseEnum<InterventionTypes>(result.InterventionType));
                        intervention.ListItemContent = result.ListItemContent;
                        //TODO: Populate the template text from the database, for now just use the canned response from the model.
                        //intervention.InterventionTemplateText = result.InterventionTemplateText;
                        intervention.InterventionTemplateText = InterventionTypesExtensions.TemplateText(InterventionTypesExtensions.ParseEnum<InterventionTypes>(result.InterventionType));
                        intervention.InterventionSuggestedCode = result.InterventionSuggestedCode;
                        intervention.IsDismissed = result.IsDismissed ?? false;

                        sqlConnection.Close();
                    }
                }
                catch (Exception e)
                {
                    //TODO: handle exception logging
                    //failure
                }
            }

            return intervention;
        }

        public ActionResult AskForHelp(int interventionId)
        {
            LogInterventionInteraction("AskForHelp() page loaded", interventionId, 0, "", "", GetUrlReferrer());

            BuildCourseSelectViewBag();
            SetupTaggingViewBag();
            BuildFeedbackItemViewBag(interventionId);

            return PartialView("_AskForHelp", GetInterventionDetails(interventionId));
        }

        public ActionResult Availability()
        {
            LogInterventionInteraction("Availability() page loaded", -10, 0, "", "", GetUrlReferrer());

            AvailableDetailsViewModel vm = new AvailableDetailsViewModel();

            //don't need intervention details, this view is independent of an intervention           

            //Lookup available classmates
            vm.AvailableUsers = GetAvailableUsers(ActiveCourseUser.AbstractCourseID);
            List<UserStatus> courseTimeStatus = GetAllUsersStatus(ActiveCourseUser.AbstractCourseID);

            //Convert times to course time             
            foreach (var user in courseTimeStatus)
            {
                vm.UsersStatus.Add(new UserStatus()
                {
                    UserProfileId = user.UserProfileId,
                    CourseId = user.CourseId,
                    StatusMessage = user.StatusMessage,
                    IsAvailableToHelp = user.IsAvailableToHelp,
                    AvailableStartTime = DateTimeExtensions.UTCToCourse(user.AvailableStartTime, ActiveCourseUser.AbstractCourseID),
                    AvailableEndTime = DateTimeExtensions.UTCToCourse(user.AvailableEndTime, ActiveCourseUser.AbstractCourseID)
                });
            }

            vm.CurrentUserStatus = vm.UsersStatus.Where(us => us.UserProfileId == ActiveCourseUser.UserProfileID).FirstOrDefault();

            SetupTaggingViewBag();

            ViewBag.CurrentUserName = ActiveCourseUser.UserProfile.FullName;
            ViewBag.CurrentUserProfileId = ActiveCourseUser.UserProfileID;
            ViewBag.CurrentUserCourseRole = ActiveCourseUser.AbstractRoleID;

            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(0, "Availability Details");

            return PartialView("_Availability", vm);
        }

        public ActionResult AvailableDetails(int interventionId)
        {
            LogInterventionInteraction("AvailableDetails() page loaded", interventionId, 0, "", "", GetUrlReferrer());

            AvailableDetailsViewModel vm = new AvailableDetailsViewModel();

            vm.Intervention = GetInterventionDetails(interventionId);

            //Lookup available classmates
            vm.AvailableUsers = GetAvailableUsers(ActiveCourseUser.AbstractCourseID);
            List<UserStatus> courseTimeStatus = GetAllUsersStatus(ActiveCourseUser.AbstractCourseID);

            //Convert times to course time             
            foreach (var user in courseTimeStatus)
            {
                vm.UsersStatus.Add(new UserStatus()
                {
                    UserProfileId = user.UserProfileId,
                    CourseId = user.CourseId,
                    StatusMessage = user.StatusMessage,
                    IsAvailableToHelp = user.IsAvailableToHelp,
                    AvailableStartTime = DateTimeExtensions.UTCToCourse(user.AvailableStartTime, ActiveCourseUser.AbstractCourseID),
                    AvailableEndTime = DateTimeExtensions.UTCToCourse(user.AvailableEndTime, ActiveCourseUser.AbstractCourseID)
                });
            }

            vm.CurrentUserStatus = vm.UsersStatus.Where(us => us.UserProfileId == ActiveCourseUser.UserProfileID).FirstOrDefault();

            SetupTaggingViewBag();

            ViewBag.CurrentUserName = ActiveCourseUser.UserProfile.FullName;
            ViewBag.CurrentUserProfileId = ActiveCourseUser.UserProfileID;

            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(interventionId);

            return PartialView("_AvailableDetails", vm);
        }

        public ActionResult BuildFailure(int interventionId)
        {
            LogInterventionInteraction("BuildFailure() page loaded", interventionId, 0, "", "", GetUrlReferrer());

            BuildCourseSelectViewBag();
            SetupTaggingViewBag();
            BuildFeedbackItemViewBag(interventionId);

            return PartialView("_BuildFailure", GetInterventionDetails(interventionId));
        }

        public ActionResult ClassmatesAvailable(int interventionId)
        {
            LogInterventionInteraction("ClassmatesAvailable() page loaded", interventionId, 0, "", "", GetUrlReferrer());

            AvailableDetailsViewModel vm = new AvailableDetailsViewModel();

            vm.Intervention = GetInterventionDetails(interventionId);

            //Lookup available classmates
            vm.AvailableUsers = GetAvailableUsers(ActiveCourseUser.AbstractCourseID);
            List<UserStatus> courseTimeStatus = GetAllUsersStatus(ActiveCourseUser.AbstractCourseID);

            //Convert times to course time             
            foreach (var user in courseTimeStatus)
            {
                vm.UsersStatus.Add(new UserStatus()
                {
                    UserProfileId = user.UserProfileId,
                    CourseId = user.CourseId,
                    StatusMessage = user.StatusMessage,
                    IsAvailableToHelp = user.IsAvailableToHelp,
                    AvailableStartTime = DateTimeExtensions.UTCToCourse(user.AvailableStartTime, ActiveCourseUser.AbstractCourseID),
                    AvailableEndTime = DateTimeExtensions.UTCToCourse(user.AvailableEndTime, ActiveCourseUser.AbstractCourseID)
                });
            }

            vm.CurrentUserStatus = vm.UsersStatus.Where(us => us.UserProfileId == ActiveCourseUser.UserProfileID).FirstOrDefault();

            SetupTaggingViewBag();

            ViewBag.CurrentUserName = ActiveCourseUser.UserProfile.FullName;
            ViewBag.CurrentUserProfileId = ActiveCourseUser.UserProfileID;
            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(interventionId);

            return PartialView("_ClassmatesAvailable", vm);
        }

        public ActionResult DismissedInterventions(int userId = 0)
        {
            LogInterventionInteraction("DismissedInterventions() page loaded");

            if (userId == 0)
            {
                userId = ActiveCourseUser.UserProfileID;
            }

            //Get all interventions for the current user
            InterventionsList vm = BuildInterventionsViewModel(userId, true);

            BuildFeedbackItemViewBag(0, "View Dismissed Suggestions");

            return PartialView("_DismissedInterventions", vm);
        }

        public ActionResult DismissedInterventionsLayout(int userId = 0)
        {
            LogInterventionInteraction("DismissedInterventionsLayout() page loaded");

            if (userId == 0)
            {
                userId = ActiveCourseUser.UserProfileID;
            }

            //Get all interventions for the current user
            InterventionsList vm = BuildInterventionsViewModel(userId, true);

            BuildFeedbackItemViewBag(0, "View Dismissed Suggestions");

            return View("DismissedInterventions", vm);
        }

        [HttpPost]
        public bool DismissIntervention(int interventionId, bool autoDismiss = false)
        {
            if (autoDismiss)
            {
                LogInterventionInteraction("DismissIntervention() AUTOMATED INITIATION.", interventionId, -1, "", "", "Automatically dismissed to replace with updated intervention");
            }
            else
            {
                LogInterventionInteraction("DismissIntervention() Clicked.", interventionId, 0, "", "", GetUrlReferrer());
            }

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
                //TODO: handle exception logging
                //failure
            }
            return isDismissed;
        }

        public ActionResult Index(int userId = 0)
        {
            LogInterventionInteraction("Intervention Index() page loaded");

            if (userId == 0)
            {
                userId = ActiveCourseUser.UserProfileID;
            }

            //check if we need to generate a 'ClassmatesAvailable' intervention first. try catch in case this breaks
            try
            {
                bool generateIntervention = CheckIfOtherUsersAreAvailable(userId, ActiveCourseUser.AbstractCourseID);
                if (generateIntervention)
                {
                    SaveIntervention(GenerateClassmatesAvailableIntervention(userId));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to CheckIfOtherUsersAreAvailable in Index()", e);
            }


            //Get all interventions for the current user
            InterventionsList vm = BuildInterventionsViewModel(userId);

            return PartialView("_Intervention", vm);
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

        private bool SaveIntervention(InterventionItem intervention)
        {
            bool dismissSuccess = false;
            bool insertSuccess = false;
            //first make sure that no intervention of the same type exists for this user.
            InterventionItem existingIntervention = GetInterventionFromUserProfileAndType(intervention.UserProfileId, intervention.InterventionType);

            if (existingIntervention.InterventionType != "ClassmatesAvailable") //no need to save another one if this one already exists!
            {
                if (existingIntervention.Id != -1) //one already exists, dismiss it first before saving the new one
                {
                    if (InterventionRequiresRefresh(existingIntervention)) //check for certain types that need to be refreshed if they exist e.g. build/runtime errors need the latest info while unanswered questions wont.
                    {
                        dismissSuccess = DismissIntervention(existingIntervention.Id, true);
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
                        throw new Exception(String.Format("Failed to insert new intervention. UserProfileId: {0}, Trigger: {1}, DateTime: {2}, TemplateText: {3}, SuggestedCode: {4} ", intervention.UserProfileId, intervention.InterventionTrigger, intervention.InterventionDateTime, intervention.InterventionTemplateText, intervention.InterventionSuggestedCode), e);
                    }
                }
            }

            if (insertSuccess)
            {
                //we don't need to trigger here since we are generating this one before loading the index... leaving here for now just in case...
                //TriggerInterventionRefresh(intervention.UserProfileId);
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

        [HttpPost]
        public string IsProgrammingCourse(int courseId, string suggestionsType)
        {
            bool isProgrammingCourse = DBHelper.GetIsProgrammingCourseSetting(courseId);

            if (isProgrammingCourse) //return 'true', dont' make any change to the template
            {
                return "true";
            }
            else //non-programming course, modify the template text based on suggestion type
            {
                switch (suggestionsType)
                {
                    case "ClassmatesAvailable":
                        return "Hey all, I having difficulty with [... describe current task ...]. I am running into [... describe the difficulty here ...]. Does anyone have any tips on how I could resolve this?\n\nI have tried [... describe what you've tried so far...]\n\nThanks!";
                        break;
                    case "OfferHelp":
                        return "You have successfully submitted your assignment early! Some of your classmates' may be having difficulty...";
                        break;
                    default:
                        return "true";
                        break;
                }
            }            
        }

        [HttpPost]
        public DateTime GetLastRefreshTime(int userProfileId = 0)
        {
            if (userProfileId == 0)
            {
                userProfileId = ActiveCourseUser.UserProfileID;
            }
            DateTime lastRefresh = DBHelper.GetInterventionLastRefreshTime(userProfileId).UTCToCourse(ActiveCourseUser != null ? ActiveCourseUser.AbstractCourseID : 1);

            return lastRefresh;
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

        [HttpPost]
        public void LogClick(int interventionId = -10, string currentpage = "", string action = "")
        {
            LogInterventionInteraction(action, interventionId, ActiveCourseUser.UserProfileID, "", "", currentpage);
        }

        public ActionResult MakeAPost(int interventionId)
        {
            LogInterventionInteraction("MakeAPost() page loaded", interventionId, 0, "", "", GetUrlReferrer());

            MakeAPostViewModel vm = new MakeAPostViewModel();

            //get intervention details
            vm.Intervention = GetInterventionDetails(interventionId);

            //Lookup popular hashtags
            vm.PopularHashtags = GetPopularHashtags(ActiveCourseUser.AbstractCourseID, 14);

            SetupTaggingViewBag();
            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(interventionId);

            return PartialView("_MakeAPost", vm);
        }

        public ActionResult MakeAPostAssignmentSubmit(int interventionId)
        {
            LogInterventionInteraction("MakeAPostAssignmentSubmit() page loaded", interventionId, 0, "", "", GetUrlReferrer());

            SetupTaggingViewBag();
            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(interventionId);

            return PartialView("_MakeAPostAssignmentSubmit", GetInterventionDetails(interventionId));
        }

        public void NotifyHub(int eventLogId, int senderUserProfileId, string eventTypeId, int? courseId, string authKey)
        {
            using (EventCollectionController ecc = new EventCollectionController())
            {
                //need to send in auth key                                       
                ecc.NotifyHub(eventLogId, senderUserProfileId, eventTypeId, courseId ?? 0, authKey);
            }
        }

        public ActionResult OfferHelp(int interventionId)
        {
            LogInterventionInteraction("OfferHelp() page loaded", interventionId, 0, "", "", GetUrlReferrer());

            AvailableDetailsViewModel vm = new AvailableDetailsViewModel();

            vm.Intervention = GetInterventionDetails(interventionId);

            //Lookup available classmates
            vm.AvailableUsers = GetAvailableUsers(ActiveCourseUser.AbstractCourseID);
            List<UserStatus> courseTimeStatus = GetAllUsersStatus(ActiveCourseUser.AbstractCourseID);

            //Convert times to course time             
            foreach (var user in courseTimeStatus)
            {
                vm.UsersStatus.Add(new UserStatus()
                {
                    UserProfileId = user.UserProfileId,
                    CourseId = user.CourseId,
                    StatusMessage = user.StatusMessage,
                    IsAvailableToHelp = user.IsAvailableToHelp,
                    AvailableStartTime = DateTimeExtensions.UTCToCourse(user.AvailableStartTime, ActiveCourseUser.AbstractCourseID),
                    AvailableEndTime = DateTimeExtensions.UTCToCourse(user.AvailableEndTime, ActiveCourseUser.AbstractCourseID)
                });
            }

            vm.CurrentUserStatus = vm.UsersStatus.Where(us => us.UserProfileId == ActiveCourseUser.UserProfileID).FirstOrDefault();

            SetupTaggingViewBag();

            ViewBag.CurrentUserName = ActiveCourseUser.UserProfile.FullName;
            ViewBag.CurrentUserProfileId = ActiveCourseUser.UserProfileID;

            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(interventionId);

            return PartialView("_OfferHelp", vm);
        }

        [HttpGet]
        [OsbleAuthorize]
        public ActionResult PopulateSuggestionList()
        {
            int courseId = ActiveCourseUser != null ? ActiveCourseUser.AbstractCourseID : 0;
            int userProfileId = ActiveCourseUser != null ? ActiveCourseUser.UserProfileID : 0;

            if (ActiveCourseUser == null) //try to get the current user and course
            {
                try
                {
                    string authToken = Request.Cookies["AuthKey"].Value.Split('=').Last();
                    var auth = new Authentication();

                    if (auth.IsValidKey(authToken))
                    {
                        userProfileId = auth.GetActiveUserId(authToken);

                        using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                        {
                            sqlConnection.Open();
                            string query = "SELECT ISNULL((SELECT TOP 1 ac.ID FROM AbstractCourses ac INNER JOIN CourseUsers cu ON ac.ID = cu.AbstractCourseID " +
                                           "INNER JOIN OSBLEInterventionsCourses oic ON ac.ID = oic.CourseId INNER JOIN EventLogs el ON ac.ID = el.CourseId " +
                                           "WHERE cu.UserProfileID = @UserProfileId ORDER BY EventDate DESC), 0) as CourseId ";

                            var result = sqlConnection.Query(query, new { UserProfileId = userProfileId }).SingleOrDefault();

                            courseId = result.CourseId;

                            sqlConnection.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("PopulateSuggestionList() ActiveCourseuser is Null and failed to get Ids from authToken", e);
                }
            }

            InterventionsList vm = new InterventionsList();

            if (userProfileId > 0 && courseId > 0)
            {
                //check if we need to generate a 'ClassmatesAvailable' intervention first. try catch in case this breaks
                try
                {
                    bool generateIntervention = CheckIfOtherUsersAreAvailable(userProfileId, courseId);
                    if (generateIntervention)
                    {
                        SaveIntervention(GenerateClassmatesAvailableIntervention(userProfileId));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to CheckIfOtherUsersAreAvailable in Index()", e);
                }
                //Get all interventions for the current user
                vm = BuildInterventionsViewModel(userProfileId);
            }

            return PartialView("_InterventionList", vm);
        }

        [HttpPost]
        [ValidateInput(false)]
        public bool PostToActivityFeed(string postContent, string codeSnippet, bool isAnonymous = false)
        {
            // Parse text and add any new hashtags to database
            FeedController.ParseHashTags(postContent);

            Dictionary<string, string> userNameIdPairs = FeedController.GetProfileNames(ActiveCourseUser.AbstractCourseID);

            // Replace all occurrences of @user with @id;
            foreach (KeyValuePair<string, string> user in userNameIdPairs)
            {
                string name = user.Value.Replace(" ", "");
                string id = "@id=" + user.Key + ";";
                //text = text.replace(name, id);
                string atMention = "@" + name;
                postContent = postContent.Replace(atMention, id);
            }

            AskForHelpEvent post = new AskForHelpEvent();

            post.EventTypeId = 1; //AskForHelpEvent
            //post.EventDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified); //don't need to convert to course time since this is grabbing server time
            post.SenderId = ActiveCourseUser.UserProfileID;
            post.CourseId = ActiveCourseUser.AbstractCourseID;
            post.Code = codeSnippet;
            post.UserComment = postContent;
            post.SolutionName = "";
            post.IsAnonymous = isAnonymous;

            int result = Posts.SaveEvent(post);

            if (result > 0)
            {
                string authKey = Request.Cookies["AuthKey"].Value.Split('=').Last();
                NotifyHub(result, post.SenderId, post.EventTypeId.ToString(), post.CourseId ?? 0, authKey);
                return true;
            }
            else
            {
                return false;
            }
        }

        public ActionResult PrivateMessages()
        {
            //this will use the same viewmodel as unasnwered questions, it will just use a different query to populate Ids.
            UnansweredQuestionsViewModel vm = new UnansweredQuestionsViewModel();
            vm.UnansweredPostIds = GetPrivateFeedPosts(ActiveCourseUser.UserProfileID, ActiveCourseUser.AbstractCourseID);
            vm.Intervention.Id = -4; //no intervention id for this page, populate with -4 for feedback links

            //Load the ViewBag
            ViewBag.CurrentCourseId = ActiveCourseUser.AbstractCourseID;
            //CurrentUser
            ViewBag.CurrentUserProfileId = ActiveCourseUser.UserProfileID;
            ViewBag.CurrentUserFullName = ActiveCourseUser.UserProfile.FullName;
            //CourseUsers
            ViewBag.CurrentCourseUsers = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            //Hashtags
            ViewBag.HashTags = DBHelper.GetHashTags();

            ViewBag.HideLoadMore = true;

            //misc viewbag items for feedposts
            ViewBag.ActiveCourse = ActiveCourseUser;
            ViewBag.EnableCustomPostVisibility = ConfigurationManager.AppSettings["EnableCustomPostVisibility"]; //<add key="EnableCustomPostVisibility" value="false"/> in web.config

            //get user courses
            List<int> currentUserCourseIds = DBHelper.GetActiveCourseIds(ActiveCourseUser.UserProfileID);
            Dictionary<int, string> currentUserCourseIdCourseName = new Dictionary<int, string>();
            foreach (int id in currentUserCourseIds)
            {
                currentUserCourseIdCourseName.Add(id, DBHelper.GetCourseFullNameFromCourseId(id));
            }

            ViewBag.CurrentUserCoursesIdCourseName = currentUserCourseIdCourseName;
            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                ViewBag.IsInstructor = true;
            else
                ViewBag.IsInstructor = false;
            ViewBag.CanGrade = ActiveCourseUser.AbstractRole.CanGrade;

            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(0, "View Private Messages");

            LogInterventionInteraction("PrivateMessages() page loaded", -10, 0, "", "", "EventLogIds: " + vm.UnansweredPostIds + " ReferrerUrl: " + GetUrlReferrer());

            return PartialView("_PrivateMessages", vm);
        }

        public ActionResult RuntimeErrors(int interventionId)
        {
            LogInterventionInteraction("RuntimeErrors() page loaded", interventionId, 0, "", "", GetUrlReferrer());

            BuildCourseSelectViewBag();
            SetupTaggingViewBag();
            BuildFeedbackItemViewBag(interventionId);

            return PartialView("_RuntimeError", GetInterventionDetails(interventionId));
        }

        public ActionResult SuggestionsSettings()
        {
            try
            {
                int userProfileId = ActiveCourseUser.UserProfileID;

                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionSettings WHERE UserProfileId = @UserProfileId ";

                    var results = sqlConnection.Query(query, new { UserProfileId = userProfileId }).SingleOrDefault();
                    if (results != null)
                    {
                        ViewBag.ShowSuggestionsWindow = results.ShowInIDESuggestions;
                        ViewBag.RefreshThreshold = results.RefreshThreshold;
                    }
                    else
                    {
                        ViewBag.ShowSuggestionsWindow = true;
                        ViewBag.RefreshThreshold = 10;
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in SuggestionsSettings()", e);
            }

            BuildFeedbackItemViewBag(0, "OSBLE+ Suggestions Dashboard Settings");

            return View();
        }

        [HttpPost]
        [OsbleAuthorize]
        public ActionResult UpdateSuggestionsSettings(bool enableSuggestionsIDE, int refreshThreshold)
        {
            int userProfileId = ActiveCourseUser.UserProfileID;
            bool updateSuccess = false;

            //enable/disable in-IDE suggestions
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionSettings WHERE UserProfileId = @UserProfileId ";
                    string updateQuery = "UPDATE OSBLEInterventionSettings SET [ShowInIDESuggestions] = @ShowInIDESuggestions, [RefreshThreshold] = @RefreshThreshold WHERE UserProfileId = @UserProfileId ";
                    string insertQuery = "INSERT INTO OSBLEInterventionSettings ([UserProfileId], [ShowInIDESuggestions], [RefreshThreshold]) VALUES( @UserProfileId, @ShowInIDESuggestions, @RefreshThreshold) ";

                    var results = sqlConnection.Query(query, new { UserProfileId = userProfileId });

                    if (results.Count() == 0) //insert
                    {
                        updateSuccess = sqlConnection.Execute(insertQuery, new { UserProfileId = userProfileId, ShowInIDESuggestions = enableSuggestionsIDE, RefreshThreshold = refreshThreshold }) != 0;
                        LogInterventionInteraction("UpdateSuggestionsSettings", -10, 0, "enableSuggestionsIDEBefore: Default Settings", "enableSuggestionsIDEAfter: " + enableSuggestionsIDE.ToString() + " refreshThresholdAfter: " + refreshThreshold.ToString(), "ReferrerUrl: " + GetUrlReferrer());
                    }
                    else //update
                    {
                        updateSuccess = sqlConnection.Execute(updateQuery, new { UserProfileId = userProfileId, ShowInIDESuggestions = enableSuggestionsIDE, RefreshThreshold = refreshThreshold }) != 0;
                        LogInterventionInteraction("UpdateSuggestionsSettings", -10, 0, "enableSuggestionsIDEBefore: " + results.First().ShowInIDESuggestions.ToString() + " refreshThresholdBefore: " + results.First().RefreshThreshold.ToString(), "enableSuggestionsIDEAfter: " + enableSuggestionsIDE.ToString() + " refreshThresholdAfter: " + refreshThreshold.ToString(), "ReferrerUrl: " + GetUrlReferrer());
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in UpdateSuggestionsSettings()", e);
            }

            return View("SuggestionsSettings");
        }

        public void SetupTaggingViewBag()
        {
            //setup user list for autocomplete            
            ViewBag.CurrentCourseUsers = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            ViewBag.HashTags = DBHelper.GetHashTags();
        }

        [HttpPost]
        public bool SubmitFeedback(int interventionId, string feedbackDetails, string feedbackComment, string currentUrl)
        {
            return UpdateInterventionFeedback(interventionId, feedbackDetails, feedbackComment, currentUrl);
        }

        [HttpPost]
        public bool SubmitFeedbackVote(int interventionId, string feedbackDetails, string currentUrl)
        {
            return UpdateInterventionFeedback(interventionId, feedbackDetails, "", currentUrl);
        }

        public ActionResult UnansweredQuestions(int interventionId)
        {
            UnansweredQuestionsViewModel vm = new UnansweredQuestionsViewModel();
            vm.UnansweredPostIds = GetUnansweredFeedPosts(ActiveCourseUser.AbstractCourseID, 14); //default 2 weeks back
            vm.Intervention = GetInterventionDetails(interventionId);

            //Load the ViewBag
            ViewBag.CurrentCourseId = ActiveCourseUser.AbstractCourseID;
            //CurrentUser
            ViewBag.CurrentUserProfileId = ActiveCourseUser.UserProfileID;
            ViewBag.CurrentUserFullName = ActiveCourseUser.UserProfile.FullName;
            //CourseUsers
            ViewBag.CurrentCourseUsers = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            //Hashtags
            ViewBag.HashTags = DBHelper.GetHashTags();

            ViewBag.HideLoadMore = true;

            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(interventionId);

            LogInterventionInteraction("UnansweredQuestions() page loaded", interventionId, 0, "", "", "EventLogIds: " + vm.UnansweredPostIds + " ReferrerUrl: " + GetUrlReferrer());

            return PartialView("_UnansweredQuestions", vm);
        }

        public ActionResult UnansweredQuestionsLayout()
        {
            UnansweredQuestionsViewModel vm = new UnansweredQuestionsViewModel();
            vm.UnansweredPostIds = GetUnansweredFeedPosts(ActiveCourseUser.AbstractCourseID, 14); //default 2 weeks back
            vm.Intervention = new InterventionItem();
            vm.Intervention.Id = -5; //id for this page, used to keep track of feedback

            //Load the ViewBag
            ViewBag.CurrentCourseId = ActiveCourseUser.AbstractCourseID;
            //CurrentUser
            ViewBag.CurrentUserProfileId = ActiveCourseUser.UserProfileID;
            ViewBag.CurrentUserFullName = ActiveCourseUser.UserProfile.FullName;
            //CourseUsers
            ViewBag.CurrentCourseUsers = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            //Hashtags
            ViewBag.HashTags = DBHelper.GetHashTags();
            //Current User Role ID
            ViewBag.CurrentUserCourseRole = ActiveCourseUser.AbstractRoleID;

            ViewBag.HideLoadMore = true;

            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(0, "Unanswered Questions");

            LogInterventionInteraction("UnansweredQuestionsLayout() page loaded", vm.Intervention.Id, 0, "", "", "EventLogIds: " + vm.UnansweredPostIds + " ReferrerUrl: " + GetUrlReferrer());

            return PartialView("UnansweredQuestions", vm);
        }

        public ActionResult UnansweredQuestionsAlternate(int interventionId)
        {
            UnansweredQuestionsViewModel vm = new UnansweredQuestionsViewModel();
            vm.UnansweredPostIds = GetUnansweredFeedPosts(ActiveCourseUser.AbstractCourseID, 14); //default 2 weeks back
            vm.Intervention = GetInterventionDetails(interventionId);

            //Load the ViewBag
            ViewBag.CurrentCourseId = ActiveCourseUser.AbstractCourseID;
            //CurrentUser
            ViewBag.CurrentUserProfileId = ActiveCourseUser.UserProfileID;
            ViewBag.CurrentUserFullName = ActiveCourseUser.UserProfile.FullName;
            //CourseUsers
            ViewBag.CurrentCourseUsers = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            //Hashtags
            ViewBag.HashTags = DBHelper.GetHashTags();

            ViewBag.HideLoadMore = true;

            BuildCourseSelectViewBag();
            BuildFeedbackItemViewBag(interventionId);

            LogInterventionInteraction("UnansweredQuestionsAlternate() page loaded", interventionId, 0, "", "", "EventLogIds: " + vm.UnansweredPostIds + " ReferrerUrl: " + GetUrlReferrer());

            return PartialView("_UnansweredQuestionsAlternate", vm);
        }

        [HttpPost]
        [ValidateInput(false)]
        public bool UpdateSuggestedCode(int interventionId, string suggestedCode)
        {
            string suggestedCodeBefore = "";
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string selectQuery = "SELECT InterventionSuggestedCode FROM OSBLEInterventions WHERE Id = @InterventionId";

                    suggestedCodeBefore = sqlConnection.Query<string>(selectQuery, new { InterventionId = interventionId }).Single();

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }

            LogInterventionInteraction("UpdateSuggestedCode() page loaded", interventionId, 0, suggestedCodeBefore, suggestedCode, "ReferrerUrl: " + GetUrlReferrer());

            bool updateSuccess = false;

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "UPDATE OSBLEInterventions SET InterventionSuggestedCode = @SuggestedCode WHERE Id = @InterventionId";

                    updateSuccess = sqlConnection.Execute(query, new { SuggestedCode = suggestedCode, InterventionId = interventionId }) != 0;

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }

            return updateSuccess;
        }

        [HttpPost]
        public async Task<bool> UpdateUserStatus(string userStatus, string availableStartTime, string availableEndTime, bool isAvailableToHelp, string month, string day)
        {
            int userProfileId = ActiveCourseUser.UserProfileID;
            int courseId = ActiveCourseUser.AbstractCourseID;
            string statusBefore = "";
            bool availableBefore = false;

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string selectQuery = "SELECT UserStatus, IsAvailableToHelp FROM UserStatus WHERE UserProfileId = @UserProfileId AND CourseId = @CourseId";

                    var result = sqlConnection.Query(selectQuery, new { UserProfileId = userProfileId, CourseId = courseId });

                    if (result.Count() != 0)
                    {
                        statusBefore = result.First().UserStatus.ToString();
                        availableBefore = Convert.ToBoolean(result.First().IsAvailableToHelp.ToString());
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }

            LogInterventionInteraction("User Updated Status", -10, 0, "StatusBefore: " + statusBefore + " AvailableToHelpBefore: " + availableBefore.ToString(), "StatusAfter: " + userStatus + " AvailableToHelpAfter: " + isAvailableToHelp.ToString(), "ReferrerUrl: " + GetUrlReferrer());

            bool updateSuccess = false;

            availableStartTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).ToString(); //for now just set start as right now. Because this is server time we don't have to convert from course time.

            //parse the end time
            DateTime outTime;
            bool timeParse = DateTime.TryParse(availableEndTime, out outTime);

            //if the user is marking themselves as not available, we don't need an end time
            bool userNotAvailable = availableEndTime == "" && !isAvailableToHelp ? true : false;

            if (timeParse || userNotAvailable) // only proceed if we've successfully parsed the time or if the user is marking themselves unavailable
            {
                if (isAvailableToHelp)
                {
                    outTime = new DateTime(outTime.Year, Int16.Parse(month), Int16.Parse(day), outTime.Hour, outTime.Minute, outTime.Second);
                    //convert from course time to UTC for the db   
                    availableEndTime = DateTimeExtensions.CourseToUTC(DateTime.SpecifyKind(outTime, DateTimeKind.Unspecified), ActiveCourseUser.AbstractCourseID).ToString();
                }

                try
                {
                    using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        sqlConnection.Open();

                        string query = "SELECT * FROM UserStatus WHERE UserProfileId = @UserProfileId AND CourseId = @CourseId";
                        string updateQuery = "UPDATE [UserStatus] SET " +
                                             "UserStatus = @UserStatus, AvailableStartTime = @AvailableStartTime, AvailableEndTime = @AvailableEndTime, IsAvailableToHelp = @IsAvailableToHelp " +
                                             "WHERE UserProfileId = @UserProfileId AND CourseId = @CourseId";
                        string insertQuery = "INSERT INTO [UserStatus] (UserProfileId, CourseId, UserStatus, AvailableStartTime, AvailableEndTime, IsAvailableToHelp) " +
                                             "VALUES (@UserProfileId, @CourseId, @UserStatus, @AvailableStartTime, @AvailableEndTime, @IsAvailableToHelp)";

                        var results = sqlConnection.Query(query, new { UserProfileId = userProfileId, CourseId = courseId });

                        if (results.Count() == 0) //insert
                        {
                            updateSuccess = sqlConnection.Execute(insertQuery, new { UserProfileId = userProfileId, CourseId = courseId, UserStatus = userStatus, AvailableStartTime = availableStartTime, AvailableEndTime = availableEndTime, IsAvailableToHelp = isAvailableToHelp }) != 0;
                        }
                        else //update
                        {
                            updateSuccess = sqlConnection.Execute(updateQuery, new { UserProfileId = userProfileId, CourseId = courseId, UserStatus = userStatus, AvailableStartTime = availableStartTime, AvailableEndTime = availableEndTime, IsAvailableToHelp = isAvailableToHelp }) != 0;
                        }

                        sqlConnection.Close();
                    }
                }
                catch (Exception e)
                {
                    //TODO: handle exception logging
                    //failure
                }
            }

            //forward update status to the intervention webapi controller for processing
            await ForwardStatusUpdate(userProfileId, ActiveCourseUser.AbstractCourseID, availableBefore, isAvailableToHelp, true);

            return updateSuccess;
        }

        public async Task<bool> ForwardStatusUpdate(int userProfileId, int courseId, bool wasAvailableToHelp, bool isAvailableToHelp, bool isStatusUpdate = false)
        {
            using (var client = new HttpClient())
            {
                var request = new UpdateUserStatus
                {
                    UserProfileId = userProfileId,
                    ActiveCourseId = courseId,
                    IsAvailableToHelp = isAvailableToHelp,
                    WasAvailableToHelp = wasAvailableToHelp,
                    IsUserStatusUpdate = isStatusUpdate,
                };

                var task = client.PostAsXmlAsync(string.Format("{0}api/intervention/UpdateUserStatus", StringConstants.DataServiceRoot), request);
                await task;
                return true;
            }
        }

        public ActionResult UserFeedback()
        {
            LogInterventionInteraction("UserFeedback() page loaded");

            return PartialView("_UserFeedback");
        }
        public ActionResult UserFeedbackLayout()
        {
            LogInterventionInteraction("UserFeedbackLayout() page loaded");

            return View("UserFeedback");
        }

        public void BuildCourseSelectViewBag()
        {
            ViewBag.CurrentCourseId = ActiveCourseUser.AbstractCourseID;

            //get user courses
            List<int> currentUserCourseIds = DBHelper.GetAllUserCourseIds(ActiveCourseUser.UserProfileID);
            Dictionary<int, string> currentUserCourseIdCourseName = new Dictionary<int, string>();
            foreach (int id in currentUserCourseIds)
            {
                currentUserCourseIdCourseName.Add(id, DBHelper.GetCourseFullNameFromCourseId(id));
            }

            ViewBag.CurrentUserCoursesIdCourseName = currentUserCourseIdCourseName;
        }

        public void BuildFeedbackItemViewBag(int interventionId = 0, string title = "", string content = "")
        {
            if (interventionId == 0)
            {
                InterventionItem feedbackItem = new InterventionItem();
                feedbackItem.Title = title;
                feedbackItem.ContentFirst = true;
                feedbackItem.ListItemContent = content;
                feedbackItem.LinkText = "";
                ViewBag.InterventionFeedbackItem = feedbackItem;
                ViewBag.MainPage = true;
            }
            else
            {
                ViewBag.InterventionFeedbackItem = GetInterventionDetails(interventionId);
                ViewBag.MainPage = false;
            }
        }
        /// <summary>
        /// Returns an InterventionList for the InterventionList ViewModel. It should contain enough information to populate the intervention list.
        /// Should only return interventions for the active user.
        /// Catch-all for now, don't necessarily need all details for current usage.
        /// </summary>
        /// <param name="userProfileId"></param>
        /// <returns></returns>
        private InterventionsList BuildInterventionsViewModel(int userProfileId, bool getDismissedInterventions = false)
        {
            InterventionsList vm = new InterventionsList();
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "";

                    if (getDismissedInterventions)
                    {
                        query = "SELECT * FROM OSBLEInterventions WHERE UserProfileId = @UserProfileId AND (IsDismissed = 1) ORDER BY InterventionDateTime DESC";
                    }
                    else
                    {
                        query = "SELECT * FROM OSBLEInterventions WHERE UserProfileId = @UserProfileId AND (IsDismissed = 0 OR IsDismissed IS NULL)  ORDER BY InterventionDateTime DESC";
                    }

                    var results = sqlConnection.Query(query, new { UserProfileId = userProfileId });

                    //dismiss interventions if still 'active' after date threshold
                    DateTime dateThreshold = DateTime.UtcNow;
                    dateThreshold = dateThreshold.AddDays(InterventionExpirationInDays);

                    foreach (var interventionResult in results)
                    {
                        bool dismissedIntervention = false;
                        //if we're not getting dismissed interventions and the intervention is "ClassmatesAvailable" check to see if they are still available... if not, dismiss it.
                        if (!getDismissedInterventions && interventionResult.InterventionType == "ClassmatesAvailable")
                        {
                            //check if users are still available for the current course
                            bool usersStillAvailable = CheckIfOtherUsersAreAvailable(userProfileId);
                            //if not, dismiss
                            if (!usersStillAvailable)
                            {
                                dismissedIntervention = DismissIntervention(interventionResult.Id, true);
                            }
                        }

                        if (dismissedIntervention)
                        {
                            //don't put the intervention on the list
                        }
                        else //add the intervention as usual
                        {
                            int comparison = DateTime.Compare(interventionResult.InterventionDateTime, dateThreshold);
                            if (comparison < 0 && !getDismissedInterventions) //dismiss one week old
                            {
                                DismissExpiredIntervention(interventionResult.Id);
                            }
                            else
                            {
                                //Parse results
                                InterventionItem intervention = new InterventionItem();
                                intervention.Id = interventionResult.Id;
                                intervention.UserProfileId = interventionResult.UserProfileId;
                                intervention.InterventionTrigger = interventionResult.InterventionTrigger;
                                intervention.InterventionMarkedHelpful = interventionResult.InterventionMarkedHelpful ?? false;
                                intervention.InterventionDateTime = DateTimeExtensions.UTCToCourse(DateTime.SpecifyKind(interventionResult.InterventionDateTime, DateTimeKind.Unspecified), ActiveCourseUser.AbstractCourseID);
                                intervention.InterventionType = InterventionTypesExtensions.Explode(InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionResult.InterventionType));
                                intervention.Icon1 = interventionResult.Icon1;
                                intervention.Icon2 = interventionResult.Icon2;
                                intervention.Title = interventionResult.Title;
                                intervention.Link = string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, interventionResult.InterventionType, interventionResult.Id);
                                intervention.LinkText = interventionResult.LinkText;
                                //TODO: Populate the template text from the database, for now just use the canned response from the model.
                                //intervention.ContentFirst = result.ContentFirst;
                                intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionResult.InterventionType));
                                intervention.ListItemContent = interventionResult.ListItemContent;
                                //TODO: Populate the template text from the database, for now just use the canned response from the model.
                                //intervention.InterventionTemplateText = interventionResult.InterventionTemplateText;
                                intervention.InterventionTemplateText = InterventionTypesExtensions.TemplateText(InterventionTypesExtensions.ParseEnum<InterventionTypes>(interventionResult.InterventionType));
                                intervention.InterventionSuggestedCode = interventionResult.InterventionSuggestedCode;
                                intervention.IsDismissed = interventionResult.IsDismissed ?? false;

                                //add to the viewmodel
                                //only add the intervention if it belongs to the current user
                                if (intervention.UserProfileId == ActiveCourseUser.UserProfileID)
                                    vm.InterventionItemList.Add(intervention);
                            }
                        }
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }

            //cover rare case where we may have somehow allowed 2 of the same intervention to be live.            
            var dupes = vm.InterventionItemList.GroupBy(x => new { x.InterventionType })
                                               .Where(x => x.Skip(1).Any());

            foreach (var duplicateList in dupes)
            {
                bool first = true;
                foreach (var duplicate in duplicateList)
                {
                    if (first) //keep the first one
                    {
                        first = false;
                    }
                    else //dismiss any duplicates and remove from the display list
                    {
                        DismissIntervention(duplicate.Id, true);
                        vm.InterventionItemList.Remove(duplicate);
                    }
                }
            }

            return vm;
        }

        private bool CheckIfOtherUsersAreAvailable(int userProfileId, int courseId = 0)
        {
            if (courseId == 0 && ActiveCourseUser == null)
            {
                courseId = GuessActiveCourseId(userProfileId);
            }

            if (courseId == 0)
            {
                try
                {
                    courseId = ActiveCourseUser.AbstractCourseID;
                }
                catch (Exception e)
                {
                    throw new Exception("CheckIfOtherUsersAreAvailable() failed to get AbstractCourseID from ActiveCourseUser...", e);
                    courseId = GuessActiveCourseId(userProfileId);
                }
            }

            if (courseId > 0) //we have a courseId, known or guessed.
            {
                try
                {
                    using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        sqlConnection.Open();
                        //look for the current user's availability for the current course (excluding themselves).
                        string query = "SELECT COUNT(*) as 'Count' FROM UserStatus WHERE CourseId = @CourseId AND UserProfileId != @UserProfileId AND IsAvailableToHelp = 1 AND AvailableEndTime > GETDATE() ";

                        int result = sqlConnection.Query<int>(query, new { UserProfileId = userProfileId, CourseId = courseId }).SingleOrDefault();

                        if (result > 0)
                        {
                            return true; //at least 1 person, besides the current user, is available!
                        }

                        sqlConnection.Close();
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
            else
            {
                return false; //we failed to check...
            }
            return false; //if we got here something is wrong!
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

        private void DismissExpiredIntervention(int interventionId)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "UPDATE OSBLEInterventions SET IsDismissed = 1 WHERE Id = @InterventionId";

                    sqlConnection.Execute(query, new { InterventionId = interventionId });

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }
        }

        private List<string> FormatAssignmentNamesForHashtags(List<string> assignmentNames)
        {
            List<string> formattedAssignmentNames = new List<string>();
            foreach (string assignmentName in assignmentNames)
            {
                formattedAssignmentNames.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(assignmentName).Replace(" ", ""));
            }
            return formattedAssignmentNames;
        }

        private List<UserStatus> GetAllUsersStatus(int courseId)
        {
            List<UserStatus> usersStatus = new List<UserStatus>();
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM UserStatus WHERE CourseId = @CourseId AND IsAvailableToHelp = 1";

                    var results = sqlConnection.Query(query, new { CourseId = courseId });

                    foreach (var status in results)
                    {
                        usersStatus.Add(
                            new UserStatus()
                            {
                                UserProfileId = status.UserProfileId,
                                StatusMessage = status.UserStatus,
                                AvailableStartTime = status.AvailableStartTime,
                                AvailableEndTime = status.AvailableEndTime
                            });
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }
            return usersStatus;
        }

        private Dictionary<int, string> GetAvailableUsers(int courseId)
        {
            Dictionary<int, string> availableUsers = new Dictionary<int, string>();

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT us.*, up.FirstName + ' ' + up.LastName as UserFullName " +
                                    "FROM UserStatus us " +
                                    "INNER JOIN UserProfiles up " +
                                    "ON us.UserProfileId = up.ID " +
                                    "WHERE us.CourseId = @CourseId AND us.IsAvailableToHelp = 1  AND AvailableEndTime > GETDATE()";

                    var results = sqlConnection.Query(query, new { CourseId = courseId });

                    foreach (var user in results)
                    {
                        availableUsers.Add(user.UserProfileId, user.UserFullName);
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }

            return availableUsers;
        }

        private string GetFeedbackDetails(int interventionId)
        {
            switch (interventionId)
            {
                case -2:
                    return "availability-details";
                    break;
                case -3:
                    return "dismissed-suggestions";
                    break;
                case -4:
                    return "private-messages";
                    break;
                case -5:
                    return "unanswered-questions-dashboard";
                    break;
                case -6:
                    return "suggestions-settings";
                    break;
                default:
                    return "user-feedback";
                    break;
            }
        }

        /// <summary>
        /// Returns an InterventionItem. Populates all values from the database.
        /// Catch-all for now, don't necessarily need all details for current usage.
        /// </summary>
        /// <param name="interventionId"></param>
        /// <returns></returns>
        private InterventionItem GetInterventionDetails(int interventionId)
        {
            InterventionItem intervention = new InterventionItem();

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventions WHERE Id = @InterventionId";
                    var result = sqlConnection.Query(query, new { InterventionId = interventionId }).Single();

                    //Parse results
                    intervention.Id = result.Id;
                    intervention.UserProfileId = result.UserProfileId;
                    intervention.InterventionTrigger = result.InterventionTrigger;
                    intervention.InterventionMarkedHelpful = result.InterventionMarkedHelpful ?? false;
                    intervention.InterventionDateTime = result.InterventionDateTime;
                    intervention.InterventionType = InterventionTypesExtensions.Explode(InterventionTypesExtensions.ParseEnum<InterventionTypes>(result.InterventionType));
                    intervention.Icon1 = result.Icon1;
                    intervention.Icon2 = result.Icon2;
                    intervention.Title = result.Title;
                    intervention.Link = string.Format("{0}/Intervention/{1}?interventionId={2}&component=7", StringConstants.WebClientRoot, result.InterventionType, result.Id);
                    intervention.LinkText = result.LinkText;
                    //TODO: Populate the template text from the database, for now just use the canned response from the model.
                    //intervention.ContentFirst = result.ContentFirst;
                    intervention.ContentFirst = InterventionTypesExtensions.ContentFirst(InterventionTypesExtensions.ParseEnum<InterventionTypes>(result.InterventionType));
                    intervention.ListItemContent = result.ListItemContent;
                    intervention.InterventionTemplateText = result.InterventionTemplateText;
                    intervention.InterventionSuggestedCode = result.InterventionSuggestedCode;
                    intervention.IsDismissed = result.IsDismissed ?? false;

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }

            //only serve an intervention to a user if it's their own
            if (intervention.UserProfileId != ActiveCourseUser.UserProfileID)
            {
                return new InterventionItem();
            }
            else
            {
                return intervention;
            }
        }

        private List<string> GetPopularHashtags(int courseId, int numberOfDays)
        {
            List<string> postsAndReplies = new List<string>();
            List<string> hashtags = new List<string>();
            List<string> popularHashtags = new List<string>();

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    //Search feed posts
                    string query = "SELECT Comment FROM FeedPostEvents " +
                                    "WHERE EventLogId IN ( SELECT Id FROM EventLogs WHERE CourseId = @CourseId )" + //only want hashtags being used in the selected course
                                    "AND CHARINDEX('#', Comment) > 0 " +
                                    "AND EventDate >= DATEADD(day, -@NumberOfDays, GETDATE())" + //specify how far back we want to look for 'popular' tags
                        //also search comments
                                    "SELECT Content FROM LogCommentEvents " +
                                    "WHERE EventLogId IN ( SELECT Id FROM EventLogs WHERE CourseId = @CourseId ) " + //only want hashtags being used in the selected course
                                    "AND CHARINDEX('#', Content) > 0 " +
                                    "AND EventDate >= DATEADD(day, -@NumberOfDays, GETDATE())" + //specify how far back we want to look for 'popular' tags
                        //and finally get all hashtags
                                    "SELECT CAST(Content as varchar(500)) FROM HashTags"; // get hashtags list

                    using (var queries = sqlConnection.QueryMultiple(query, new { CourseId = courseId, NumberOfDays = numberOfDays }))
                    {
                        postsAndReplies.AddRange(queries.Read<string>().ToList()); //read post result
                        postsAndReplies.AddRange(queries.Read<string>().ToList()); //read replies result
                        hashtags.AddRange(queries.Read<string>().ToList()); //read hashtags result
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }

            //parse posts for tags
            foreach (string postOrReply in postsAndReplies)
            {
                List<string> parts = postOrReply.Split('#').ToList(); //split post by hashtag symbol
                foreach (string part in parts)
                {
                    if (hashtags.Contains(part.Trim(), StringComparer.CurrentCultureIgnoreCase)) //if the part is on the hashtag list, add it
                        if (!popularHashtags.Contains(part.Trim(), StringComparer.CurrentCultureIgnoreCase)) //only add if the hashtag has not already been added
                            popularHashtags.Add(part.Trim());
                }
            }

            //now add assignment names to the list!
            popularHashtags.AddRange(FormatAssignmentNamesForHashtags(GetRecentAssignmentNames(ActiveCourseUser.AbstractCourseID, 14)));

            //now add random suggested tags
            popularHashtags.Add("StudyGroup");
            popularHashtags.Add("GoCougs!");
            popularHashtags.Add("ExamReview");

            return popularHashtags;
        }

        public string GetPrivateFeedPosts(int userProfileId, int courseId = 0)
        {
            string privatePostIds = "";

            if (courseId == 0)
            {
                courseId = ActiveCourseUser.AbstractCourseID;
            }

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT Id, EventVisibleTo " +
                                   "FROM EventLogs " +
                                   "WHERE CourseId = @CourseId " +
                                   "AND (EventVisibleTo LIKE @UserProfileId + ',%' " +
                                   "OR EventVisibleTo LIKE '%,' + @UserProfileId + ',%' " +
                                   "OR EventVisibleTo LIKE '%,' + @UserProfileId) ";

                    var results = sqlConnection.Query(query, new { UserProfileId = userProfileId.ToString(), CourseId = courseId });

                    foreach (var post in results)
                    {
                        //query should return results including just the user profileId, but just to be sure...
                        var ids = new List<string>(post.EventVisibleTo.Split(','));
                        if (ids.Contains(userProfileId.ToString()))
                        {
                            privatePostIds += post.Id + ",";
                        }
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }
            privatePostIds = privatePostIds.Length > 1 ? privatePostIds.Remove(privatePostIds.Length - 1) : privatePostIds;//remove the last comma
            return privatePostIds;
        }

        private List<string> GetRecentAssignmentNames(int courseId, int numberOfDays)
        {
            List<string> assignmentNames = new List<string>();
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT AssignmentName FROM Assignments WHERE CourseID = @CourseId AND DueDate >= DATEADD(day, -@NumberOfDays, GETDATE()) AND IsDraft = 0 ";

                    var results = sqlConnection.Query<string>(query, new { CourseId = courseId, NumberOfDays = numberOfDays }).ToList();

                    assignmentNames.AddRange(results);

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }
            return assignmentNames;
        }

        private string GetUnansweredFeedPosts(int courseId, int numberOfDays)
        {
            string unansweredPostIds = "";

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
                    //6) Posts is not soft deleted (in EventLogs, IsDeleted = 1 if deleted, null otherwise)
                    string query = "(SELECT Id FROM EventLogs WHERE (CourseId = @CourseId OR CourseId IS NULL) AND (IsDeleted IS NULL OR IsDeleted != 1) " +
                                   "AND EventTypeId IN (7, 1) AND EventDate >= DATEADD(day, -@NumberOfDays, GETDATE()) " +
                                   "AND SenderId IN  " +
                                   "(SELECT UserProfileID FROM CourseUsers WHERE AbstractCourseID = @CourseId AND AbstractRoleID NOT IN (1, 2)) " +
                                   "AND Id NOT IN  " +
                                   "(SELECT SourceEventLogId FROM LogCommentEvents WHERE Id IN  " +
                                   "(SELECT DISTINCT LogCommentEventId FROM HelpfulMarkGivenEvents))) " +
                                   "ORDER BY EventDate DESC";
                    var results = sqlConnection.Query<int>(query, new { CourseId = courseId, NumberOfDays = numberOfDays }).ToList();

                    foreach (var post in results)
                    {
                        unansweredPostIds += post.ToString() + ",";
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }
            unansweredPostIds = unansweredPostIds.Length > 1 ? unansweredPostIds.Remove(unansweredPostIds.Length - 1) : unansweredPostIds;//remove the last comma
            return unansweredPostIds;
        }

        private string GetUnansweredFeedPostsOnlyUnanswered(int courseId, int numberOfDays)
        {
            string unansweredPostIds = "";
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    //FeedPostEvents
                    string query = "SELECT fpe.EventLogId " +
                            "FROM FeedPostEvents fpe " +
                            "INNER JOIN EventLogs el ON fpe.EventLogId = el.Id " +
                            "WHERE fpe.EventLogId IN " +
                            "(SELECT Id FROM EventLogs WHERE EventTypeId IN (7, 1) AND (CourseId = @CourseId OR CourseId IS NULL) AND EventDate >= DATEADD(day, -@NumberOfDays, GETDATE()) " + //EventTypeId of 7 == FeedPostEvent
                            "AND Id NOT IN (SELECT SourceEventLogId FROM LogCommentEvents) AND (IsDeleted = 0 OR IsDeleted IS NULL)) " +
                        //AskForHelpEvents
                            "SELECT ahe.EventLogId " +
                            "FROM AskForHelpEvents ahe " +
                            "INNER JOIN EventLogs el ON ahe.EventLogId = el.Id " +
                            "WHERE ahe.EventLogId IN " +
                            "(SELECT Id FROM EventLogs WHERE EventTypeId IN (7, 1) AND (CourseId = @CourseId OR CourseId IS NULL) AND EventDate >= DATEADD(day, -@NumberOfDays, GETDATE()) " + //EventTypeId of 1 == AskForHelpEvent
                            "AND Id NOT IN (SELECT SourceEventLogId FROM LogCommentEvents) AND (IsDeleted = 0 OR IsDeleted IS NULL))";

                    //can potentially get questions from outside of the course currently as the plugin ask for help may save a null courseId                   
                    using (var queries = sqlConnection.QueryMultiple(query, new { CourseId = courseId, NumberOfDays = numberOfDays }))
                    {
                        var feedResults = queries.Read().ToList(); //read feedpost result
                        var askForHelpResults = queries.Read().ToList(); //read askForHelp result                        

                        foreach (var post in feedResults)
                        {
                            unansweredPostIds += post.EventLogId + ",";
                        }

                        foreach (var post in askForHelpResults)
                        {
                            unansweredPostIds += post.EventLogId + ",";
                        }
                    }

                    unansweredPostIds = unansweredPostIds.Length > 1 ? unansweredPostIds.Remove(unansweredPostIds.Length - 1) : unansweredPostIds;//remove the last comma

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                //failure
            }

            return unansweredPostIds;
        }

        private string GetUrlReferrer()
        {
            string urlReferrer = "";
            if (Request != null && Request.UrlReferrer != null)
            {
                urlReferrer = Request.UrlReferrer.ToString();
            }
            return urlReferrer;
        }
        private void LogInterventionInteraction(string interventionDetails = "", int interventionId = -10, int userProfileId = 0, string interventionDetailBefore = "", string interventionDetailAfter = "", string additionalActionDetails = "")
        { //details first so we can pass in just details if desired.
            try
            {
                if (userProfileId == 0)
                {
                    userProfileId = ActiveCourseUser.UserProfileID;
                }
            }
            catch (Exception)
            {
                //do nothing for now.            
            }

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
                throw new Exception("Failed to insert new interaction", e);
            }
        }

        private bool UpdateInterventionFeedback(int interventionId, string feedbackDetails, string feedbackComment = "", string additionalActionDetails = "")
        {
            bool? feedbackVote = null;

            bool insertSuccess = false;
            bool updateSuccess = false;

            if (feedbackDetails.Contains("thumbs-up-feedback") || feedbackDetails.Contains("thumbs-down-feedback"))
            {
                feedbackVote = true;
            }

            if (feedbackComment != "")
            {
                feedbackDetails = GetFeedbackDetails(interventionId);
            }

            int userProfileId = ActiveCourseUser.UserProfileID;

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    //Search feed posts
                    string updateQuery = "UPDATE OSBLEInterventions " +
                                         "SET InterventionMarkedHelpful = @FeedbackVote " +
                                         "WHERE Id = @InterventionId ";

                    string insertQuery = "INSERT INTO OSBLEInterventionInteractions " +
                                         "VALUES (@InterventionId, @UserProfileId, GETDATE(), @FeedbackDetails, @FeedbackComment, '', '', @AdditionalActionDetails) ";
                    if (interventionId > 0) //for feedback not associated with an intervention, we don't want to update
                    {
                        updateSuccess = sqlConnection.Execute(updateQuery, new { InterventionId = interventionId, FeedbackVote = feedbackVote }) != 0;
                    }

                    insertSuccess = sqlConnection.Execute(insertQuery, new { InterventionId = interventionId, UserProfileId = userProfileId, FeedbackDetails = feedbackDetails, FeedbackComment = feedbackComment, FeedbackVote = feedbackVote, AdditionalActionDetails = additionalActionDetails }) != 0;

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                return false;
            }
            if (updateSuccess && insertSuccess || interventionId < 0 && insertSuccess)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}