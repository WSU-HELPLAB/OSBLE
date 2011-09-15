﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Utility;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class BasicsController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "Basics"; }
        }

        public override string ControllerDescription 
        {
            get
            {
                return "Basic assignment information";
            }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get
            {
                //the basics page has no prereqs
                return new List<WizardBaseController>();
            }
        }

        public override ActionResult Index()
        {
            base.Index();
            ModelState.Clear();

            //SUBMISSION CATEGORIES
            var cat = from c in (activeCourse.AbstractCourse as Course).Categories
                      where c.Name != Constants.UnGradableCatagory
                      select c;
            ViewBag.Categories = new SelectList(cat, "ID", "Name");

            //ASSIGNMENT TYPES
            var types = db.AssignmentTypes.ToList();
            ViewBag.AssignmentTypes = new SelectList(types, "Type", "Type");

            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = model;
            if (ModelState.IsValid)
            {
                WasUpdateSuccessful = true;

                //update our DB
                if (Assignment.ID == 0)
                {
                    db.Assignments.Add(Assignment);
                }
                else
                {
                    db.Entry(Assignment).State = System.Data.EntityState.Modified;
                }
                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            return View(Assignment);
        }
    }
}
