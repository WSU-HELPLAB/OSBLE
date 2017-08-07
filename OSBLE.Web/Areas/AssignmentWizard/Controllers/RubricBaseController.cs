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
            bool globalCommentsChecked = false;
            bool criteriaCommentsChecked = false;
            bool enableHalfStep = false;
            bool enableQuarterStep = false;

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
                globalCommentsChecked = rubric.HasGlobalComments;
                criteriaCommentsChecked = rubric.HasCriteriaComments;
                enableHalfStep = rubric.EnableHalfStep;
                enableQuarterStep = rubric.EnableQuarterStep;
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

            ViewBag.EvalsExist = (from re in db.RubricEvaluations
                                  where re.AssignmentID == Assignment.ID
                                  select re).Count() > 0;

            ViewBag.globalCommentsCheckedValue = globalCommentsChecked ? "checked" : "";
            ViewBag.criteriaCommentsCheckedValue = criteriaCommentsChecked ? "checked" : "";
            ViewBag.enableHalfStepCheckedValue = enableHalfStep ? "checked" : "";
            ViewBag.enableQuarterStepCheckedValue = enableQuarterStep ? "checked" : "";
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
        /// This function will create a rubric if one does not exist, 
        /// If a rubric already exists, this function will update a rubric after an edit if no rows or columns have been added, otherwise it will delete the rubric and recreate it.
        /// </summary>
        protected void UpdateRubric()
        {

            //Grabbing all needed information to create or update the rubric
            List<string> keys = getFormKeys();
            List<List<string>> rubricTable = getRubricTable(keys);
            List<string> levelTitles = getLevelTitles(keys);
            List<int> pointSpreads = getPointSpreads(keys);
            bool globalComments = hasGlobalComments();
            bool criteriaComments = hasCriteriaComments();
            bool enableHalfStep = allowHalfStep();
            bool enableQuarterStep = allowQuarterStep();
            string rubricDescription = getRubricName();

            if (Assignment.Rubric != null) //A rubric already exists
            {
                //Grabbing current rubric's row/column count
                int currentRowCount = Assignment.Rubric.Criteria.Count;
                int currentColumnCount = Assignment.Rubric.Levels.Count;

                //getting the new rubric's row/column count
                int updatedRubricRowCount = rubricTable.Count;
                int updatedRubricColumnCount = rubricTable[0].Count - 2;    //Minus 2 to so Criterion weight/performance criterion columns are not counted

                //If there are equal row/columns, update the rubric without deleting the rubric. 
                if (currentColumnCount == updatedRubricColumnCount && currentRowCount == updatedRubricRowCount)
                {
                    //Updating a rubric equates to 
                    //updating Rubric: updating HasCriteriaCOmments, HasGlobalComments, and DEscription (for name) of the rubirc
                    //updating Rubric's Crition(s): weight and criteriontitle
                    //updating Rubric's CellDescriptions: only Description needs to be modified
                    //updating Rubric's Level(s): pointspread (if modified, must modify existing REs' (RubricEvaluations) scores), and leveltitle


                    //Updating Rubric
                    Assignment.Rubric.Description = rubricDescription;
                    Assignment.Rubric.HasCriteriaComments = criteriaComments;
                    Assignment.Rubric.HasGlobalComments = globalComments;
                    Assignment.Rubric.EnableHalfStep = enableHalfStep;
                    Assignment.Rubric.EnableQuarterStep = enableQuarterStep;

                    //Updating Criteria and CellDescriptions
                    int outVal = 0;
                    for (int i = 0; i < Assignment.Rubric.Criteria.Count; i++)
                    {
                        //Criteria Titles are in the first column, weights are in the second column
                        Assignment.Rubric.Criteria[i].CriterionTitle = rubricTable[i][0];
                        int.TryParse(rubricTable[i][1], out outVal);
                        Assignment.Rubric.Criteria[i].Weight = outVal;

                        //Grabbing all CellDescriptions for that Criterion (based off the ID)
                        List<CellDescription> currentRowCells = Assignment.Rubric.CellDescriptions.Where(cd => cd.CriterionID == Assignment.Rubric.Criteria[i].ID).ToList();
                        for (int j = 0; j < currentRowCells.Count; j++)
                        {
                            currentRowCells[j].Description = rubricTable[i][j + 2]; //j + 2 to offset from weight/performacne crit columns
                        }
                    }

                    //Updating Levels, must know sum of old and new point spreads to recalculate RubricEvaluation scores.
                    double oldLevelPointSpreadSum = 0;
                    double newLevelPointSpreadSum = 0;
                    for (int i = 0; i < Assignment.Rubric.Levels.Count; i++)
                    {
                        oldLevelPointSpreadSum += Assignment.Rubric.Levels[i].PointSpread;
                        newLevelPointSpreadSum += pointSpreads[i];
                        Assignment.Rubric.Levels[i].PointSpread = pointSpreads[i];
                        Assignment.Rubric.Levels[i].LevelTitle = levelTitles[i];
                    }

                    //Recalculating any existing RubricEvaluations' CriteronEvaluations' scores as the pointSpread may have changed
                    List<RubricEvaluation> rubricEvals = (from re in db.RubricEvaluations
                                                          where re.AssignmentID == Assignment.ID
                                                          select re).ToList();

                    foreach (RubricEvaluation re in rubricEvals)
                    {
                        List<CriterionEvaluation> critEvals = re.CriterionEvaluations.ToList();
                        //Might need to rearrange critEvals depending on row swaps performed on rubric.

                        for (int i = 0; i < critEvals.Count; i++)
                        {
                            if (critEvals[i].Score.HasValue)
                            {
                                //To adjust scores, we must multiply their score by the ratio of Pointsread change. 
                                //(i.e. if the spread goes from 5 to 10, their scores must be doubled)
                                double multiplier = newLevelPointSpreadSum / oldLevelPointSpreadSum;
                                double newScore = critEvals[i].Score.Value * multiplier;
                                if (newScore > newLevelPointSpreadSum) //If rounding up leads them to a higher grade than possible, set it to max.
                                {
                                    newScore = newLevelPointSpreadSum;
                                }
                                critEvals[i].Score = newScore;
                                db.Entry(critEvals[i]).State = System.Data.EntityState.Modified;
                            }
                        }
                    }
                    db.SaveChanges();
                }
                else //Rows and columns were not equal, delete and recreate rubric.
                {
                    int rubricID = CreateRubricModel(rubricTable,
                                        levelTitles,
                                        pointSpreads,
                                        globalComments,
                                        criteriaComments,
                                        enableHalfStep,
                                        enableQuarterStep,
                                        rubricDescription);
                    Assignment.RubricID = rubricID;
                    db.Entry(Assignment).State = System.Data.EntityState.Modified;
                    List<RubricEvaluation> toRemove = (from re in db.RubricEvaluations where re.AssignmentID == Assignment.ID select re).ToList();
                    foreach (RubricEvaluation re in toRemove)
                    {
                        db.RubricEvaluations.Remove(re);
                    }
                    db.SaveChanges();
                }
            }
            else //No rubric exists, create a new one
            {
                int rubricID = CreateRubricModel(rubricTable,
                                        levelTitles,
                                        pointSpreads,
                                        globalComments,
                                        criteriaComments,
                                        enableHalfStep,
                                        enableQuarterStep,
                                        rubricDescription);
                Assignment.RubricID = rubricID;
                db.Entry(Assignment).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
        }

        private List<int> getSwapList()
        {
            List<int> joedirt = new List<int>();
            return joedirt;
        }

        /// <summary>
        /// Returns a list of keys from the HttpPost
        /// </summary>
        /// <returns></returns>
        private List<string> getFormKeys()
        {
            List<string> Keys = new List<string>();
            //acquire a list of all relevant keys for the rubric
            foreach (string key in Request.Params.Keys)
            {
                if (Regex.Match(key, "rubric:").Success)
                {
                    Keys.Add(key);
                }
            }
            return Keys;
        }

        /// <summary>
        /// Returns a list of level titles from the HttpPost
        /// </summary>
        /// <param name="keys">Keys received from getKeysFromHtml()</param>
        /// <returns></returns>
        private List<string> getLevelTitles(List<string> keys)
        {
            List<string> levelTitles = new List<string>();

            levelTitles = (from k in keys
                           where Regex.Match(k, @"rubric:\d+:L").Success
                           orderby k
                           select Request.Params[k]).ToList();

            return levelTitles;
        }

        /// <summary>
        /// Returns a list of integers representing the point spreads for the levels of a rubric from an HttpPost
        /// </summary>
        /// <param name="keys">Keys received from getKeysFromHtml()</param>
        /// <returns></returns>
        private List<int> getPointSpreads(List<string> keys)
        {
            List<int> pointSpreads = new List<int>();

            pointSpreads = (from k in keys
                            where Regex.Match(k, @"rubric:\d+:S").Success
                            orderby k
                            select Int32.Parse(Request.Params[k])).ToList();

            return pointSpreads;
        }

        /// <summary>
        /// Returns a list of strings representing all but the first first row (Level row) of a rubric from the HttpPost
        /// </summary>
        /// <param name="keys">Keys received from getKeysFromHtml()</param>
        /// <returns></returns>
        private List<List<string>> getRubricTable(List<string> keys)
        {

            List<List<string>> rubricTable = new List<List<string>>(); // store the entire table, excluding the top row
            List<string> RowStrings = (from k in keys
                                       where Regex.Match(k, @"rubric:0:\d+").Success
                                       select k).ToList();
            foreach (string rowstring in RowStrings)
            {
                //splitkey = {rubric, X, Y}
                string[] splitKey = rowstring.Split(':');

                //AC: This regex won't work quite right with rubrics having more than 9 rows.
                //  example: rubrics:0:1 and rubrics:0:10 will both match the following regular
                //  expression.  To get around this, I'm doing a secondary loop which checks to
                //  make sure that we're only grabbing the correct row.
                Regex r = new Regex(@"rubric:\d+:" + splitKey[2]);
                List<string> potentialKeys = (from k in keys
                                              where r.Match(k).Success == true
                                              select k).ToList();
                List<string> rowValues = new List<string>();
                int keyLength = splitKey[2].Length;
                string endPattern = string.Format(":{0}", splitKey[2]);
                foreach (string potentialKey in potentialKeys)
                {
                    if (potentialKey.EndsWith(endPattern) == true)
                    {
                        rowValues.Add(Request.Params[potentialKey]);
                    }
                }

                rubricTable.Add(rowValues);
            }
            return rubricTable;
        }

        /// <summary>
        /// Returns true if the rubric from the HttpPost has global comments enabled
        /// </summary>
        /// <returns></returns>
        private bool hasGlobalComments()
        {
            return Request.Params["globalComments"] != null;
        }

        /// <summary>
        /// Returns true if the rubric from the HttpPost has criteria comments enabled
        /// </summary>
        /// <returns></returns>
        private bool hasCriteriaComments()
        {
            return Request.Params["criterionComments"] != null;
        }

        /// <summary>
        /// Returns true if the rubric from the HttpPost has half-point grades enabled
        /// </summary>
        /// <returns></returns>
        private bool allowHalfStep()
        {
            string stepValue = Request.Form["gradeStepRadio"].ToString();
            if (stepValue == "halfStep") return true;
            return false;
        }

        /// <summary>
        /// Returns true if the rubric from the HttpPost has quarter-point grades enabled
        /// </summary>
        /// <returns></returns>
        private bool allowQuarterStep()
        {
            string stepValue = Request.Form["gradeStepRadio"].ToString();
            if (stepValue == "quarterStep") return true;
            return false;
        }

        /// <summary>
        /// Returns the name of the rubric from the HttpPost
        /// </summary>
        /// <returns></returns>
        private string getRubricName()
        {
            string rubricDescription = Request.Params["rubricDescription"];
            if (rubricDescription == null || rubricDescription == "")   //Automatically assign name if one is not given
            {
                rubricDescription = "Rubric for " + Assignment.AssignmentName;
            }
            return rubricDescription;
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
            bool enableHalfStep = true;
            bool enableQuarterStep = true;

            if (Request.Params["globalComments"] == null)
            {
                hasGlobalComments = false;
            }

            if (Request.Params["criterionComments"] == null)
            {
                hasCriteriaComments = false;
            }

            if (Request.Params["halfStep"] == null)
            {
                enableHalfStep = false;
            }

            if (Request.Params["quarterStep"] == null)
            {
                enableQuarterStep = false;
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
                enableHalfStep,
                enableQuarterStep,
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
            bool enableHalfStep,
            bool enableQuarterStep,
            string rubricDescription)
        {
            Rubric rubric = new Rubric();

            rubric.HasGlobalComments = hasGlobalComments;
            rubric.HasCriteriaComments = hasCriteriaComments;
            rubric.EnableHalfStep = enableHalfStep;
            rubric.EnableQuarterStep = enableQuarterStep;
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
                    criterion.Weight = 1;
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
