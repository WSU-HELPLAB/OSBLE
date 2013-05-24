using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Controllers
{
    public class CookieTestController : Controller
    {
        //
        // GET: /CookieTest/

        public ActionResult Index()
        {
            return View(new CookieModel());
        }

        [HttpPost]
        public ActionResult Index(CookieModel model)
        {
            HttpCookie cookie = new HttpCookie("CookieTest");
            cookie.Values["Name"] = model.Name;
            Session["CookieTestName"] = model.Name;
            cookie.Expires = DateTime.UtcNow.AddDays(300);
            Response.Cookies.Set(cookie);
            return View(model);
        }

        public ActionResult CheckCookie()
        {
            HttpCookie cookie = Request.Cookies.Get("CookieTest");
            CookieModel model = new CookieModel();
            model.Name = cookie.Values["Name"];
            try
            {
                model.SessionValue = Session["CookieTestName"].ToString();
            }
            catch (Exception ex)
            {
                model.SessionValue = ex.ToString();
            }
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public class CookieModel
        {
            public string Name { get; set; }
            public string SessionValue { get; set; }
            public string Time { get; set; }
            public CookieModel()
            {
                Name = "";
                SessionValue = "";
                Time = DateTime.UtcNow.ToLongTimeString();
            }
        }
    }
}
