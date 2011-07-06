using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models;
using OSBLE.Models.ViewModels;

namespace OSBLE.Controllers
{ 
    public class RubricController : OSBLEController
    {
        public ActionResult Index()
        {
            Rubric rubric = db.Rubrics.Find(1);
            RubricViewModel viewModel = new RubricViewModel();
            viewModel.Rubric = rubric;
            return View(viewModel);
        }
    }
}