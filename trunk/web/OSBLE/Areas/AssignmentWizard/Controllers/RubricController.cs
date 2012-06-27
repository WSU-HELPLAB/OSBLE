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

            List<List<string>> rubricTable = new List<List<string>>();

            if (Assignment.HasRubric)
            {
                ViewBag.hasRubric = true;

                foreach (Criterion criterion in Assignment.Rubric.Criteria)
                {
                    List<string> row = new List<string>();

                    row.Add(criterion.CriterionTitle);
                    row.Add(criterion.Weight.ToString());

                    foreach (CellDescription cd in (from cd in Assignment.Rubric.CellDescriptions
                                                    where cd.CriterionID == criterion.ID
                                                    select cd).ToList())
                    {
                        row.Add(cd.Description);
                    }
                    rubricTable.Add(row);
                }
            }
            else
            {
                ViewBag.hasRubric = true;

                List<string> row = new List<string>();
                row.Add("");
                row.Add("");
                row.Add("");
                rubricTable.Add(row);
            }

            ViewBag.ActiveCourse = ActiveCourseUser;
            ViewBag.rubricTable = rubricTable;

            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            string a = Request.Params["rubric:0:S"];
            
            //reset our assignment
            Assignment = db.Assignments.Find(model.ID);

            if (Assignment.HasRubric)
            {
                //delete the old rubric (to be replaced with the one in the HTML)
               db.Rubrics.Remove(Assignment.Rubric);
               db.SaveChanges();
            }

            //Load the rubric from the view
            int rubricID = LoadRubricFromHTML();

            Assignment.RubricID = rubricID;
            db.Entry(Assignment).State = System.Data.EntityState.Modified;
            db.SaveChanges();

            return base.PostBack(Assignment);
        }

        /// <summary>
        /// Called by the HttpPost. Parses the rubric information from the view
        /// creates a model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>RubricID of newly created rubric</returns>
        private int LoadRubricFromHTML()
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

                if (Regex.Match(key, "rubric:").Success)
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

            bool hasGlobalComments = true;
            bool hasCriteriaComments = true;
            if (Request.Params["globalComments"] == null)
            {
                hasGlobalComments = false;
            }

            if (Request.Params["criterionComments"] == null)
            {
                hasCriteriaComments = false;
            }

            return CreateRubricModel(rubricTable,
                levelTitles,
                hasGlobalComments,
                hasCriteriaComments);
        }

        /// <summary>
        /// Given the paramaters describing a rubric, create a 
        /// rubric model and store it in the database. Note: the rubric is not yet
        /// associated with an assignment when this function returns.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="rubricTable">List of List of strings representing the body of a rubric
        ///         Each List of strings represents a row and contains the following:
        ///              Index 0: performance criterion title
        ///              Index 1: criterion weight
        ///              Index 2-n: level description</param>
        /// <param name="levelTitles">A list each level title</param>
        /// <param name="hasGlobalComments"></param>
        /// <param name="hasCriteriaComments"></param>
        /// <returns>RubricID of the new rubric</returns>
        private int CreateRubricModel(List<List<string>> rubricTable, 
            List<string> levelTitles,
            bool hasGlobalComments,
            bool hasCriteriaComments)
        {
            Rubric rubric = new Rubric();

            rubric.HasGlobalComments = hasGlobalComments;
            rubric.HasCriteriaComments = hasCriteriaComments;
            rubric.Description = "what is the rubric description";

            db.Rubrics.Add(rubric);
            db.SaveChanges();

            Assignment.RubricID = rubric.ID;

            //levels
            int p = 1;
            foreach (string levelTitle in levelTitles)
            {
                Level level = new Level();
                if (levelTitle == "")
                {
                    //WasUpdateSuccessful = false;
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
                if (int.TryParse(row[1], out weight))
                {
                    criterion.Weight = weight;
                }
                else
                {
                    criterion.Weight = 0;
                }
                criterion.RubricID = rubric.ID;
                db.Criteria.Add(criterion);
                p++;
            }

            db.SaveChanges();

            int i = 0;
            foreach (Criterion criterion in rubric.Criteria) //for each row 
            {
                int n = 2; // the level titles start at index 2 in rubricTable
                foreach (Level level in rubric.Levels) //
                {
                    CellDescription desc = new CellDescription();
                    desc.CriterionID = criterion.ID;
                    desc.LevelID = level.ID;
                    desc.RubricID = rubric.ID;

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

            return rubric.ID;
        }
    }
}
