using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Assignments;
using System.Text.RegularExpressions;

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
                //return new List<AssignmentTypes>();
                List<AssignmentTypes> types = base.AllAssignmentTypes.ToList();
                types.Remove(AssignmentTypes.TeamEvaluation);
                return types;
            }
        }

        public override ActionResult Index()
        {
            base.Index();

            //List of other assignments
            List<Assignment> assignments = new List<Assignment>();

            ViewBag.ActiveCourse = ActiveCourseUser;
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            // Each input element is stored in Request.Params where the key in params is the name of the element.
            // The location of the element can be determined by its name.
            // Excluding the first row, the name of each input element is of the form "rubric:X:Y" where x and y correspond
            // to the x and y location on the table.
            // The second row of the rubric (the row below column titles) starts at Y=0
            // The Y value for the level titles in the first row is "L" (rubric:N:L) where N is the 
            // level number, starting at 0, and L denotes a Level Title.

            List<List<string>> rubricTable = new List<List<string>>(); // store the entire table, excluding the top row
            //store the level title values
            List<string> levelTitles = new List<string>();

            List<string> Keys = new List<string>();
            //acquire a list of all relevant keys for the rubric
            foreach (string key in Request.Params.Keys)
            {
                Match match = Regex.Match(key, "rubric:");
                if (match.Success)
                {
                    Keys.Add(key);
                }
            }

            //generate a list of level titles
            levelTitles = (from k in Keys
                              where Regex.Match(k, @"rubric:\d+:L").Success
                              orderby k
                              select Request.Params[k]).ToList();

            List<string> RowStrings = (from k in Keys
                                       where Regex.Match(k, @"rubric:0:\d+").Success
                                       select k).ToList();

            foreach (string rowstring in RowStrings)
            {
                //splitkey = {rubric, X, Y}
                string[] splitKey = rowstring.Split(':');

                Regex r = new Regex(@"rubric:\d+:" + splitKey[2]);

                List<string> rowValues = (from k in Keys
                                    where r.Match(k).Success
                                    orderby k
                                    select Request.Params[k]).ToList();

                rubricTable.Add(rowValues);
            }


            Rubric rubric = new Rubric();

            if (Request.Params["globalComments"] == null)
            {
                rubric.HasGlobalComments = false;
            }
            else
            {
                rubric.HasGlobalComments = true;
            }

            if (Request.Params["criterionComments"] == null)
            {
                rubric.HasCriteriaComments = false;
            }
            else
            {
                rubric.HasCriteriaComments = true;
            }

            rubric.Description = "gibberish";

            //save rubric here

            db.Rubrics.Add(rubric);
            db.SaveChanges();

            model.RubricID = rubric.ID;
            
            //levels
            int p = 1;
            foreach (string levelTitle in levelTitles)
            {
                Level level = new Level();
                if (levelTitle == "")
                {
                    level.LevelTitle = "Level Title " + p.ToString();
                }
                else
                {
                    level.LevelTitle = levelTitle;
                }
                level.RangeStart = 0;
                level.RangeEnd = 4;
                level.RubricID = rubric.ID;
                db.Levels.Add(level);
                p++;
            }
          
            //Criteria
            p = 1;
            foreach (List<string> row in rubricTable)
            {
                Criterion criterion = new Criterion();
                
                if (row[0] == "")
                {
                    criterion.CriterionTitle = "Criterion " + p.ToString();
                }
                else
                {
                    criterion.CriterionTitle = row[0];
                }

                int weight;
                if(int.TryParse(row[1], out weight))
                {
                    criterion.Weight = weight;
                }
                else{
                    criterion.Weight = 0;
                }
                criterion.RubricID = rubric.ID;
                db.Criteria.Add(criterion);
                p++;
            }

            db.SaveChanges();

            int i = 0;
            foreach(Criterion criterion in rubric.Criteria) //for each row 
            {
                int n = 2; // the level titles start at index 2 in rubricTable
                foreach (Level level in rubric.Levels) //
                {
                    CellDescription desc = new CellDescription();
                    desc.CriterionID = criterion.ID;
                    desc.LevelID = level.ID;
                    
                    if (rubricTable[i][n] == "")
                    {
                        desc.Description = "Description for " + level.LevelTitle;
                    }
                    else
                    {
                        desc.Description = rubricTable[i][n];
                    }

                    db.CellDescriptions.Add(desc);
                    rubric.CellDescriptions.Add(desc);
                    n++;
                }
                i++;
            }

            Assignment = db.Assignments.Find(model.ID);
            Assignment.RubricID = rubric.ID;
            db.Entry(Assignment).State = System.Data.EntityState.Modified;
            db.SaveChanges();

            return base.PostBack(Assignment);
        }
    }
}
