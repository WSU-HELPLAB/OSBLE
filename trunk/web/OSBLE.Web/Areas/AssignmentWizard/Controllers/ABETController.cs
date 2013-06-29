// Created by Evan Olds for the OSBLE project at WSU
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using System.Data.Entity.Validation;
using System.Diagnostics;
using OSBLE.Areas.AssignmentWizard.Models;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class ABETController : WizardBaseController
    {
        //
        // GET: /AssignmentWizard/ABET/

        public override ActionResult Index()
        {
            base.Index();
            ModelState.Clear();

            // Make a list of departments
            List<string> depts = new List<string>();
            OSBLE.Models.FileSystem.OSBLEDirectory fs = OSBLE.Models.FileSystem.Directories.GetAdmin();
            OSBLE.Models.FileSystem.FileCollection fc = fs.File("departments.txt");
            if (0 == fc.Count)
            {
                // Administrator hasn't created the departments list yet. Leave the list 
                // empty so that the option will show up as disabled on the page.
            }
            else
            {
                IEnumerator<string> e = fc.GetEnumerator();
                e.MoveNext();
                string[] lines = System.IO.File.ReadAllLines(e.Current);
                depts.AddRange(lines);
            }
            ViewBag.DepartmentsList = depts;

            // We want a collection of DIVs on the page that list the outcome options 
            // for each department. We build the HTML for that here.
            StringBuilder sb = new StringBuilder();
            foreach (string dept in depts)
            {
                // We're expecting a text file with a name in the format:
                // [department]_abet_outcomes.txt
                string fname = System.IO.Path.Combine(fs.GetPath(), dept + "_abet_outcomes.txt");
                if (System.IO.File.Exists(fname))
                {
                    int i = 0;
                    string[] fileLines = System.IO.File.ReadAllLines(fname);
                    sb.AppendLine(
                        "<div id=\"" + dept + "\" style=\"display: none;\">");
                    foreach (string line in fileLines)
                    {
                        bool isChecked = ContainsOutcome(Assignment.ABETOutcomes, line);
                        sb.AppendFormat(
                            "  <input type=\"checkbox\" id=\"{0}\" name=\"{0}\" value=\"{1}\" {2}/>{1}<br />",
                            "cb" + dept + (i++).ToString(), line,
                            isChecked ? "checked " : string.Empty);
                    }
                    sb.AppendLine("</div>");
                }
                else
                {
                    // If the file DOESN'T exist then we want something on the page that 
                    // will let the user know to contact the admin.
                    sb.AppendLine(
                        "<div id=\"" + dept + "\" style=\"display: none;\">ABET outcomes for this " + 
                        "department are not yet available. Please contact your system administrator " + 
                        "about this issue.</div>");
                }
            }
            ViewBag.OptionsDIVsHTML = sb.ToString();

            return View(Assignment);
        }

        private static bool ContainsOutcome(IList<AbetAssignmentOutcome> outcomes, string outcomeName)
        {
            if (null != outcomes)
            {
                foreach (AbetAssignmentOutcome outcome in outcomes)
                {
                    if (outcome.Outcome == outcomeName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = db.Assignments.Find(model.ID);
            string dept = Request.Form["slctABETDepartment"];
            if (string.IsNullOrEmpty(dept))
            {
                dept = null;
            }
            Assignment.ABETDepartment = dept;

            // Delete all outcomes first (they will be reset below)
            while (Assignment.ABETOutcomes.Count > 0)
            {
                db.AbetSubmissionOutcomes.Remove(Assignment.ABETOutcomes[0]);
            }
            db.SaveChanges();

            // Look for all keys in form that start with "cb" and add their 
            // corresponding values to the list of ABET outcomes for the course.
            foreach (string key in Request.Form.AllKeys)
            {
                if (key.StartsWith("cb"))
                {
                    AbetAssignmentOutcome outcome = new AbetAssignmentOutcome();
                    outcome.Assignment = Assignment;
                    outcome.AssignmentID = Assignment.ID;
                    outcome.Outcome = Request.Form[key];
                    Assignment.ABETOutcomes.Add(outcome);
                }
            }

            WasUpdateSuccessful = true;

            //update our DB.  Note that we don't have logic for inserting new assignments
            //as we require that the basics controller come first and that handles new
            //assignment creation.
            db.Entry(Assignment).State = System.Data.EntityState.Modified;
            db.SaveChanges();
            return base.PostBack(model);
        }

        public override string ControllerName
        {
            get { return "ABET"; }
        }

        public override string ControllerDescription
        {
            get { return "The instructor will tag student work for ABET assessment"; }
        }

        public override IWizardBaseController Prerequisite
        {
            get
            {
                return new BasicsController();
            }
        }

        public override bool IsRequired
        {
            get
            {
                return false;
            }
        }

        public override ICollection<OSBLE.Models.Assignments.AssignmentTypes> ValidAssignmentTypes
        {
            get { return base.AllAssignmentTypes; }
        }
    }
}
