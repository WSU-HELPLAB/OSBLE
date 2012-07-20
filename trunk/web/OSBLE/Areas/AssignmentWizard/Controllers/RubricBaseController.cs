using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses.Rubrics;
using System.IO;
using OSBLE.Resources;
using System.Text.RegularExpressions;
using OSBLE.Models.Courses;
using OSBLE.Areas.AssignmentWizard.ViewModels;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public abstract class RubricBaseController : WizardBaseController
    {
        public override ActionResult Index()
        {
            return base.Index();
        }

        [HttpPost]
        public ActionResult LoadExistingRubric(int rubricID)
        {
            base.Index();
            return LoadRubric(rubricID);
        }

        [HttpPost]
        public ActionResult LoadRubricFromCsv(HttpPostedFileBase file)
        {
            base.Index();
            CreateRubricSelectionViewModel();

            //fill out the rubric from the csv file
            MemoryStream rubricStream = new MemoryStream();
            file.InputStream.CopyTo(rubricStream);
            CsvParser csv = new CsvParser(rubricStream);

            List<string> row = csv.getNextRow();

            IList<Level> levels = new List<Level>();

            Regex rgx = new Regex(@"\(\d+-\d+ pts\)");

            for (int i = 2; i < row.Count; i++)
            {
                Level l = new Level();
                Match match = rgx.Match(row[i]);

                //if level piont spread was specified:
                if (match.Success)
                {
                    row[i] = rgx.Replace(row[i], "");

                    //get the numbers out of the match and add them to the level point spread fields
                    MatchCollection spread = Regex.Matches(match.ToString(), @"\d+");
                    if (spread.Count == 2)
                    {
                        //set the point range:
                        l.PointSpread = Int32.Parse(spread[1].ToString());
                    }

                }

                l.LevelTitle = row[i];
                levels.Add(l);
            }

            List<List<string>> rubricTable = new List<List<string>>();
            while (row != null)
            {
                row = csv.getNextRow();
                if (row != null)
                {
                    rubricTable.Add(row);
                }
            }

            string rubricDescription = file.FileName;

            ViewBag.rubricDescription = rubricDescription;
            ViewBag.levels = levels;
            ViewBag.ActiveCourse = ActiveCourseUser;
            ViewBag.rubricTable = rubricTable;

            return View("Index", Assignment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="student">if true, load the student rubric</param>
        /// <returns></returns>
        protected ActionResult LoadRubric(int? rubricID)
        {
            CreateRubricSelectionViewModel();

            List<List<string>> rubricTable = new List<List<string>>();
            IList<Level> levels = new List<Level>();
            string rubricDescription;

            Rubric rubric = db.Rubrics.Find(rubricID);

            if (rubric != null)
            {
                rubricDescription = rubric.Description;
                levels = rubric.Levels;
                foreach (Criterion criterion in rubric.Criteria)
                {
                    List<string> row = new List<string>();

                    row.Add(criterion.CriterionTitle);
                    row.Add(criterion.Weight.ToString());

                    foreach (CellDescription cd in (from cd in rubric.CellDescriptions
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
                //create empty rubric
                rubricDescription = null;
                ViewBag.hasRubric = true;
                List<string> row = new List<string>();
                row.Add("");
                row.Add("");
                row.Add("");
                rubricTable.Add(row);
                levels.Add(new Level());
            }

            ViewBag.rubricDescription = rubricDescription;
            ViewBag.levels = levels;
            ViewBag.ActiveCourse = ActiveCourseUser;
            ViewBag.rubricTable = rubricTable;

            return View("Index", Assignment);
        }

        private void CreateRubricSelectionViewModel()
        {
            List<CourseUser> myCourseUsers = (from cu in db.CourseUsers
                                              where cu.UserProfileID == ActiveCourseUser.UserProfileID
                                              select cu).ToList();

            List<Course> myCourses = (from cu in myCourseUsers
                                      select cu.AbstractCourse as Course).ToList();

            RubricSelectionViewModel rubricSelectionViewModel = new RubricSelectionViewModel();

            foreach (Course course in myCourses)
            {
                rubricSelectionViewModel.AddCourse(course);
            }

            ViewBag.rubricSelection = rubricSelectionViewModel;
        }



        /// <summary>
        /// Called by the HttpPost. Parses the rubric information from the view
        /// creates a model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>RubricID of newly created rubric</returns>
        protected int LoadRubricFromHTML()
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
            List<int> pointSpreads = new List<int>();

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

            pointSpreads = (from k in Keys
                            where Regex.Match(k, @"rubric:\d+:S").Success
                            orderby k
                            select Int32.Parse(Request.Params[k])).ToList();

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

            string rubricDescription = Request.Params["rubricDescription"];
            if (rubricDescription == null || rubricDescription == "")
            {
                rubricDescription = "Rubric for " + Assignment.AssignmentName;
            }

            return CreateRubricModel(rubricTable,
                levelTitles,
                pointSpreads,
                hasGlobalComments,
                hasCriteriaComments,
                rubricDescription);
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
            List<int> pointSpreads,
            bool hasGlobalComments,
            bool hasCriteriaComments,
            string rubricDescription)
        {
            Rubric rubric = new Rubric();

            rubric.HasGlobalComments = hasGlobalComments;
            rubric.HasCriteriaComments = hasCriteriaComments;
            rubric.Description = rubricDescription;

            db.Rubrics.Add(rubric);
            db.SaveChanges();

            //Assignment.RubricID = rubric.ID;

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
                level.PointSpread = pointSpreads[p - 1];
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
