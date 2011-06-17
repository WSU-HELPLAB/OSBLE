using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.HomePage;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.Assignments.Activities.Scores;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Assignments;
using System.IO;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class GradebookController : OSBLEController
    {
        public enum ColumnAction { InsertLeft, InsertRight, Delete, Clear, NoAction };

        //
        // GET: /Gradebook/
        public GradebookController()
            : base()
        {
            ViewBag.CurrentTab = "Grades";
        }

        /// <summary>
        /// Adds a column (Gradable) to the current Tab (Weight).
        /// </summary>
        /// <param name="colunmName">The name of column</param>
        /// <param name="pointsPossible">The number of points that this column is worth</param>
        /// <param name="position">The order in which this column should appear</param>
        /// <returns></returns>
        
        
        private void AddColumn(string colunmName, int pointsPossible, int position)
        {
            int categoryId = Convert.ToInt32(Session["CurrentCategoryID"]);

            GradeAssignment newAssignment = new GradeAssignment()
            {
                Name = "Untitled",
                PointsPossible = 100,
                AssignmentActivities = new List<AssignmentActivity>(),
                CategoryID = categoryId,
                ColumnOrder = 0
            };
            db.GradeAssignments.Add(newAssignment);
            db.SaveChanges();
        }

        /************
        /// <summary>
        /// Updates a preexisting column (gradable)
        /// </summary>
        /// <param name="gradableId">The ID of the gradable to be updated</param>
        /// <param name="colunmName">The gradables new column name</param>
        /// <param name="pointsPossible">The total points possible for the gradable</param>
        /// <param name="position">The relative position of the gradable with respect to other gradables for the current Weight</param>
        /// <returns></returns>
        [HttpPost]
        public void EditColumn(int gradableId, string colunmName, int pointsPossible, int position)
        {
            if (ModelState.IsValid)
            {
                var gradableQuery = from g in db.Gradables
                                    where g.ID == gradableId
                                    select g;
                if (gradableQuery.Count() > 0)
                {
                    Gradable gradable = gradableQuery.First();
                    gradable.Name = colunmName;
                    gradable.PossiblePoints = pointsPossible;
                    gradable.Position = position;
                    db.SaveChanges();
                }
            }
        }*/

        /// <summary>
        /// Deletes a column (gradable) from the current table
        /// </summary>
        /// <param name="gradableId">The ID of the gradable to remove</param>
        /// <returns></returns>
        [HttpPost]
        public void DeleteColumn(int assignmentId)
        {
            AbstractAssignment assignment = db.AbstractAssignments.Find(assignmentId);
            db.AbstractAssignments.Remove(assignment);
            db.SaveChanges();
        }

        [HttpPost]
        public void ClearDropLowest(int categoryId, int userId)
        {
            if (ModelState.IsValid)
            {
                if (categoryId > 0)
                {
                    var studentScores = from scores in db.Scores
                                        where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                                        && scores.UserProfileID == userId
                                        && scores.isDropped == true
                                        select scores;
                    if (studentScores.Count() > 0)
                    {
                        foreach (Score score in studentScores)
                        {
                            score.isDropped = false;
                        }
                        db.SaveChanges();
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult ClearAllDropLowest(int categoryId)
        {
            if (ModelState.IsValid)
            {
                if (categoryId > 0)
                {
                    var studentScores = from scores in db.Scores
                                        where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                                        && scores.isDropped == true
                                        select scores;
                    if (studentScores.Count() > 0)
                    {
                        foreach (Score score in studentScores)
                        {
                            score.isDropped = false;
                        }
                        db.SaveChanges();
                    }
                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult DropLowest(int categoryId, int userId)
        {
            if (ModelState.IsValid)
            {
                double lowest = 10000;
                int id = 0;
                if (categoryId > 0)
                {
                    var studentScores = from scores in db.Scores
                                        join user in db.UserProfiles on scores.UserProfileID equals user.ID
                                        where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                                        && scores.UserProfileID == userId
                                        && scores.isDropped == false
                                        group scores by scores.UserProfileID into userScores
                                        select userScores;

                    if (studentScores.Count() > 0)
                    {
                        for (int i = 0; i < studentScores.Count(); i++)
                        {
                            var item = studentScores.AsEnumerable().ElementAt(i);
                            foreach (Score score in item)
                            {
                                if (score.Points < lowest)
                                {
                                    lowest = score.Points;
                                    id = score.ID;
                                }
                            }
                            foreach (Score score in item)
                            {
                                if (score.ID == id)
                                {
                                    score.isDropped = true;
                                }
                            }
                            db.SaveChanges();
                            lowest = 10000;
                        }
                    }
                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }


        [HttpPost]
        public ActionResult AllDropLowest(int categoryId)
        {
            if (ModelState.IsValid)
            {
                double lowest = 10000;
                int id = 0;
                if (categoryId > 0)
                {
                    var studentScores = from scores in db.Scores
                                        join user in db.UserProfiles on scores.UserProfileID equals user.ID
                                        where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                                        && scores.isDropped == false
                                        group scores by scores.UserProfileID into userScores
                                        select userScores;

                    if (studentScores.Count() > 0)
                    {
                        for (int i = 0; i < studentScores.Count(); i++)
                        {
                            var item = studentScores.AsEnumerable().ElementAt(i);
                            foreach (Score score in item)
                            {
                                if (score.Points < lowest)
                                {
                                    lowest = score.Points;
                                    id = score.ID;
                                }
                            }
                            foreach (Score score in item)
                            {
                                if (score.ID == id)
                                {
                                    score.isDropped = true;
                                }
                            }
                            db.SaveChanges();
                            lowest = 10000;
                        }
                    }
                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }

        
        /// <summary>
        /// Clears a gradable (column) from the current table.
        /// Changes all the numbers in the gradable to 0.
        /// </summary>
        /// <param name="gradableId">The ID of the gradable to clear</param>
        [HttpPost]
        public void ClearColumn(int assignmentId)
        {
            var assignmentQuery = from s in db.Scores
                                  where s.AssignmentActivity.AbstractAssignmentID == assignmentId
                                  select s;

            if (assignmentQuery.Count() > 0)
            {
                foreach (Score item in assignmentQuery)
                {
                    item.Points = -1;
                }
                db.SaveChanges();
            }

            var assignmentPoints = from a in db.AbstractAssignments
                                   where a.ID == assignmentId
                                   select a;

            if (assignmentPoints.Count() > 0)
            {
                foreach (AbstractAssignment item in assignmentPoints)
                {
                    item.PointsPossible = 0;
                }
                db.SaveChanges();
            }
        } 
        
        private ColumnAction StringToColumnAction(string action)
        {
            if (string.Compare(ColumnAction.InsertLeft.ToString(), action) == 0)
            {
                return ColumnAction.InsertLeft;
            }
            else if (string.Compare(ColumnAction.InsertRight.ToString(), action) == 0)
            {
                return ColumnAction.InsertRight;
            }
            else if (string.Compare(ColumnAction.Clear.ToString(), action) == 0)
            {
                return ColumnAction.Clear;
            }
            else if (string.Compare(ColumnAction.Delete.ToString(), action) == 0)
            {
                return ColumnAction.Delete;
            }
            
            return ColumnAction.NoAction;
        }

        [HttpPost]
        public ActionResult ModifyCategoryPoints(double value, int categoryId)
        {
            if (ModelState.IsValid)
            {
                if (categoryId != 0)
                {
                    var category = from c in db.Categories
                                   where c.ID == categoryId
                                   select c;

                    if (category.Count() > 0)
                    {
                        foreach (Category item in category)
                        {
                            item.Points = value;
                        }
                        db.SaveChanges();
                    }
                }
            }
            return View("Index");
        }

        [HttpPost]
        public ActionResult ModifyCategoryName(string value, int categoryId)
        {
            if (ModelState.IsValid)
            {
                if (categoryId != 0)
                {
                    var category = from c in db.Categories
                                   where c.ID == categoryId
                                   select c;

                    if (category.Count() > 0)
                    {
                        foreach (Category item in category)
                        {
                            item.Name = value;
                        }
                        db.SaveChanges();
                    }
                }
            }
            return View("_Tabs");
        }

        [HttpPost]
        public ActionResult ModifyAssignmentName(string value, int assignmentId)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId != 0)
                {
                    var assignmentQuery = from a in db.AbstractAssignments
                                          where a.ID == assignmentId
                                          select a;

                    if (assignmentQuery.Count() > 0)
                    {
                        foreach (AbstractAssignment item in assignmentQuery)
                        {
                            item.Name = value;
                        }
                        db.SaveChanges();
                    }
                }
            }
            return View("_Gradebook");
        }

        
        [HttpPost]
        public ActionResult ModifyPossiblePoints(int value, int assignmentId)
        {

            if (ModelState.IsValid)
            {
                if (assignmentId != 0)
                {
                    var gradableQuery = from g in db.AbstractAssignments
                                        where g.ID == assignmentId
                                        select g;

                    if (gradableQuery.Count() > 0)
                    {
                        foreach (AbstractAssignment item in gradableQuery)
                        {
                            item.PointsPossible = value;
                        }
                        db.SaveChanges();
                    }
                }
                else
                {
                    Json("failure");
                }
            }
                return View("_Gradebook");
            //return RedirectToAction("Index");
        }
        

        [HttpPost]
        public ActionResult ModifyCell(int value, int userId, int gradableId)
        {
            //Continue if we have a valid gradable ID
            if (assignmentId != 0)
            {
                var gradableQuery = from g in db.GradableScores
                                    where g.UserProfileID == userId
                                    && g.GradableID == gradableId
                                    select g;
                if (gradableQuery.Count() > 0)
                {
                    foreach (Score item in gradableQuery)
                    {
                        item.Points = value;
                    }
                    db.SaveChanges();
                }
                else
                {
                    Score newScore = new Score()
                    {
                        UserProfileID = userId,
                        Score = value
                    };

                    db.Scores.Add(newScore);
                    db.SaveChanges();
                }
            }

            //BuildGradebook((int)Session["categoryId"]);
            //return View("_Gradebook");
        }
        

        [HttpPost]
        public ActionResult ModifyColumn()
        {
            //convert POST params to variables
            int assignmentId = 0;
            if (Request.Form["assignmentId"] != null)
            {
                assignmentId = Convert.ToInt32(Request.Form["assignmentId"]);
            }
            ColumnAction action = StringToColumnAction(Request.Form["actionRequested"]);

            //only continue if we received a valid gradable id
            if (assignmentId != 0)
            {
                AbstractAssignment assignments = (from g in db.AbstractAssignments where g.ID == assignmentId select g).First();
                switch (action)
                {
                    case ColumnAction.InsertLeft:
                        int position = (assignments.ColumnOrder == 0) ? 0 : assignments.ColumnOrder - 1;
                        AddColumn("Untitled", 100, position);
                        break;
                    case ColumnAction.InsertRight:
                        AddColumn("Untitled", 100, assignments.ColumnOrder + 1);
                        break;
                    case ColumnAction.Delete:
                        DeleteColumn(assignmentId);
                        break;
                    case ColumnAction.Clear:
                        ClearColumn(assignmentId);
                        break;
                        
                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult UpdateCells()
        {
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult ImportColumnsFromCSV()
        {
            int currentCourseId = ActiveCourse.CourseID;

            var students = from student in db.CoursesUsers
                           where student.CourseID == currentCourseId
                           group student by student.UserProfile.ID into studentList
                           select studentList;

            return View();
        }
         

        /// <summary>
        /// This will initialize the page using the supplied weightId (tab).
        /// </summary>
        /// <param name="weightId">The weight to load</param>
        /// <returns></returns>
        public ActionResult Index(int? weightId)
        {
            if (categoryId == null)
            {
                categoryId = GetDefaultWeightId();
            }
            Session["categoryId"] = categoryId;
            BuildGradebook((int)categoryId);
            return View();
        }

        /// <summary>
        /// This function is responsible for making the various calls needed to build the gradebook.
        /// Originally, this code was in the Index() action, but it was moved into its own function
        /// because I (AC) needed to rebuild the gradebook whenever a structural change (add/remove column, etc.)
        /// was made.
        ///
        /// After building the gradebook, this function makes the necessary components available to the
        /// View via the ViewBag.  The components are:
        ///  ViewBag.Tabs = All the tabs present in the gradebook
        ///  ViewBag.Gradables = The columns for the current tab
        ///  ViewBag.Grades = Student scores for each column
        ///  ViewBag.Users = List of students in the course
        /// </summary>
        private void BuildGradebook(int categoryId)
        {
            //LINQ complains when we use this directly in our queries, so pull it beforehand
            int currentCourseId = ActiveCourse.CourseID;

            //List of students scores
            List<Score> studentScores = new List<Score>();

            //pull all weights (tabs) for the current course
            var weights = from weight in db.Weights
                          where weight.CourseID == currentCourseId
                          select weight;
            List<Weight> allWeights = weights.ToList();
            Weight currentTab = (from w in allWeights where w.ID == weightId select w).First();

            List<Category> allCategories = cats.ToList();

            Category currentTab = (from c in allCategories where c.ID == categoryId select c).First();

            //save to the session.  Needed later for AJAX-related updates.
            Session["CurrentCategoryId"] = currentTab.ID;

            //pull the gradables (columns) for the current weight (tab)
            List<Gradable> gradables = (from g in db.Gradables
                                        where g.WeightID == currentTab.ID
                                        select g).ToList();

            //pull the students in the course.  Each student is a row.
            List<UserProfile> students = (from up in db.UserProfiles
                                          join cu in db.CoursesUsers on up.ID equals cu.UserProfileID
                                          where cu.CourseID == currentCourseId && cu.CourseRoleID == (int)CourseRole.OSBLERoles.Student
                                          orderby up.LastName, up.FirstName
                                          select up).ToList();

            //Finally the scores for each student.
            List<Score> scor = (from score in db.Scores
                                where score.AssignmentActivity.AbstractAssignment.CategoryID == currentTab.ID
                                select score).ToList();

            var userScore = from scores in db.Scores
                            join user in db.UserProfiles on scores.UserProfileID equals user.ID
                            where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                            && scores.isDropped == false
                            && scores.Points >= 0
                            group scores by scores.UserProfileID into userScores
                            select userScores;

            if (userScore.Count() > 0)
            {
                for (int i = 0; i < userScore.Count(); i++)
                {
                    double currentPoints = 0;
                    double currentTotal = 0;
                    int currentUser = 0;
                    var item = userScore.AsEnumerable().ElementAt(i);
                    foreach (Score a in item)
                    {
                        currentUser = a.UserProfileID;
                        currentPoints += a.Points;
                        currentTotal += a.AssignmentActivity.AbstractAssignment.PointsPossible;
                    }
                    Score newscore = new Score()
                    {
                        UserProfileID = currentUser,
                        Points = ((currentPoints / currentTotal) * 100),
                        isDropped = false
                    };
                    studentScores.Add(newscore);
                }
            }


            //save everything that we need to the viewebag
            ViewBag.Categories = allCategories;
            ViewBag.Grades = scor;
            ViewBag.Assignments = assignments;
            ViewBag.GradeAssignments = gradeAssignments;
            ViewBag.Users = students;
            ViewBag.Percents = studentScores;
        }

        /// <summary>
        /// This will initialize the page using the first weightId found in the database.  If
        /// No weightId exists, a new one will be created
        /// </summary>
        /// <returns></returns>
        private int GetDefaultWeightId()
        {
            //LINQ complains when we use this directly in our queries, so pull it beforehand
            int currentCourseId = ActiveCourse.CourseID;

            //By default, select the first tab
            var categoryQuery = from category in db.Categories
                              where category.CourseID == currentCourseId
                              orderby category.ColumnOrder ascending
                              select category;

            //if something was found, complete the query and get the first weight listed
            if (categoryQuery.Count() > 0)
            {
                Category category = categoryQuery.First();
                return category.ID;
            }
            //ELSE: create a new tab and an anitial gradable
            else
            {
                Category newCategory = new Category()
                {
                    Name = "Category1",
                    CourseID = currentCourseId,
                    Course = ViewBag.ActiveCourse.Course,
                    Points = 0,
                    ColumnOrder = 0,
                    Assignments = new List<AbstractAssignment>()
                };
                db.Categories.Add(newCategory);
                db.SaveChanges();

                GradeAssignment newAssignment = new GradeAssignment()
                {
                    Name = "Untitled",
                    PossiblePoints = 100,
                    GradableScores = new List<GradableScore>(),
                    WeightID = newWeight.ID,
                    Position = 0
                };
                db.AssignmentActivities.Add(newActivity);
                db.SaveChanges();

                //with a new weight / gradable combo created, we can call Index() to finish
                //off the rendering
                return newCategory.ID;
            }
        }
    }
}
