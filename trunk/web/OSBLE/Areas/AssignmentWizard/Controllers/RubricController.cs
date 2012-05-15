﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class RubricController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "Rubric";  }
        }

        public override string ControllerDescription
        {
            get { return "The instructor will use a grading rubric"; }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                return new TeamController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                List<AssignmentTypes> types = base.AllAssignmentTypes.ToList();
                types.Remove(AssignmentTypes.TeamEvaluation);
                return types;
            }
        }

        public override ActionResult Index()
        {
            base.Index();
            ViewBag.ActiveCourse = ActiveCourse;
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = model;
            if (Assignment.ID != 0)
            {
                string rubricIdStr = Request.Form["RubricID"].ToString();
                int rubricId = 0;
                if (Int32.TryParse(rubricIdStr, out rubricId))
                {
                    Assignment = db.Assignments.Find(model.ID);
                    Assignment.RubricID = rubricId;
                    db.Entry(Assignment).State = System.Data.EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    WasUpdateSuccessful = false;
                }
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            ViewBag.ActiveCourse = ActiveCourse;
            return base.PostBack(Assignment);
        }
    }
}
