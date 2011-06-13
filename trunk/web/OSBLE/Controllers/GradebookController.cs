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

            AssignmentActivity newActivity = new GradeActivity()
            {
                AbstractAssignmentID = newAssignment.ID,
                AbstractAssignment = newAssignment

            };
            db.AssignmentActivities.Add(newActivity);
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

        public void ClearAllDropLowest(int categoryId)
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
        }

        [HttpPost]
        public void DropLowest(int categoryId, int userId)
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
            BuildGradebook(categoryId);
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
                    item.Points = 0;
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
            return RedirectToAction("Index");
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
            return RedirectToAction("Index");
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
            return Json("success");
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
                return Json("success");
            //return RedirectToAction("Index");
        }
        

        [HttpPost]
        public ActionResult AddCategory()
        {
            var currentCourseId = ActiveCourse.CourseID;
            Category newCategory = new Category()
            {
                Name = "Untitled",
                CourseID = currentCourseId,
                Course = ViewBag.ActiveCourse.Course,
                Points = 0,
                ColumnOrder = 0,
                Assignments = new List<AbstractAssignment>()
            };
            db.Categories.Add(newCategory);
            db.SaveChanges();
            BuildGradebook(newCategory.ID);
            return View("_Tabs");
        }


        [HttpPost]
        public ActionResult AddPoints(int assignmentId, double number)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId > 0)
                {
                    List<Score> grades = (from grade in db.Scores
                                          where grade.AssignmentActivity.AbstractAssignmentID == assignmentId
                                          select grade).ToList();

                    if (grades.Count() > 0)
                    {
                        foreach (Score item in grades)
                        {
                            item.Points += number;
                        }
                        db.SaveChanges();
                    }
                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }
        

        [HttpPost]
        public ActionResult ModifyCell(double value, int userId, int assignmentId)
        {

            //Continue if we have a valid gradable ID
            if (assignmentId != 0)
            {
                var gradableQuery = from g in db.Scores
                                    where g.UserProfileID == userId &&
                                    g.AssignmentActivityID == assignmentId
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
                        Points = value,
                        AssignmentActivityID = assignmentId,
                        PublishedDate = DateTime.Now,
                        isDropped = false
                    };

                    db.Scores.Add(newScore);
                    db.SaveChanges();
                }
            }

            return View();
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
         

        /// <summary>
        /// This will initialize the page using the supplied weightId (tab).
        /// </summary>
        /// <param name="weightId">The weight to load</param>
        /// <returns></returns>
        public ActionResult Index()
        {
            //LINQ complains when we use this directly in our queries, so pull it beforehand
            int currentCourseId = ActiveCourse.CourseID;
            List<Score> percentList = new List<Score>();

            //pull the students in the course.  Each student is a row.
            List<UserProfile> studentList = (from up in db.UserProfiles
                                             join cu in db.CoursesUsers on up.ID equals cu.UserProfileID
                                             where cu.CourseID == currentCourseId && cu.CourseRoleID == (int)CourseRole.OSBLERoles.Student
                                             orderby up.LastName, up.FirstName
                                             select up).ToList();

            var mainScores =    from score in db.Scores
                                join category in db.Categories on score.AssignmentActivity.AbstractAssignment.CategoryID equals category.ID
                                where score.AssignmentActivity.AbstractAssignment.Category.CourseID == currentCourseId &&
                                score.AssignmentActivity.AbstractAssignment.CategoryID == category.ID &&
                                score.isDropped == false
                                group score by score.AssignmentActivity.AbstractAssignment.CategoryID into assignmentScores
                                select new
                                {
                                    AssignmentId = assignmentScores.Key,
                                    StudentScores =
                                                    from stu in assignmentScores
                                                    group stu by stu.UserProfileID into students
                                                    select new
                                                    {
                                                        StudentId = students.Key,
                                                        Score = students.Sum(stu => stu.Points),
                                                        perfectScore = students.Sum(stu => stu.AssignmentActivity.AbstractAssignment.PointsPossible),
                                                        category = students.Select(stu => stu.AssignmentActivity.AbstractAssignment.Category).FirstOrDefault(),
                                                        activity = students.Select(stu => stu.AssignmentActivity).FirstOrDefault()
                                                    }
                                };

            foreach (var item in mainScores)
            {
                for (int i = 0; i < item.StudentScores.Count(); i++ )
                {
                    var scores = item.StudentScores.ElementAt(i);
                
                    Score studentScore = new Score()
                    {
                        AssignmentActivity = scores.activity,
                        UserProfileID = scores.StudentId,
                        Points = ((scores.Score / scores.perfectScore)*100),
                        isDropped = false
                    };


                    GradeAssignment newGA = new GradeAssignment()
                    {
                        ID = item.AssignmentId,
                        Category = scores.category,
                        CategoryID = scores.category.ID,
                        PointsPossible = scores.perfectScore
                    };
                    studentScore.AssignmentActivity.AbstractAssignment = newGA;
                    percentList.Add(studentScore);
                }
            }

            List<CoursesUsers> courseUsers = (from users in db.CoursesUsers
                                              where users.CourseID == currentCourseId
                                              select users).ToList();
                      
            List<Category> categories = (from category in db.Categories
                                         where category.CourseID == currentCourseId
                                         select category).ToList();

            List<GradeAssignment> gradeAssignments = (from ga in db.GradeAssignments
                                                      where ga.Category.CourseID == currentCourseId
                                                      select ga).ToList();

            ViewBag.Students = studentList;
            ViewBag.Scores = percentList;
            ViewBag.Categories = categories;
            ViewBag.CoursesUser = courseUsers;
            ViewBag.GradeAssignments = gradeAssignments;

            return View();
        }

        /// <summary>
        /// Switches to one of the tabs (Categories) to display the assignments
        /// </summary>
        /// <param name="categoryId">We need to know which tab we are going too</param>
        /// <returns></returns>
        public ActionResult Tab(int? categoryId)
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
            var cats = from category in db.Categories
                             where category.CourseID == currentCourseId
                             select category;

            List<Category> allCategories = cats.ToList();

            Category currentTab = (from c in allCategories where c.ID == categoryId select c).First();

            //save to the session.  Needed later for AJAX-related updates.
            Session["CurrentCategoryId"] = currentTab.ID;

            //pull the gradables (columns) for the current weight (tab)
            List<AbstractAssignment> assignments = (from a in db.AbstractAssignments
                                                    where a.CategoryID == currentTab.ID
                                                    select a).ToList();

            if (assignments.Count() == 0)
            {
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

                AssignmentActivity newActivity = new GradeActivity()
                {
                    AbstractAssignmentID = newAssignment.ID,
                    /*PointsPossible = newAssignment.PointsPossible,
                    ColumnOrder = 0,
                    Scores = new List<Score>()*/
                };
                db.AssignmentActivities.Add(newActivity);
                db.SaveChanges();
            }

            //Pull the gradeAssignments
            List<GradeAssignment> gradeAssignments = (from ga in db.GradeAssignments
                                                      where ga.CategoryID == currentTab.ID
                                                      select ga).ToList();

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
                    PointsPossible = 100,
                    AssignmentActivities = new List<AssignmentActivity>(),
                    CategoryID = newCategory.ID,
                    ColumnOrder = 0
                };
                db.GradeAssignments.Add(newAssignment);
                db.SaveChanges();

                AssignmentActivity newActivity = new GradeActivity()
                {
                    AbstractAssignmentID = newAssignment.ID
                    /*PointsPossible = newAssignment.PointsPossible,
                    ColumnOrder = 0,
                    Scores = new List<Score>()*/
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
