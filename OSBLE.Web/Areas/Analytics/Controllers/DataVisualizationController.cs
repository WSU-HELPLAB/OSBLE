using System;
using System.Web.Mvc;

using OSBIDE.Data.DomainObjects;
using OSBIDE.Data.SQLDatabase;
using OSBIDE.Web.Models.Analytics;
using OSBIDE.Web.Models.Attributes;
using OSBIDE.Library.Models;

namespace OSBIDE.Web.Controllers
{
    [AllowAccess(SystemRole.Instructor, SystemRole.Admin)]
    public class DataVisualizationController : ControllerBase
    {
        //
        // GET: /DataVisualization/
        public ActionResult Index()
        {
            var analytics = Analytics.FromSession();
            if (analytics.VisualizationParams == null)
            {
                analytics.VisualizationParams = new VisualizationParams();
            }

            // always start from criteria data entry
            analytics.VisualizationParams.TimeFrom = analytics.Criteria.DateFrom;
            analytics.VisualizationParams.TimeTo = analytics.Criteria.DateTo;

            return View("~/Views/Analytics/DataVisualization.cshtml", analytics.VisualizationParams);
        }

        public ActionResult GetData(int? timeScale, DateTime? timeFrom, DateTime? timeTo, int? timeout, bool grayscale, bool? realtime)
        {
            var analytics = UpdateSession(timeScale, timeFrom, timeTo, timeout, grayscale);

            var chartData = TimelineChartDataProc.Get(timeFrom, timeTo, analytics.SelectedDataItems, analytics.VisualizationParams.TimeScale, timeout, grayscale, realtime);
            var jsonResult = Json(chartData, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult GetCSVData(int? scaleSetting, DateTime? timeFrom, DateTime? timeTo, int? timeout, bool? realtime)
        {
            var analytics = UpdateSession(scaleSetting, timeFrom, timeTo, timeout, true);

            var chartCsvData = TimelineChartDataProc.GetCSV(timeFrom, timeTo, analytics.SelectedDataItems, analytics.VisualizationParams.TimeScale, timeout, analytics.VisualizationParams.GrayScale, realtime);
            return File(new System.Text.UTF8Encoding().GetBytes(chartCsvData), "text/csv", "timeline.csv");
        }

        private static Analytics UpdateSession(int? timeScale, DateTime? timeFrom, DateTime? timeTo, int? timeout, bool grayscale)
        {
            var analytics = Analytics.FromSession();

            analytics.VisualizationParams.TimeFrom = timeFrom;
            analytics.VisualizationParams.TimeTo = timeTo;
            analytics.VisualizationParams.TimeScale = timeScale.HasValue ? (TimeScale)timeScale.Value : TimeScale.Days;
            analytics.VisualizationParams.Timeout = timeout;
            analytics.VisualizationParams.GrayScale = grayscale;

            return analytics;
        }

        public ActionResult ProcessAzureTableStorage()
        {
            return Json(PassiveSocialEventUtilProc.Run(CurrentUser.SchoolId), JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateActiveSocialEvents()
        {
            return Json(ActiveSocialEventUtilProc.Run(), JsonRequestBehavior.AllowGet);
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
