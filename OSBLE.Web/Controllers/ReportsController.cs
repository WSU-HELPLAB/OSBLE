using OSBLE.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Controllers;
using OSBLE.Models.Report;
using System.Text;
using System.Reflection;

namespace OSBLE.Controllers
{
    [IsAdmin, OsbleAuthorize]
    public class ReportsController : OSBLEController
    {
        /// <summary>
        /// Loads the reports index page. This page shows all possible report generation links
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Loads the Post-Reply counts page, this page shows a simple date range picker (inclusive) and 
        /// will generate the post-reply report in a copy-pastable to excel format (rows/columns) for easy sorting or manipulation.
        /// </summary>
        /// <returns></returns>
        public ActionResult PostReplyCounts()
        {
            return View();
        }

        /// <summary>
        /// Builds and returns a post-reply count table
        /// FirstName	LastName	Posts	Replies
        /// ---------   --------    -----   -------
        /// 
        /// that can be copy-pasted into excel or other spreadsheet programs for easy sortying or manipulation.
        /// Posts/replies are counted between the from and to date range (inclusive)
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult PostReplyReport(string from, string to)
        {
            List<Tuple<string, int, int>> postsAndRepliesForCurrentCourse = DBHelper.GetPostsAndRepliesCount(ActiveCourseUser.AbstractCourseID, Convert.ToDateTime(from), Convert.ToDateTime(to));
            Tuple<DateTime, DateTime, List<Tuple<string, int, int>>> reportOutput = new Tuple<DateTime, DateTime, List<Tuple<string, int, int>>>(Convert.ToDateTime(from), Convert.ToDateTime(to), postsAndRepliesForCurrentCourse);
            ViewBag.ReportOutput = reportOutput;
            return View();
        }

        /// <summary>
        /// Loads the intervention interactions page, this page shows a simple date range picker (inclusive),
        /// has report configuration options, and
        /// will generate the intervention interactions report in a copy-pastable to excel format (rows/columns) for easy sorting or manipulation. 
        /// </summary>
        /// <returns></returns>
        public ActionResult InterventionInteraction()
        {
            return View();
        }

        /// <summary>
        /// Builds and returns a InterventionInteraction table        
        /// that can be copy-pasted into excel or other spreadsheet programs for easy sortying or manipulation.
        /// Intervention interactions are between the chosen from and to date range (inclusive)
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [HttpPost]
        [MultipleButton(Name = "action", Argument = "InterventionInteractionReport")]
        public ActionResult InterventionInteractionReport(string from, string to)
        {
            int courseId = ActiveCourseUser.AbstractCourseID;

            //get options from the filter checkboxes
            Dictionary<string, bool> options = new Dictionary<string, bool>();
            var filterTypes = OSBLEInteractionReportFiltersExtensions.GetValues<OSBLEInteractionReportFilters>();
            foreach (var filter in filterTypes)
            {
                string filterName = OSBLEInteractionReportFiltersExtensions.Explode(filter);
                string filterOption = Request.Form[filterName];
                if (!String.IsNullOrEmpty(filterOption))
                {
                    options.Add(filterName, true);
                }
            }

            List<OSBLEInterventionReportItem> interventionReport = DBHelper.GetInterventionInteractionReport(courseId, Convert.ToDateTime(from), Convert.ToDateTime(to), options);
            ViewBag.ReportItems = interventionReport;

            return View();
        }

        /// <summary>
        /// Builds and returns a InterventionInteraction table CSV               
        /// Intervention interactions are between the chosen from and to date range (inclusive)
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [HttpPost]
        [MultipleButton(Name = "action", Argument = "InterventionInteractionReportCSV")]
        public ActionResult InterventionInteractionReportCSV(string from, string to)
        {
            int courseId = ActiveCourseUser.AbstractCourseID;

            //get options from the filter checkboxes
            Dictionary<string, bool> options = new Dictionary<string, bool>();
            var filterTypes = OSBLEInteractionReportFiltersExtensions.GetValues<OSBLEInteractionReportFilters>();
            foreach (var filter in filterTypes)
            {
                string filterName = OSBLEInteractionReportFiltersExtensions.Explode(filter);
                string filterOption = Request.Form[filterName];
                if (!String.IsNullOrEmpty(filterOption))
                {
                    options.Add(filterName, true);
                }
            }

            List<OSBLEInterventionReportItem> interventionReport = DBHelper.GetInterventionInteractionReport(courseId, Convert.ToDateTime(from), Convert.ToDateTime(to), options);

            //make a csv for export
            var csv = new StringBuilder();
            csv.Append(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}{17}", "FirstName", "LastName", "AbstractCourseID", "OSBLEInterventionId",
                                     "InteractionDateTime", "InteractionDetails", "InterventionFeedback", "InterventionDetailBefore", "InterventionDetailAfter", "AdditionalActionDetails",
                                     "InterventionTrigger", "InterventionType", "InterventionTemplateText", "InterventionSuggestedCode", "IsDismissed", "RefreshThreshold",
                                     "ShowInIDESuggestions", Environment.NewLine));
            foreach (var row in interventionReport)
            {
                var newLine = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}{17}",
                    row.FirstName, row.LastName, row.CourseId, row.OSBLEInterventionId, row.InteractionDateTime, row.InteractionDetails, row.InterventionFeedback, row.InterventionDetailBefore,
                    row.InterventionDetailAfter, row.AdditionalActionDetails, row.InterventionTrigger, row.InterventionType, row.InterventionTemplateText, row.InterventionSuggestedCode,
                    row.IsDismissed, row.RefreshThreshold, row.ShowInIDESuggestions, Environment.NewLine);
                csv.Append(newLine);
            }

            const string contentType = "text/plain";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(bytes, contentType, "Intervention Interaction " + DateTime.Now + " (Exported Report).csv");
        }
    }

    /// <summary>
    /// Allows one form with multiple submit buttons
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class MultipleButtonAttribute : ActionNameSelectorAttribute
    {
        public string Name { get; set; }
        public string Argument { get; set; }

        public override bool IsValidName(ControllerContext controllerContext, string actionName, MethodInfo methodInfo)
        {
            var isValidName = false;
            var keyValue = string.Format("{0}:{1}", Name, Argument);
            var value = controllerContext.Controller.ValueProvider.GetValue(keyValue);

            if (value != null)
            {
                controllerContext.Controller.ControllerContext.RouteData.Values[Name] = Argument;
                isValidName = true;
            }

            return isValidName;
        }
    }
}
