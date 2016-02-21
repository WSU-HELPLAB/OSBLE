using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.UI;
using DDay.iCal.Serialization.iCalendar;
using OSBLE.Models.OSBLECommunity;
using OSBLE.Models.ViewModels;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using OSBLEPlus.Logic.Utility;
using System.Collections;
using Dapper;
using OSBLE.Utility;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    public class OSBLECommunityController : OSBLEController
    {
        /// <summary>
        /// Index, returns a view with the OSBLE Community Page
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            try
            {
                if (ActiveCourseUser == null || ActiveCourseUser.UserProfileID < 1 || ActiveCourseUser.AbstractCourseID < 1)
                {
                    //The page is being accessed without being logged in.
                    return RedirectToAction("LogOn", "Account", new { returnUrl = "~/OSBLECommunity/" });        
                }
                else
                {
                    return PartialView(setupViewModel());
                }                
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;                
                return View("OSBLECommunityGenericError");
            }
        }

        /// <summary>
        /// setupViewModel initializes the OSBLECommunity view with current user and course id
        /// </summary>
        /// <returns>viewmodel for an osble community</returns>
        private OSBLECommunityViewModel setupViewModel()
        {
            OSBLECommunityViewModel vm = new OSBLECommunityViewModel();
            vm.UserProfileId = ActiveCourseUser.UserProfileID;
            vm.AbstractCourseId = ActiveCourseUser.AbstractCourseID;

            return vm;
        }

        /// <summary>
        /// GetGridPartialView gets and returns the selected partial view as string.
        /// </summary>
        /// <param name="gridType"> string descriptor for which partial view to return </param>
        /// <returns>returns the partial view as a string with an html id prepended and delimited by '__' </returns>
        public string GetGridPartialView(string gridType = "None")
        {
            //TODO: set up each view with custom data before returning
            //Note: using "__" to prepend the 'widget' type that is being returned.
            switch (gridType)
            {                                               
                case "Online":                    
                    return gridType + "__" + ControllerContext.RenderPartialToString("_Online", LastLogActivity(StringConstants.OnlineActivtyMetricInMinutes));
                case "RecentActivity":
                    return gridType + "__" + ControllerContext.RenderPartialToString("_RecentActivity", RecentActivty());
                case "Goals":
                    return gridType + "__" + ControllerContext.RenderPartialToString("_Goals", null);
                case "CommunityStanding":
                    return gridType + "__" + ControllerContext.RenderPartialToString("_CommunityStanding", null);
                case "PersonalStanding":
                    return gridType + "__" + ControllerContext.RenderPartialToString("_PersonalStanding", null);
                default:
                    return gridType + "__" + ControllerContext.RenderPartialToString("Error", null); 
            }
            
        }               
       
        /// <summary>
        /// SaveGrid: saves a single grid to the db
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="userProfileId"></param>
        /// <param name="gridType"></param>
        /// <param name="gridOptions"></param>
        /// <param name="gridLayout"></param>
        /// <returns></returns>
        public int SaveGrid(int courseId, int userProfileId, string gridType, string gridOptions, string gridLayout)
        {
            //TODO: update this to work with new 'widget' setup.
            OSBLECommunityViewModel vm = new OSBLECommunityViewModel();

            OSBLECommunityGrid gridItem = new OSBLECommunityGrid();
            gridItem.GridLayout = "a grid layout";
            gridItem.GridOptions = "grid options";
            gridItem.GridType = "test type";

            vm.Grids.Add(gridItem);

            vm.AbstractCourseId = 1;
            vm.UserProfileId = 1;

            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    //insert into OSBLECommunityGrids table
                    string query = "INSERT INTO OSBLECommunityGrids values (@type, @options, @layout); SELECT CAST(SCOPE_IDENTITY() as int)";
                    int gridId = sqlConnection.Query<int>(query, new { type = gridType, options = gridOptions, layout = gridLayout }).Single();

                    //now insert into linking table OSBLECommunityGridsUsers
                    query = "INSERT INTO OSBLECommunityGridsUsers values (@UserProfileId, @AbstractCourseId, @OSBLECommunityGridId)";
                    sqlConnection.Query<int>(query, new { UserProfileId = userProfileId, AbstractCourseId = courseId, GridId = gridId }).Single();

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                return 0; //failure
            }
            return 1; //success
        }

        public string Testing() //todo: modify to accept parameter for getting different views.
        {            
            string render = ControllerContext.RenderPartialToString("_Online", null);

            string temp = "#online__" + render;            
            return temp;            
        }

        /// <summary>
        /// LoadUserGrids
        /// </summary>
        /// <param name="userProfileId">Current User Profile ID</param>
        /// <param name="courseId">Current User's active course ID</param>
        /// <returns>Packaged string containing grid types, options, and layouts</returns>
        public string LoadUserGrids(int userProfileId, int courseId)
        {
            //TODO: update this for the new grid setup
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();                    

                    string query =  @"SELECT GridType, GridOptions, GridLayout " +
                                    "FROM OSBLECommunityGrids SHG " +
                                    "INNER JOIN OSBLECommunityGridsUsers SHGU " +
                                    "ON SHG.Id = SHGU.OSBLECommunityGridId " +
                                    "WHERE AbstractCourseId = @id " +
                                    "AND UserProfileId = @user ";
         
                    var results = sqlConnection.Query(query, new { id = courseId, user = userProfileId });

                    string gridList = "";

                    foreach (var grid in results)
                    {
                        //DEBUG: only did this to test results, will change next by adding to gridList instead of the below
                        //Package grid results
                        string a = grid.GridType;
                        string b = grid.GridOptions;
                        string c = grid.GridLayout;
                    }
                    sqlConnection.Close();
                    //TODO: V
                    return "TODO: parse above results and return here";
                }
            }
            catch (Exception e)
            {                
                return e.Message;
            }            
        }

        /// <summary>
        /// Gets a list of user names with log activity in the last [input] minutes
        /// </summary>
        /// <param name="minutes">number of minutes</param>
        /// <returns>list of user names</returns>
        public OSBLECommunityOnlineViewModel LastLogActivity(int minutes = 30)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    const string query = @"SELECT DISTINCT " +
                                         "UserProfiles.FirstName AS FirstName, " +
                                         "UserProfiles.LastName AS LastName, " +
                                         "UserProfiles.ID AS UserProfileID " +
                                         "FROM ((dbo.UserProfiles AS UserProfiles " +
                                         "INNER JOIN dbo.EventLogs AS EventLogs ON (UserProfiles.ID  = EventLogs.SenderId )) " +
                                         "INNER JOIN dbo.EventTypes AS EventTypes ON (EventTypes.EventTypeId  = EventLogs.EventTypeId )) " +
                                         "WHERE Eventlogs.EventDate > DATEADD(mi, -@minutes, GETDATE())" +

                                         //get last activity date
                                         "SELECT TOP 1 " +
                                         "EventLogs.EventDate " +
                                         "FROM EventLogs " +
                                         "WHERE Eventlogs.EventDate > DATEADD(mi, -@minutes, GETDATE()) " +
                                         "ORDER BY EventDate DESC";

                    //var results = sqlConnection.Query(query, new { minutes = minutes });
                    
                    var vm = new OSBLECommunityOnlineViewModel();

                    using (var queries = sqlConnection.QueryMultiple(query, new { minutes = minutes }))
                    {
                        var resultsActiveUsers = queries.Read().ToList();
                        var resultLastActivity = queries.Read().Single();

                        foreach (var user in resultsActiveUsers)
                        {
                            vm.OnlineUser.Add(user.FirstName + " " + user.LastName, user.UserProfileID);
                        }

                        //TODO: format date correctly... it seems that the date value is not correct somehow, but TimeOfDay is?
                        vm.LastActivty = DateTime.Parse(resultLastActivity.EventDate.ToString());
                    }

                    sqlConnection.Close();

                    return vm;
                }
            }
            catch (Exception e)
            {                
                //todo handle error
                return new OSBLECommunityOnlineViewModel();
            }            
        }

        /// <summary>
        /// returns a list of viewmodel objects with the last @numberOfEvents event details
        /// </summary>
        /// <param name="numberOfEvents">number of results to return</param>
        /// <returns>a list of OSBLECommunityRecentActivityViewModels</returns>
        public List<OSBLECommunityRecentActivtyViewModel> RecentActivty(int numberOfEvents = 20)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                                    //Recent activity for top level posts ItemType: 1, 6, 7, 11
                    const string query =    @"SELECT TOP (@numberOfEvents) " +
		                                    "EventLogs.Id as EventLogId " +
		                                    ",EventTypes.EventTypeId " +
		                                    ",EventTypes.EventTypeName " +
		                                    ",[EventDate] " +
		                                    ",[SenderId]	as UserId " +
		                                    ",UserProfiles.FirstName " +
                                            "FROM [OSBLEPlus].[dbo].[EventLogs] " +
                                            "INNER JOIN EventTypes " +
                                            "ON EventLogs.EventTypeId = EventTypes.EventTypeId " +
                                            "INNER JOIN UserProfiles " +
                                            "ON EventLogs.SenderId = UserProfiles.ID " +
                                            "WHERE EventLogs.EventTypeId IN (1, 6, 7, 11) " +
                                            "ORDER BY EventDate DESC " +
                                    
                                            //Recent activty for reply level posts ItemType: 9
                                            "SELECT TOP (@numberOfEvents) " +
                                            //"EventLogs.Id as EventLogId   " +
                                            "LogCommentEvents.SourceEventLogId AS EventLogId " + //we need to point to the top level post
                                            ",EventTypes.EventTypeId   " +
                                            ",EventTypes.EventTypeName   " +
                                            ",Eventlogs.EventDate " +
                                            ",[SenderId]	as UserId   " +
                                            ",UserProfiles.FirstName " +                                    
                                            "FROM [OSBLEPlus].[dbo].[EventLogs]   " +
                                            "INNER JOIN EventTypes   " +
                                            "ON EventLogs.EventTypeId = EventTypes.EventTypeId   " +
                                            "INNER JOIN UserProfiles   " +
                                            "ON EventLogs.SenderId = UserProfiles.ID " +
                                            "INNER JOIN LogCommentEvents " +
                                            "ON EventLogs.Id = LogCommentEvents.EventLogId " +
                                            "WHERE EventLogs.EventTypeId IN (9)  " +
                                            "ORDER BY EventDate DESC " +

                                            //Recent activty for markedhelpful posts ItemType: 8
                                            "SELECT TOP (@numberOfEvents ) " +
                                            "LogCommentEvents.SourceEventLogId AS EventLogId " + //we need to point to the top level post
                                            ",Eventlogs.EventTypeId " +
                                            ",EventTypes.EventTypeName   " +
                                            ",HelpfulMarkGivenEvents.EventDate " +
                                            ",UserProfiles.ID As UserId " +
                                            ",UserProfiles.FirstName " +
                                            "FROM (((dbo.HelpfulMarkGivenEvents " +
                                            "INNER JOIN dbo.LogCommentEvents " +
	                                        "ON (LogCommentEvents.Id  = HelpfulMarkGivenEvents.LogCommentEventId )) " +
                                            "INNER JOIN dbo.EventLogs " +
	                                        "ON (EventLogs.Id  = HelpfulMarkGivenEvents.EventLogId )) " +
                                            "INNER JOIN dbo.UserProfiles " +
	                                        "ON (UserProfiles.ID  = EventLogs.SenderId )) " +
                                            "INNER JOIN EventTypes " +
                                            "ON EventLogs.EventTypeId = EventTypes.EventTypeId " +
                                            "ORDER BY HelpfulMarkGivenEvents.EventDate DESC";

                    List<OSBLECommunityRecentActivtyViewModel> vms = new List<OSBLECommunityRecentActivtyViewModel>();

                    using (var queries = sqlConnection.QueryMultiple(query, new { numberOfEvents = numberOfEvents }))
                    {
                        var resultsTopLevelPosts = queries.Read().ToList();
                        var resultsComments = queries.Read().ToList();
                        var resultsMarkHelpful = queries.Read().ToList();

                        //top level posts
                        vms.AddRange(resultsTopLevelPosts.Select(userEvent => BuildVM(userEvent)).AsParallel().Cast<OSBLECommunityRecentActivtyViewModel>());

                        //replies
                        vms.AddRange(resultsComments.Select(userEvent => BuildVM(userEvent)).AsParallel().Cast<OSBLECommunityRecentActivtyViewModel>());

                        //helpful marks
                        vms.AddRange(resultsMarkHelpful.Select(userEvent => BuildVM(userEvent)).AsParallel().Cast<OSBLECommunityRecentActivtyViewModel>());
                    }
                    
                    sqlConnection.Close();

                    //order and only take the top numberOfEvents
                    return vms.OrderByDescending(v => v.EventDate).Take(numberOfEvents).ToList();
                }
            }
            catch (Exception e)
            {                
                //todo handle error
                return new List<OSBLECommunityRecentActivtyViewModel>();
            }
        }        
        private OSBLECommunityRecentActivtyViewModel BuildVM(dynamic queryResult)
        {
            OSBLECommunityRecentActivtyViewModel vm = new OSBLECommunityRecentActivtyViewModel();
            vm.User.Add(queryResult.FirstName, queryResult.UserId);
            vm.Event.Add(queryResult.EventTypeName, queryResult.EventLogId);
            vm.EventDate = queryResult.EventDate;
            return vm;
        }
    }   
}
