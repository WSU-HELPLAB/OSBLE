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
                    return gridType + "__" + ControllerContext.RenderPartialToString("_Online", null);
                case "RecentActivity":
                    return gridType + "__" + ControllerContext.RenderPartialToString("_RecentActivity", null);
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
    }   
}
