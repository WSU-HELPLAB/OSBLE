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
using System.Web;
using OSBLE.Utility;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.ViewModels;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class GradebookController : OSBLEController
    {
        public enum ColumnAction { InsertLeft, InsertRight, Delete, Clear, ImportCSV, NoAction };
        public int colorCount { get; set; }

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
        /// 


        [HttpPost]
        [CanModifyCourse]
        public ActionResult ImportColumnsFromCSV(HttpPostedFileBase file)
        {
            Session["GradeFile"] = file;
            Session["radio"] = Request.Params["rdio"];
            Session["assignmentId"] = Request.Params["assignmentColumnId"];

            List<string[]> parsedData = new List<string[]>();
            List<int> positionList = new List<int>();
            List<int> doNotAdd = new List<int>();
            int currentCourseId = ActiveCourse.AbstractCourseID;

            var students = from student in db.CourseUsers
                           where student.AbstractCourseID == currentCourseId
                           group student by student.UserProfile.ID into studentList
                           select studentList;

            StreamReader readFile = new StreamReader(file.InputStream);

            string line;
            string[] row;

            while ((line = readFile.ReadLine()) != null)
            {
                row = line.Split(',');
                parsedData.Add(row);
                ViewBag.Headers = parsedData.ElementAt(0);
            }

            file.InputStream.Seek(0, SeekOrigin.Begin);
            file.InputStream.Position = 0;

            ViewBag.File = file.FileName;

            return View();
        }

        [HttpPost]
        public ActionResult ApplyAssignments(string idColumn, string assignmentColumn)
        {
            List<string[]> parsedData = new List<string[]>();
            string[] assignments = assignmentColumn.Split(',');
            int studentPosition = 0;
            List<int> index = new List<int>();
            List<int> positionList = new List<int>();
            string studentId = (0).ToString();
            int categoryId = Convert.ToInt32(Session["CurrentCategoryID"]);

            HttpPostedFileBase file = Session["GradeFile"] as HttpPostedFileBase;

            StreamReader sr = new StreamReader(file.InputStream);

            string line;
            string[] row;

            while ((line = sr.ReadLine()) != null)
            {
                row = line.Split(',');
                parsedData.Add(row);
            }

            if (parsedData.Count > 0)
            {
                for (int i = 0; i < parsedData.Count(); i++)
                {
                    int currentAssignmentID = Convert.ToInt32(Session["assignmentId"]); ;
                    int assignmentNumber = 1;
                    int count = 0;
                    int currentColOrder = 0;
                    var item = parsedData.ElementAt(i);
                    foreach (var assignment in item)
                    {
                        if (i == 0)
                        {
                            if (assignments.Contains(assignment) || assignment == idColumn)
                            {
                                if (assignment == idColumn)
                                {
                                    studentPosition = count;
                                }
                                else
                                {
                                    AbstractAssignmentActivity assign = (from g in db.AbstractAssignmentActivities where g.ID == currentAssignmentID select g).FirstOrDefault();
                                    if (Session["radio"].ToString() == "r")
                                    {

                                        var position = (from pos in db.AbstractAssignmentActivities
                                                        where pos.AbstractAssignment.CategoryID == categoryId &&
                                                        pos.ColumnOrder > assign.ColumnOrder
                                                        orderby pos.ColumnOrder
                                                        select pos);

                                        if (position.FirstOrDefault() != null)
                                        {
                                            AddColumn(assignment.ToString(), 10, assign.ColumnOrder + assignmentNumber);
                                            positionList.Add(assign.ColumnOrder + assignmentNumber);
                                            index.Add(count);
                                        }
                                        else
                                        {
                                            AddColumn(assignment.ToString(), 10, assign.ColumnOrder + assignmentNumber);
                                            positionList.Add(assign.ColumnOrder + assignmentNumber);
                                            index.Add(count);
                                        }
                                        assignmentNumber++;

                                    }
                                    else if (Session["radio"].ToString() == "l")
                                    {

                                        var position = from pos in db.AbstractAssignmentActivities
                                                       where pos.AbstractAssignment.CategoryID == categoryId &&
                                                       pos.ColumnOrder >= assign.ColumnOrder
                                                       orderby pos.ColumnOrder
                                                       select pos;

                                        if (position.Count() > 0)
                                        {
                                            AddColumn(assignment.ToString(), 10, assign.ColumnOrder);
                                            positionList.Add(assign.ColumnOrder - 1);
                                            index.Add(count);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (count == studentPosition)
                            {
                                studentId = assignment;
                            }
                            else if (index.Contains(count))
                            {
                                var student = from stu in db.CourseUsers
                                              where stu.UserProfile.Identification == studentId
                                              select stu;

                                if (student.Count() > 0)
                                {
                                    int col = positionList.ElementAt(currentColOrder);
                                    currentColOrder++;
                                    
                                    var assignmentQuery = from a in db.AbstractAssignmentActivities
                                                          where a.AbstractAssignment.CategoryID == categoryId &&
                                                          a.ColumnOrder == col
                                                          select a;

                                    var currentAssignment = assignmentQuery.FirstOrDefault();
                                    double categoryMaxPoints = db.Categories.Find(categoryId).MaxAssignmentScore;

                                    UserProfile user = (from u in db.UserProfiles
                                                        where u.Identification == studentId
                                                        select u).FirstOrDefault();

                                    if (assignmentQuery.Count() > 0)
                                    {
                                        if (user != null)
                                        {
                                            double points = Convert.ToDouble(assignment);
                                            double rawPoints = points;
                                            var teamuser = from c in currentAssignment.TeamUsers where c.Contains(user) select c;
                                            if (categoryMaxPoints >= 0)
                                            {
                                                if (((points/currentAssignment.PointsPossible)*100) > categoryMaxPoints)
                                                {
                                                    points = (currentAssignment.PointsPossible * (categoryMaxPoints / 100));
                                                }
                                            }
                                            if (teamuser.Count() > 0)
                                            {
                                                Score newScore = new Score()
                                                {
                                                    TeamUserMember = teamuser.First(),
                                                    Points = points,
                                                    AssignmentActivityID = currentAssignment.ID,
                                                    PublishedDate = DateTime.Now,
                                                    isDropped = false,
                                                    StudentPoints = -1,
                                                    RawPoints = rawPoints
                                                };
                                                db.Scores.Add(newScore);
                                                db.SaveChanges();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        count++;
                    }
                }

            }
            return RedirectToAction("Tab", "Gradebook", new { categoryId = (int)Session["categoryId"] });
        }

        //[HttpPost]
        public ActionResult ExportToCSV()
        {
            int currentCourseId = activeCourse.AbstractCourseID;

            //overall string
            string finalString = "";

            //Stores the line for weights
            string weights = "";

            //Stores the line for perfect score
            string perfect = "";

            //Stores the line for average score
            string average = "";

            //Stores the line for the header
            string header = "";

            //Stores all the lines for the table
            List<string> table = new List<string>();

            //Store an individual line for the table
            string tableLine = "";

            string letterGrade = "";

            //Empty holder for section column
            weights += "";
            perfect += "";
            average += "";
            header += "Section";

            //Category Headers for weight and perfect
            weights += ",,Weight";
            perfect += ",,Perfect Score";
            average += ",,Average Score";
            header += ",Student ID,Name";

            List<LetterGrade> letterGrades = ((activeCourse.AbstractCourse as Course).LetterGrades).OrderByDescending(l => l.MinimumRequired).ToList();
            //Grade is empty for weight and holds the Best grade for perfect score
            weights += ",";
            if (letterGrades.Count() > 0)
            {
                perfect += "," + letterGrades.FirstOrDefault().Grade;
            }
            else
            {
                perfect += ",";
            }
            header += ",Grade";

            List<Category> Category = (from category in db.Categories
                                       where category.CourseID == currentCourseId &&
                                       category.Name != Constants.UnGradableCatagory
                                       select category).ToList();

            //stores the total category weight
            double totalCategoryWeights = 0;
            foreach (Category category in Category)
            {
                totalCategoryWeights += category.Points;
            }

            //Adds the total weight and 100% for perfect score
            weights += "," + totalCategoryWeights.ToString();
            perfect += ",100%";
            header += ",Total Grade";

            foreach (Category category in Category)
            {
                weights += "," + category.Points.ToString();
                perfect += ",100%";
                if (totalCategoryWeights > 0)
                {
                    header += "," + category.Name.ToUpper() + " " + "(" + ((category.Points / totalCategoryWeights) * 100) + "%)";
                }
                else
                {
                    header += "," + category.Name.ToUpper();
                }

                List<AbstractAssignmentActivity> Assignments = (from assignment in db.AbstractAssignmentActivities
                                                                where assignment.AbstractAssignment.CategoryID == category.ID &&
                                                                (!(assignment is StopActivity))
                                                                select assignment).ToList();

                foreach (AbstractAssignmentActivity assignment in Assignments)
                {
                    weights += ",";
                    perfect += "," + assignment.PointsPossible.ToString();
                    header += "," + assignment.Name;
                }
            }

            double totalGrade = 0;
            double totalCategoryPoints = 0;
            double totalCategoryPossible = 0;
            double categoryTotalWeight = 0;
            int studentCount = 0;

            //pull the students in the course.  Each student is a row.
            List<UserProfile> students = (from up in db.UserProfiles
                                          join cu in db.CourseUsers on up.ID equals cu.UserProfileID
                                          where cu.AbstractCourseID == currentCourseId && cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                          orderby up.LastName, up.FirstName
                                          select up).ToList();

            double TotalCategoryWeights = (from cat in db.Categories
                                           where cat.CourseID == currentCourseId &&
                                           cat.Points >= 0
                                           select cat.Points).Sum();

            List<Score> allScores = (from score in db.Scores
                                     where score.AssignmentActivity.AbstractAssignment.Category.CourseID == currentCourseId &&
                                     score.Points >= 0 &&
                                     score.isDropped == false
                                     select score).ToList();

            foreach (UserProfile up in students)
            {
                double studentTotalGrade = 0;
                tableLine = "";
                CourseUsers courseUser = (from course in db.CourseUsers
                                           where course.UserProfileID == up.ID
                                           select course).FirstOrDefault();

                //Adding the section the student is in
                tableLine += courseUser.Section.ToString();

                //Add the name of the student
                tableLine += "," + up.Identification + "," + up.FirstName + " " + up.LastName;

                studentCount++;
                categoryTotalWeight = TotalCategoryWeights;
                foreach (Category category in Category)
                {

                    List<Score> userScore = (from score in allScores
                                             where score.TeamUserMember.Contains(up) &&
                                             score.AssignmentActivity.AbstractAssignment.CategoryID == category.ID
                                             select score).ToList();

                    if (userScore.Count() == 0)
                    {
                        categoryTotalWeight -= category.Points;
                    }
                }
                double studentCategoryPoints = 0;
                double studentCategoryPossible = 0;
                foreach (Category category in Category)
                {
                    double categoryPoints = 0;
                    double categoryPossible = 0;
                    double categoryTotal = 0;
                    //studentCategoryPoints = 0;
                    //studentCategoryPossible = 0;                   

                    List<Score> totalScores = (from score in allScores
                                               where score.TeamUserMember.Contains(up) &&
                                               score.AssignmentActivity.AbstractAssignment.CategoryID == category.ID
                                               select score).ToList();
                    if (totalScores.Count() > 0)
                    {
                        foreach (Score score in totalScores)
                        {
                            if (score.AssignmentActivity.AbstractAssignment.CategoryID == category.ID)
                            {
                                categoryPoints += score.Points;
                                categoryPossible += score.AssignmentActivity.PointsPossible;
                            }
                        }
                        categoryTotal = categoryPoints / categoryPossible;
                        if (TotalCategoryWeights > 0)
                        {
                            totalGrade += categoryTotal * (category.Points / categoryTotalWeight) * 100;
                            studentTotalGrade += categoryTotal * (category.Points / categoryTotalWeight) * 100;
                        }
                        else
                        {
                            totalCategoryPoints += categoryPoints;
                            totalCategoryPossible += categoryPossible;
                            studentCategoryPoints += categoryPoints;
                            studentCategoryPossible += categoryPossible;
                        }
                    }
                }
                if (TotalCategoryWeights == 0)
                {
                    studentTotalGrade = (studentCategoryPoints / studentCategoryPossible) * 100;
                }
                if (studentTotalGrade == 0)
                {
                    tableLine += ",,NG";
                }
                else
                {
                    //get the letter grade from the total grade
                    letterGrade = "";
                    if (letterGrades.Count() > 0)
                    {
                        foreach (LetterGrade letter in letterGrades)
                        {
                            if (studentTotalGrade >= letter.MinimumRequired)
                            {
                                letterGrade = letter.Grade;
                                break;
                            }
                        }
                    }
                    tableLine += "," + letterGrade;
                    tableLine += "," + studentTotalGrade.ToString(".#") + "%";
                }

                foreach (Category category in Category)
                {
                    double categoryPoints = 0;
                    double categoryPossible = 0;
                    double categoryPercent = 0;
                    List<Score> userScores = (from score in allScores
                                              where score.TeamUserMember.Contains(up) &&
                                              score.AssignmentActivity.AbstractAssignment.CategoryID == category.ID
                                              select score).ToList();

                    foreach (Score score in userScores)
                    {
                        categoryPoints += score.Points;
                        categoryPossible += score.AssignmentActivity.PointsPossible;
                    }
                    categoryPercent = (categoryPoints / categoryPossible) * 100;
                    if (categoryPoints == 0 && categoryPossible == 0)
                    {
                        tableLine += ",NG";
                    }
                    else
                    {
                        tableLine += "," + categoryPercent.ToString(".#") + "%";
                    }

                    List<AbstractAssignmentActivity> Assignments = (from assignment in db.AbstractAssignmentActivities
                                                                    where assignment.AbstractAssignment.CategoryID == category.ID &&
                                                                    (!(assignment is StopActivity))
                                                                    select assignment).ToList();

                    foreach (AbstractAssignmentActivity assignment in Assignments)
                    {
                        Score userScore = (from score in userScores
                                           where score.AssignmentActivityID == assignment.ID
                                           select score).FirstOrDefault();

                        if (userScore != null)
                        {
                            tableLine += "," + userScore.Points.ToString(".#");
                        }
                        else
                        {
                            tableLine += ",NG";
                        }
                    }
                }
                table.Add(tableLine);

            }
            if (TotalCategoryWeights == 0)
            {
                totalGrade = (totalCategoryPoints / totalCategoryPossible) * 100;
            }
            else
            {
                totalGrade = (totalGrade / (studentCount * 100) * 100);
            }

            //get the letter grade from the total grade
            letterGrade = "";
            if (letterGrades.Count() > 0)
            {
                foreach (LetterGrade letter in letterGrades)
                {
                    if (totalGrade >= letter.MinimumRequired)
                    {
                        letterGrade = letter.Grade;
                        break;
                    }
                }
            }

            average += "," + letterGrade;
            //Add the average of the Total Grade
            average += "," + totalGrade.ToString(".#") + "%";

            //Holds the total grade
            totalGrade = 0;

            double assignPoints = 0;
            double assignPossible = 0;

            foreach (Category category in Category)
            {
                List<Score> Scores = (from score in allScores
                                      where score.AssignmentActivity.AbstractAssignment.CategoryID == category.ID
                                      select score).ToList();

                double totalAverage = 0;
                double averagePoints = 0;
                double averagePossible = 0;

                //Make sure there is at least one score in the category
                bool oneScore = false;
                foreach (Score score in Scores)
                {
                    oneScore = true;
                    averagePoints += score.Points;
                    averagePossible += score.AssignmentActivity.PointsPossible;
                }
                double categoryScore = averagePoints / averagePossible;
                totalAverage += categoryScore * 100;
                if (oneScore == true)
                {
                    average += "," + totalAverage.ToString(".#") + "%";
                }
                else
                {
                    average += ",NG";
                }

                List<AbstractAssignmentActivity> Assignments = (from assignment in db.AbstractAssignmentActivities
                                                                where assignment.AbstractAssignment.CategoryID == category.ID &&
                                                                (!(assignment is StopActivity))
                                                                select assignment).ToList();

                foreach (AbstractAssignmentActivity assignment in Assignments)
                {
                    var score = (from scores in allScores
                                 where scores.AssignmentActivityID == assignment.ID
                                 select scores);

                    totalAverage = 0;
                    averagePoints = 0;
                    averagePossible = 0;

                    //Make sure there is at least one score in the category
                    oneScore = false;
                    foreach (Score s in score)
                    {
                        oneScore = true;
                        averagePoints += s.Points;
                        averagePossible += s.AssignmentActivity.PointsPossible;
                    }
                    categoryScore = averagePoints / averagePossible;
                    totalAverage += categoryScore * 100;
                    if (oneScore == true)
                    {
                        average += "," + totalAverage.ToString(".#");
                    }
                    else
                    {
                        average += ",NG";
                    }
                }
            }

            finalString += weights + "\n" + perfect + "\n" + average + "\n" + header + "\n";

            foreach (string s in table)
            {
                finalString += s + "\n";
            }

            ViewBag.Final = finalString;
            context.Response.AppendHeader("Content-Disposition", "attachment; filename=\"Grades.csv\"");

            Response.ContentType = "application/octet-stream";


            return View();
        }


        private void AddColumn(string columnName, int pointsPossible, int position)
        {
            int categoryId = Convert.ToInt32(Session["CurrentCategoryID"]);

            List<TeamUserMember> userMembers = new List<TeamUserMember>();

            int currentCourseId = ActiveCourse.AbstractCourseID;
            List<CourseUsers> Users = (from user in db.CourseUsers
                                        where user.AbstractCourseID == currentCourseId
                                        select user).ToList();

            foreach (CourseUsers u in Users)
            {
                UserMember userMember = new UserMember()
                {
                    UserProfile = u.UserProfile,
                    UserProfileID = u.UserProfileID
                };
                userMembers.Add(userMember);
                db.TeamUsers.Add(userMember);

            }
            db.SaveChanges();

            var allAssignments = from assign in db.AbstractAssignmentActivities
                                 where assign.AbstractAssignment.CategoryID == categoryId &&
                                 assign.ColumnOrder >= position
                                 select assign;

            if (allAssignments.Count() > 0)
            {
                foreach (AbstractAssignmentActivity item in allAssignments)
                {
                    item.ColumnOrder += 1;
                }
            }

            StudioAssignment newAssignment = new StudioAssignment()
            {
                Name = "Untitled",
                //PointsPossible = 100,
                AssignmentActivities = new List<AbstractAssignmentActivity>(),
                CategoryID = categoryId,
                ColumnOrder = position,
                Description = "No description",
                IsDraft = false
            };
            db.StudioAssignments.Add(newAssignment);
            db.SaveChanges();

            GradeActivity newActivity = new GradeActivity()
            {
                AbstractAssignmentID = newAssignment.ID,
                AbstractAssignment = newAssignment,
                Name = columnName,
                PointsPossible = newAssignment.PointsPossible,
                TeamUsers = userMembers,
                ColumnOrder = position
            };
            db.AbstractAssignmentActivities.Add(newActivity);
            db.SaveChanges();
            //Response.ContentType
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
            AbstractAssignmentActivity assignmentActivity = db.AbstractAssignmentActivities.Find(assignmentId);
            StudioAssignment assignment = db.StudioAssignments.Find(assignmentActivity.AbstractAssignmentID);
            List<TeamUserMember> teamMember = (from a in assignmentActivity.TeamUsers select a).ToList();

            var rubricEvals = (from a in db.RubricEvaluations where a.AbstractAssignmentActivityID == assignmentActivity.ID select a);

            //Linq won't let me delete from the iCollection so I made a List<Deliverable> to 
            //store the deliverables and then looping through the list to delete them.
            List<Deliverable> deliverables = new List<Deliverable>();

            foreach (Deliverable d in assignment.Deliverables)
            {
                deliverables.Add(d);
            }

            foreach (Deliverable d in deliverables)
            {
                assignment.Deliverables.Remove(d);
            }
            db.SaveChanges();

            if (rubricEvals.Count() > 0)
            {
                var criterion = (from c in db.Criteria where c.RubricID == assignment.RubricID select c);
                foreach (Criterion c in criterion)
                {
                    db.Criteria.Remove(c);
                }
                db.SaveChanges();

                foreach (RubricEvaluation r in rubricEvals)
                {
                    db.RubricEvaluations.Remove(r);
                }
                db.SaveChanges();
            }
            
            foreach (UserMember item in teamMember)
            {
                db.TeamUsers.Remove(item);
            }
            db.SaveChanges();

            db.AbstractAssignmentActivities.Remove(assignmentActivity);
            db.SaveChanges();

            db.StudioAssignments.Remove(assignment);
            db.SaveChanges();
        }

       [HttpPost]
       public void ClearDropLowest(int categoryId, string userId)
       {
           if (ModelState.IsValid)
           {
               //Get student
               //var user = (from u in db.UserProfiles where u.Identification == userId select u).FirstOrDefault();

               if (categoryId > 0)
               {
                   UserProfile user = (from u in db.UserProfiles
                                       where u.Identification == userId
                                       select u).FirstOrDefault();

                   List<Score> scoreList = (from scores in db.Scores
                                            where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                                            && scores.isDropped == true
                                            select scores).ToList();
                     
                   var studentScores = (from scores in scoreList
                                        where scores.TeamUserMember.Contains(user) &&
                                        scores.isDropped == true
                                        select scores);
                   

                   //var studentScores = from scores in db.Scores
                   //                    where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                   //                    //&& scores.TeamUserMemberID == userId
                   //                    && scores.isDropped == true
                   //                    select scores;

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
            BuildGradebook(categoryId);
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult DropLowest(int categoryId, string userId)
        {
            if (ModelState.IsValid)
            {
                ClearDropLowest(categoryId, userId);
                
                if (categoryId > 0)
                {
                    UserProfile user = (from u in db.UserProfiles
                                        where u.Identification == userId
                                        select u).FirstOrDefault();

                    List<Score> scoreList = (from scores in db.Scores
                                             where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                                             orderby (scores.Points / scores.AssignmentActivity.PointsPossible)
                                             select scores).ToList();

                    var studentScores = (from scores in scoreList
                                         where scores.TeamUserMember.Contains(user)
                                         select scores);

                    Category currentCategory = (from cat in db.Categories where cat.ID == categoryId select cat).FirstOrDefault();

                    if (studentScores.Count() > 0)
                    {    
                        List<Score> scores = studentScores.ToList();
                        
                        var max = 0;
                        int dropX = currentCategory.dropX;
                        
                        if (currentCategory.Customize == (int)Category.GradeOptions.XtoTake)
                        {
                            max = scores.Count() - dropX;
                        }
                        else
                        {
                            max = dropX;
                        }
                        for (int i = 0; i < max; i++)
                        {
                            //if there are less assignments than you want to drop
                            // changed so that it will always show one grade per Chris
                            if (i < scores.Count() - 1)
                            {
                                scores[i].isDropped = true;
                            }
                        }
                        db.SaveChanges();
                    }
                }
            }
            BuildGradebook(categoryId);
            return View("_Gradebook");
        }


        [HttpPost]
        public ActionResult AllDropLowest(int categoryId, int dropX, string customize)
        {
            if (ModelState.IsValid)
            {
                //storing the amount of assignments wanted to drop
                var currentCatagory = (from cat in db.Categories where cat.ID == categoryId select cat).FirstOrDefault();
                currentCatagory.dropX = dropX;
                db.SaveChanges();

                switch (customize)
                {
                    case "CompAverage":
                        currentCatagory.Customize = (int)Category.GradeOptions.CompAverage;
                        db.SaveChanges();
                        break;
                    case "XtoDrop":
                        currentCatagory.Customize = (int)Category.GradeOptions.XtoDrop;
                        db.SaveChanges();
                        break;
                    case "XtoTake":
                        currentCatagory.Customize = (int)Category.GradeOptions.XtoTake;
                        db.SaveChanges();
                        break;
                    default:
                        break;
                };

                if (customize != "CompAverage")
                {
                    if (categoryId > 0)
                    {
                        List<Score> scoreList = (from s in db.Scores
                                                 where s.AssignmentActivity.AbstractAssignment.CategoryID == categoryId &&
                                                 s.Points >= 0
                                                 select s).ToList();

                        var studentScores = (from scores in scoreList
                                             orderby scores.Points / scores.AssignmentActivity.PointsPossible
                                             select scores).GroupBy(s => s.TeamUserMember.Name);

                        if (studentScores.Count() > 0)
                        {
                            for (int i = 0; i < studentScores.Count(); i++)
                            {
                                List<Score> scores = studentScores.AsEnumerable().ElementAt(i).ToList();
                                var max = dropX;
                                if (customize == "XtoTake")
                                {
                                    max = scores.Count() - dropX;
                                }

                                for (int j = 0; j < max; j++)
                                {                                    
                                    if (j < scores.Count() - 1)
                                    {
                                        scores[j].isDropped = true;
                                    }
                                }
                                db.SaveChanges();
                            }
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
                                  where s.AssignmentActivityID == assignmentId
                                  select s;

            if (assignmentQuery.Count() > 0)
            {
                foreach (Score item in assignmentQuery)
                {
                    item.Points = -1;
                    item.AssignmentActivity.PointsPossible = 0;
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
            else if (string.Compare(ColumnAction.ImportCSV.ToString(), action) == 0)
            {
                return ColumnAction.ImportCSV;
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
                    Category category = (from c in db.Categories
                                         where c.ID == categoryId
                                         select c).FirstOrDefault();

                    if (category != null)
                    {
                        category.Points = value;
                        db.SaveChanges();
                    }

                }
            }
            TeacherIndex();
            return View("_Tabs");
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
            TeacherIndex();
            //ViewBag.Categories = (activeCourse.AbstractCourse as Course).Categories;
            return View("_Tabs");
        }

        [HttpPost]
        public ActionResult ModifyAssignmentName(string value, int assignmentId)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId != 0)
                {
                    var assignmentQuery = from a in db.AbstractAssignmentActivities
                                          where a.ID == assignmentId
                                          select a;

                    if (assignmentQuery.Count() > 0)
                    {
                        foreach (AbstractAssignmentActivity item in assignmentQuery)
                        {
                            item.Name = value;
                        }
                        db.SaveChanges();
                    }
                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }


        [HttpPost]
        public ActionResult ModifyPossiblePoints(int value, int assignmentId)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId != 0)
                {
                    var activityQuery = from a in db.AbstractAssignmentActivities
                                        where a.ID == assignmentId
                                        select a;

                    if (activityQuery.Count() > 0)
                    {
                        foreach (AbstractAssignmentActivity item in activityQuery)
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
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
            //return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult AddCategory()
        {
            var currentCourseId = ActiveCourse.AbstractCourseID;
            var numTabs = from cats in db.Categories
                          where cats.CourseID == currentCourseId
                          orderby cats.ColumnOrder descending
                          select cats;
            int colorCount = numTabs.Count();
            string color = null;
            string name = null;
            switch (colorCount)
           { 
                case 1:
                    color = "#74FEF8";
                    name = "Category 1";
                    break;
                case 2:
                    color = "Tomato";
                    name = "Category 2";
                    break;
                case 3:
                    color = "Plum";
                    name = "Category 3";
                    break;
                case 4:
                    color = "SpringGreen";
                    name = "Category 4";
                    break;
                case 5:
                    color = "BurlyWood";
                    name = "Category 5";
                    break;
                case 6:
                    color = "Yellow";
                    name = "Category 6";
                    break;
                case 7:
                    color = "#B3BE53";
                    name = "Category 7";
                    break;
                case 8:
                    color = "Orange";
                    name = "Category 8";
                    break;
                case 9:
                    color = "Pink";
                    name = "Category 9";
                    break;
                case 10:
                    color = "#13BE00";
                    name = "Category 10";
                    break;
                default:
                    color = "#74FEF8";
                    name = "Category";
                    break;
            };

            Category newCategory = new Category()
            {
                Name = name,
                CourseID = currentCourseId,
                Points = 0,
                ColumnOrder = numTabs.First().ColumnOrder + 1,
                Assignments = new List<AbstractAssignment>(),
                TabColor = color
            };

            db.Categories.Add(newCategory);
            db.SaveChanges();
            TeacherIndex();
            return View("Index");
        }
        
        [HttpPost]
        public void DeleteCategory(int categoryId)
        {
            if (ModelState.IsValid)
            {
                int currentCourseId = ActiveCourse.AbstractCourseID;
                List<AbstractAssignmentActivity> activities = (from a in db.AbstractAssignmentActivities 
                                                               where a.AbstractAssignment.CategoryID == categoryId
                                                               select a).ToList();
                for (int i = 0; i < activities.Count(); i++)
                {
                    List<TeamUserMember> teamMember = (from a in activities[i].TeamUsers select a).ToList();
                    foreach (TeamUserMember item in teamMember)
                    {
                        if (item is UserMember)
                        {
                            db.TeamUsers.Remove(item);
                        }
                        else if (item is TeamUserMember)
                        {
                            db.TeamUsers.Remove(item);
                        }
                    }
                    db.SaveChanges();
                }                    

                Category category = db.Categories.Find(categoryId);
                db.Categories.Remove(category);
                db.SaveChanges();
            }
        }

        [CanModifyCourse]
        [HttpPost]
        public ActionResult ChangeTabColor(int categoryId, string value)
        {
            if (ModelState.IsValid)
            {
                if (categoryId > 0)
                {
                    var categoryTab = from tab in db.Categories
                                      where tab.ID == categoryId
                                      select tab;

                    foreach (Category item in categoryTab)
                    {
                        item.TabColor = value;
                    }
                    db.SaveChanges();
                }
            }
            TeacherIndex();
            return View("Index");
        }


        [HttpPost]
        public ActionResult AddPoints(int assignmentId, double number)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId > 0)
                {
                    List<Score> grades = (from grade in db.Scores
                                          where grade.AssignmentActivityID == assignmentId &&
                                          grade.Points >= 0 
                                          select grade).ToList();

                    var assignment = (from assigns in db.AbstractAssignmentActivities
                                      where assigns.ID == assignmentId
                                      orderby assigns.ColumnOrder
                                      select assigns).FirstOrDefault();

                    if (grades.Count() > 0)
                    {
                        foreach (Score item in grades)
                        {
                            if (item.AddedPoints > 0)
                            {
                                item.Points -= assignment.addedPoints;
                            }
                            item.Points += number;
                            item.AddedPoints = number;
                        }
                        assignment.addedPoints = number;
                        db.SaveChanges();
                    }
                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult ChangeCategoryName(int assignmentId, string categoryName)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId != 0)
                {
                    int currentCourseId = ActiveCourse.AbstractCourseID;

                    AbstractAssignmentActivity assignment = (from assign in db.AbstractAssignmentActivities
                                                             where assign.ID == assignmentId
                                                             select assign).FirstOrDefault();

                    Category category = (from cat in db.Categories
                                         where cat.Name == categoryName && cat.CourseID == currentCourseId
                                         select cat).FirstOrDefault();

                    int newCategoryLastAssignment = (from a in db.AbstractAssignmentActivities
                                                     where a.AbstractAssignment.CategoryID == category.ID
                                                     orderby a.ColumnOrder descending
                                                     select a.ColumnOrder).FirstOrDefault();

                    if (assignment != null && category != null)
                    {
                        assignment.AbstractAssignment.CategoryID = category.ID;
                        assignment.ColumnOrder = newCategoryLastAssignment + 1;
                        db.SaveChanges();
                    }
                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult MoveRight(int assignmentId)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId > 0)
                {
                    int categoryId = (int)Session["categoryId"];

                    var assignmentColPosition = from col in db.AbstractAssignmentActivities
                                                where col.ID == assignmentId
                                                && col.AbstractAssignment.CategoryID == categoryId
                                                select col;

                    if (assignmentColPosition.Count() > 0)
                    {
                        int nextCol = Convert.ToInt32(assignmentColPosition.First().ColumnOrder) + 1;

                        var nextAssignmentColPosition = from next in db.AbstractAssignmentActivities
                                                        where next.ColumnOrder >= nextCol &&
                                                        next.AbstractAssignment.CategoryID == categoryId
                                                        orderby next.ColumnOrder
                                                        select next;

                        if (nextAssignmentColPosition.Count() > 0)
                        {
                            int tempCol = assignmentColPosition.First().ColumnOrder;
                            assignmentColPosition.First().ColumnOrder = nextAssignmentColPosition.First().ColumnOrder;
                            nextAssignmentColPosition.First().ColumnOrder = tempCol;

                            db.SaveChanges();
                        }
                    }
                }
            }

            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult MoveLeft(int assignmentId)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId > 0)
                {
                    int categoryId = (int)Session["categoryId"];

                    var assignmentColPosition = from col in db.AbstractAssignmentActivities
                                                where col.ID == assignmentId &&
                                                col.AbstractAssignment.CategoryID == categoryId
                                                select col;
                    if (assignmentColPosition.Count() > 0)
                    {
                        int prevCol = Convert.ToInt32(assignmentColPosition.First().ColumnOrder) - 1;

                        var prevAssignmentColPosition = from next in db.AbstractAssignmentActivities
                                                        where next.ColumnOrder <= prevCol &&
                                                        next.AbstractAssignment.CategoryID == categoryId
                                                        orderby next.ColumnOrder descending
                                                        select next;

                        if (prevAssignmentColPosition.Count() > 0)
                        {
                            int tempCol = assignmentColPosition.First().ColumnOrder;
                            assignmentColPosition.First().ColumnOrder = prevAssignmentColPosition.First().ColumnOrder;
                            prevAssignmentColPosition.First().ColumnOrder = tempCol;

                            db.SaveChanges();
                        }
                    }

                }
            }

            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult MoveCategoryRight(int categoryId)
        {
            if (ModelState.IsValid)
            {
                if (categoryId > 0)
                {
                    var categoryColPosition = from col in db.Categories
                                              where col.ID == categoryId
                                              select col;
                    if (categoryColPosition.Count() > 0)
                    {
                        int nextCol = Convert.ToInt32(categoryColPosition.First().ColumnOrder) + 1;
                        int courseId = Convert.ToInt32(categoryColPosition.First().CourseID);

                        var nextCategoryColPosition = from next in db.Categories
                                                      where next.ColumnOrder >= nextCol &&
                                                      next.CourseID == courseId
                                                      orderby next.ColumnOrder
                                                      select next;

                        if (nextCategoryColPosition.Count() > 0)
                        {
                            int tempCol = categoryColPosition.First().ColumnOrder;
                            categoryColPosition.First().ColumnOrder = nextCategoryColPosition.First().ColumnOrder;
                            nextCategoryColPosition.First().ColumnOrder = tempCol;

                            db.SaveChanges();
                        }
                    }
                }
            }
            TeacherIndex();
            return View("Index");
        }

        [HttpPost]
        public ActionResult MoveCategoryLeft(int categoryId)
        {
            if (ModelState.IsValid)
            {
                if (categoryId > 0)
                {
                    var categoryColPosition = from col in db.Categories
                                              where col.ID == categoryId
                                              select col;
                    if (categoryColPosition.Count() > 0)
                    {
                        int prevCol = Convert.ToInt32(categoryColPosition.First().ColumnOrder) - 1;
                        int courseId = Convert.ToInt32(categoryColPosition.First().CourseID);

                        var prevCategoryColPosition = from next in db.Categories
                                                      where next.ColumnOrder <= prevCol &&
                                                      next.CourseID == courseId &&
                                                      next.Name != Constants.UnGradableCatagory
                                                      orderby next.ColumnOrder descending
                                                      select next;

                        if (prevCategoryColPosition.Count() > 0)
                        {
                            int tempCol = categoryColPosition.First().ColumnOrder;
                            categoryColPosition.First().ColumnOrder = prevCategoryColPosition.First().ColumnOrder;
                            prevCategoryColPosition.First().ColumnOrder = tempCol;

                            db.SaveChanges();
                        }
                    }
                }
            }
            TeacherIndex();
            return View("Index");
        }
        [HttpPost]
        public ActionResult ModifyMaxPoints(double value)
        {
            if (ModelState.IsValid)
            {
                int currentCategoryId = (int)Session["categoryId"];
                Category currentCategory = db.Categories.Find(currentCategoryId);

                currentCategory.MaxAssignmentScore = value;
                db.SaveChanges();

                List<Score> scores = (from score in db.Scores
                                      where score.AssignmentActivity.AbstractAssignment.CategoryID == currentCategoryId
                                      select score).ToList();

                if (scores.Count() > 0)
                {
                    //First, we need to set the scores back to the raw total to do the new calculations.
                    foreach (Score score in scores)
                    {
                        score.Points = score.RawPoints;
                    }
                    db.SaveChanges();

                    //If the values percent is greater than the maximum percent, we need to set the value to the 
                    //maximum percent. We give the score the total points possible for the assignment 
                    //multiplied by the (max value / 100). This will give us the total percent for the assignment
                    //the student can receive. We need to check both points and student points.
                    foreach (Score score in scores)
                    {
                        if (((score.Points/score.AssignmentActivity.PointsPossible)*100) > value)
                        {
                            score.Points = (score.AssignmentActivity.PointsPossible * (value / 100));
                        }
                        if (((score.StudentPoints/score.AssignmentActivity.PointsPossible)*100) > value)
                        {
                            score.StudentPoints = (score.AssignmentActivity.PointsPossible * (value / 100));
                        }
                    }
                    db.SaveChanges();
                }
            }
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult RemoveModifyMaxPoints()
        {
            if (ModelState.IsValid)
            {
                //Get the current category
                int currentCategoryId = (int)Session["categoryId"];
                Category currentCategory = db.Categories.Find(currentCategoryId);

                //Set the current categories max assignment score to -1
                if (currentCategory.MaxAssignmentScore > -1)
                {
                    currentCategory.MaxAssignmentScore = -1;
                    db.SaveChanges();

                    List<Score> scores = (from score in db.Scores
                                          where score.AssignmentActivity.AbstractAssignment.CategoryID == currentCategoryId
                                          select score).ToList();

                    //If there are scores, set the student scores to their raw scores and 
                    //save the database.
                    if (scores.Count() > 0)
                    {
                        foreach (Score score in scores)
                        {
                            score.Points = score.RawPoints;
                        }
                        db.SaveChanges();
                    }
                }
                else
                {
                }
            }
            return View("_Gradebook");
        }

        [HttpPost]
        public ActionResult ModifyCell(double value, string userId, int assignmentId)
        {

            //Continue if we have a valid gradable ID
            if (assignmentId != 0)
            {
                double latePenalty = 0.0;
                //Get student
                var user = (from u in db.UserProfiles where u.Identification == userId select u).FirstOrDefault();

                if (user != null)
                {
                    double rawValue = value;
                    List<Score> gradableQuery = (from g in db.Scores
                                                 where g.AssignmentActivityID == assignmentId
                                                 select g).ToList();

                    Score grades = (from grade in gradableQuery
                                    where grade.TeamUserMember.Contains(user)
                                    select grade).FirstOrDefault();

                    var assignmentQuery = from a in db.AbstractAssignmentActivities
                                          where a.ID == assignmentId
                                          select a;

                    var currentAssignment = assignmentQuery.FirstOrDefault();
                    var teamuser = from c in currentAssignment.TeamUsers where c.Contains(user) select c;
                    Category currentCategory = currentAssignment.AbstractAssignment.Category;

                    if (grades != null)
                    {
                        TimeSpan? lateness = calculateLateness(currentAssignment.AbstractAssignment.Category.Course, currentAssignment, teamuser.First());
                        if (lateness != null)
                        {
                            latePenalty = CalcualateLatePenaltyPercent(currentAssignment, (TimeSpan)lateness);
                            latePenalty = (100 - latePenalty) / 100;
                            value = value * latePenalty;
                        }

                        if (currentCategory.MaxAssignmentScore >= 0)
                        {
                            if (((value/grades.AssignmentActivity.PointsPossible)*100) > currentCategory.MaxAssignmentScore)
                            {
                                value = (currentAssignment.PointsPossible * (currentCategory.MaxAssignmentScore / 100));
                            }
                        }

                        if (grades.Points == value)
                        {
                            //Don't do anything to the points because our value coming in equals the points in the db.
                            //However, we do need to set the raw value in case that changed.
                            grades.RawPoints = rawValue;
                            db.SaveChanges();
                        }
                        else
                        {
                            grades.Points = value;
                            grades.AddedPoints = 0;
                            grades.LatePenaltyPercent = latePenalty;
                            grades.StudentPoints = -1;
                            grades.RawPoints = rawValue;
                            db.SaveChanges();
                       } 
                    }
                    else
                    {
                        if (teamuser.Count() > 0)
                        {
                            TimeSpan? lateness = calculateLateness(currentAssignment.AbstractAssignment.Category.Course, currentAssignment, teamuser.First());
                            if (lateness != null)
                            {
                                latePenalty = CalcualateLatePenaltyPercent(currentAssignment, (TimeSpan)lateness);
                                latePenalty = (100 - latePenalty) / 100;
                                value = value * latePenalty;
                            }

                            if (currentCategory.MaxAssignmentScore > 0)
                            {
                                if (((value/currentAssignment.PointsPossible)*100) > currentCategory.MaxAssignmentScore)
                                {
                                    value = (currentAssignment.PointsPossible * (currentCategory.MaxAssignmentScore / 100));
                                }
                            }

                            Score newScore = new Score()
                            {
                                TeamUserMember = teamuser.First(),
                                Points = value,
                                AssignmentActivityID = currentAssignment.ID,
                                PublishedDate = DateTime.Now,
                                isDropped = false,
                                LatePenaltyPercent = latePenalty,
                                StudentPoints = -1,
                                RawPoints = rawValue
                            };

                            db.Scores.Add(newScore);
                            db.SaveChanges();
                        }
                    }
                    if (currentAssignment.addedPoints > 0)
                    {
                        AddPoints(assignmentId, currentAssignment.addedPoints);
                    }
                }
            }

            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }


        [HttpPost]
        public ActionResult ModifyStudentScore(string userId, int assignmentId, double value)
        {
            if (ModelState.IsValid)
            {
                if (assignmentId > 0)
                {
                    UserProfile student = (from user in db.UserProfiles
                                           where user.Identification == userId
                                           select user).FirstOrDefault();
                    if (student != null)
                    {
                        double rawValue = value;
                        AbstractAssignmentActivity currentAssignment = (from assignment in db.AbstractAssignmentActivities
                                                                        where assignment.ID == assignmentId
                                                                        select assignment).FirstOrDefault();

                        List<Score> assignmentScores = (from scores in db.Scores
                                                        where scores.AssignmentActivityID == assignmentId
                                                        select scores).ToList();

                        Score studentScore = (from score in assignmentScores
                                              where score.TeamUserMember.Contains(student)
                                              select score).FirstOrDefault();

                        Category currentCategory = currentAssignment.AbstractAssignment.Category;

                        if (studentScore != null)
                        {
                            if (currentCategory.MaxAssignmentScore > 0)
                            {
                                if (((value / currentAssignment.PointsPossible) * 100) > currentCategory.MaxAssignmentScore)
                                {
                                    value = (currentAssignment.PointsPossible * (currentCategory.MaxAssignmentScore / 100));
                                }
                            }

                            studentScore.StudentPoints = value;
                            db.SaveChanges();
                        }
                        else
                        {
                            var teamuser = from c in currentAssignment.TeamUsers where c.Contains(student) select c;
                            if (currentCategory.MaxAssignmentScore > 0)
                            {
                                if (((value/currentAssignment.PointsPossible)*100) > currentCategory.MaxAssignmentScore)
                                {
                                    value = (currentAssignment.PointsPossible * (currentCategory.MaxAssignmentScore / 100));
                                }
                            }

                            Score newScore = new Score()
                            {
                                TeamUserMember = teamuser.First(),
                                Points = -1,
                                StudentPoints = value,
                                AssignmentActivityID = currentAssignment.ID,
                                PublishedDate = DateTime.Now,
                                isDropped = false,
                                RawPoints = rawValue
                            };

                            db.Scores.Add(newScore);
                            db.SaveChanges();
                        }

                        if (currentAssignment.AbstractAssignment.Category.dropX > 0)
                        {
                            ClearDropLowest(currentAssignment.AbstractAssignment.CategoryID, userId);
                            DropLowest(currentAssignment.AbstractAssignment.CategoryID, userId);
                        }
                    }
                }
            }

            BuildStudentGradebook((int)Session["categoryId"]);
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
                AbstractAssignmentActivity assignments = (from g in db.AbstractAssignmentActivities where g.ID == assignmentId select g).FirstOrDefault();
                switch (action)
                {
                    case ColumnAction.InsertLeft:
                        int position = (assignments.ColumnOrder == 1) ? 1 : assignments.ColumnOrder;
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
                    case ColumnAction.ImportCSV:

                        break;
                    default:
                        break;

                }
            }
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }


        [HttpPost]
        public void SetTabStudent(string studentId)
        {
            if (ModelState.IsValid)
            {
                if (studentId != null)
                {
                    Session["StudentID"] = studentId;
                }
            }
        }

        [HttpPost]
        public ActionResult UpdateCells()
        {
            BuildGradebook((int)Session["categoryId"]);
            return View("_Gradebook");
        }




        /// <summary>
        /// This will initialize the page using the supplied weightId (tab).
        /// </summary>
        /// <param name="weightId">The weight to load</param>
        /// <returns></returns>
        public ActionResult Index()
        {

            if (activeCourse.AbstractRole.CanGrade == true)
            {
                return TeacherIndex();
            }
            else if (activeCourse.AbstractRole.CanSubmit == true)
            {
                return StudentIndex();
            }
            return RedirectToAction("Index", "Home");

        }

        [OutputCache(Duration=3600)]
        public ActionResult TeacherIndex()
        {
            //LINQ complains when we use this directly in our queries,so pull it beforehand
            int currentCourseId = ActiveCourse.AbstractCourseID;
            List<Score> percentList = new List<Score>();

            var letterGrades = from letters in db.Courses
                               where letters.ID == currentCourseId
                               select letters.LetterGrades;

            //pull the students in the course.  Each student is a row.
            List<UserProfile> studentList = (from up in db.UserProfiles
                                             join cu in db.CourseUsers on up.ID equals cu.UserProfileID
                                             where cu.AbstractCourseID == currentCourseId && cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                             orderby up.LastName, up.FirstName
                                             select up).ToList();

            List<Score> scor = (from s in db.Scores
                                where s.AssignmentActivity.AbstractAssignment.Category.CourseID == currentCourseId
                                select s).ToList();


            List<CourseUsers> courseUsers = (from users in db.CourseUsers
                                              where users.AbstractCourseID == currentCourseId
                                              select users).ToList();

            List<Category> categories = (from category in db.Categories
                                         where category.CourseID == currentCourseId
                                         orderby category.ColumnOrder
                                         select category).ToList();

            List<AbstractAssignment> gradeAssignments = (from ga in db.AbstractAssignments
                                                         where ga.Category.CourseID == currentCourseId
                                                         select ga).ToList();

            List<Score> allGrades = (from grades in db.Scores
                                     where grades.AssignmentActivity.AbstractAssignment.Category.CourseID == currentCourseId &&
                                     grades.Points >= 0 &&
                                     grades.isDropped == false
                                     select grades).ToList();

            List<Score> allGradedAssignments = (from grades in allGrades
                                                join assignments in db.AbstractAssignmentActivities on grades.AssignmentActivityID equals assignments.ID
                                                where assignments.Scores.Count() > 0
                                                select grades).ToList();

            List<LetterGrade> letterGradeList = ((activeCourse.AbstractCourse as Course).LetterGrades).ToList();

            List<Score> categoryTotalPercent = (from categoryTotal in db.Scores
                                                where categoryTotal.AssignmentActivity.AbstractAssignment.Category.CourseID == currentCourseId &&
                                                categoryTotal.Points >= 0 &&
                                                categoryTotal.isDropped == false
                                                select categoryTotal).ToList();

            List<Category> categoriesWithWeightsAndScores = (from cats in categoryTotalPercent
                                                             select cats.AssignmentActivity.AbstractAssignment.Category).Distinct().ToList();

            double totalCategoryWeights = (from cat in categoriesWithWeightsAndScores
                                           select cat.Points).Sum();

            List<AbstractAssignmentActivity> assignmentList = (from assignment in db.AbstractAssignmentActivities
                                                               join scores in db.Scores on assignment.ID equals scores.AssignmentActivityID
                                                               where assignment.Scores.Count() > 0 &&
                                                               scores.Points >= 0
                                                               select assignment).Distinct().ToList();

            List<Score> studentScores = new List<Score>();

            foreach (UserProfile up in studentList)
            {
                foreach (Category cat in categories)
                {
                    if (cat.Name != Constants.UnGradableCatagory)
                    {
                        List<Score> allScores = (from points in db.Scores
                                                 where points.AssignmentActivity.AbstractAssignment.CategoryID == cat.ID
                                                 select points).ToList();

                        List<Score> userScores = (from points in allScores
                                                  where points.TeamUserMember.Contains(up)
                                                  select points).ToList();

                        if (userScores.Count() > 0)
                        {
                            foreach (Score score in userScores)
                            {
                                studentScores.Add(score);
                            }
                        }
                    }
                }
            }


            ViewBag.Students = studentList;
            ViewBag.Scores = studentScores;
            ViewBag.Categories = categories;
            ViewBag.CoursesUser = courseUsers;
            ViewBag.GradeAssignments = gradeAssignments;
            ViewBag.LetterGrades = letterGradeList;
            ViewBag.AllGrades = allGrades;
            ViewBag.CategoryTotalPercent = categoryTotalPercent;
            ViewBag.CatsWithWeightsAndScores = categoriesWithWeightsAndScores;
            ViewBag.TotalCategoryWeights = totalCategoryWeights;
            ViewBag.AllGradedAssignments = allGradedAssignments;
            ViewBag.Assignments = assignmentList;

            return View("Index");
        }

        public ActionResult StudentIndex()
        {
            bool usesWeights = false;
            int currentCourseId = activeCourse.AbstractCourseID;

            List<Category> categories = (from category in db.Categories
                                         where category.CourseID == currentCourseId &&
                                         category.Name != Constants.UnGradableCatagory
                                         orderby category.ColumnOrder
                                         select category).ToList();

            List<UserProfile> studentList = (from up in db.UserProfiles
                                             join cu in db.CourseUsers on up.ID equals cu.UserProfileID
                                             where cu.AbstractCourseID == currentCourseId && cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                             orderby up.LastName, up.FirstName
                                             select up).ToList();


            List<LetterGrade> letterGrades = ((activeCourse.AbstractCourse as Course).LetterGrades).OrderByDescending(l => l.MinimumRequired).ToList();

            List<UserProfile> currentUser = new List<UserProfile>();
            currentUser.Add(CurrentUser);

            int sectionNumber = (from section in db.CourseUsers
                                 where section.UserProfileID == CurrentUser.ID
                                 select section.Section).FirstOrDefault();

            List<Score> categoryTotalPercent = (from categoryTotal in db.Scores
                                                where categoryTotal.AssignmentActivity.AbstractAssignment.Category.CourseID == currentCourseId
                                                select categoryTotal).ToList();

            List<Score> userCategoryTotalPercent = (from userCategoryTotal in categoryTotalPercent
                                                    where userCategoryTotal.TeamUserMember.Contains(CurrentUser) 
                                                    select userCategoryTotal).ToList();

            List<Category> categoriesWithWeightsAndScores = (from cats in categoryTotalPercent
                                                             where cats.Points >= 0 ||
                                                             cats.StudentPoints >= 0                                                             
                                                             select cats.AssignmentActivity.AbstractAssignment.Category).Distinct().ToList();

            List<Category> userCatsWithWeightsAndScores = (from cats in userCategoryTotalPercent
                                                             where cats.Points >= 0 ||
                                                             cats.StudentPoints >= 0
                                                             select cats.AssignmentActivity.AbstractAssignment.Category).Distinct().ToList();

            double totalCategoryWeights = (from cat in categoriesWithWeightsAndScores
                                           select cat.Points).Sum();

            double userTotalCategoryWeights = (from cat in userCatsWithWeightsAndScores
                                               select cat.Points).Sum();


            ViewBag.Categories = categories;
            ViewBag.LetterGrades = letterGrades;
            ViewBag.CurrentStudent = currentUser;
            ViewBag.SectionNumber = sectionNumber;
            ViewBag.AllUserGrades = userCategoryTotalPercent;
            ViewBag.TotalCategoryWeights = totalCategoryWeights;
            ViewBag.UserTotalCategoryWeights = userTotalCategoryWeights;
            ViewBag.CatsWithWeightsAndScores = categoriesWithWeightsAndScores;
            ViewBag.CategoryTotalPercent = categoryTotalPercent;
            ViewBag.Students = studentList;
            return View("Index");
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
            if (Session["StudentID"] != null)
            {
                ViewBag.StudentId = Session["StudentID"];
            }
            Session["StudentID"] = null;

            if (activeCourse.AbstractRole.CanGrade == true)
            {
                BuildGradebook((int)categoryId);
                return View();
            }
            else if (activeCourse.AbstractRole.CanSubmit == true)
            {
                BuildStudentGradebook((int)categoryId);
                return View();
            }

            return RedirectToAction("Index", "Home");

        }


        private void BuildStudentGradebook(int categoryId)
        {
            int currentCourseId = ActiveCourse.AbstractCourseID;

            List<Category> categories = (from category in db.Categories
                                         where category.CourseID == currentCourseId
                                         orderby category.ColumnOrder
                                         select category).ToList();

            List<AbstractAssignmentActivity> assignments = (from assignment in db.AbstractAssignmentActivities
                                                            where assignment.AbstractAssignment.CategoryID == categoryId &&
                                                            (!(assignment is StopActivity))
                                                            orderby assignment.ColumnOrder
                                                            select assignment).ToList();

            List<UserProfile> currentUser = new List<UserProfile>();
            currentUser.Add(CurrentUser);

            List<Score> totalScores = (from scores in db.Scores
                                       where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                                       select scores).ToList();

            List<Score> userScores = (from score in totalScores
                                      where score.TeamUserMember.Contains(CurrentUser)
                                      select score).ToList();


            int numDropped = (from cats in categories
                              where cats.ID == categoryId
                              select cats.dropX).FirstOrDefault();

            int customize = (from op in categories
                             where op.ID == categoryId
                             select op.Customize).FirstOrDefault();

            Category.GradeOptions customize_options = new Category.GradeOptions();
            switch (customize)
            {
                case 0:
                    customize_options = Category.GradeOptions.CompAverage;
                    break;
                case 1:
                    customize_options = Category.GradeOptions.XtoDrop;
                    break;
                case 2:
                    customize_options = Category.GradeOptions.XtoTake;
                    break;
                default:
                    break;
            };


            ViewBag.Categories = categories;
            ViewBag.Assignments = assignments;
            ViewBag.CurrentStudent = currentUser;
            ViewBag.NumDropped = numDropped;
            ViewBag.Customize = customize_options;
            ViewBag.UserScores = userScores;
            ViewBag.TotalScores = totalScores;
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
            int currentCourseId = ActiveCourse.AbstractCourseID;
            List<TeamUserMember> userMembers = new List<TeamUserMember>();
            //List of students scores
            List<Score> studentScores = new List<Score>();

            //pull all weights (tabs) for the current course
            var cats = from category in db.Categories
                       where category.CourseID == currentCourseId
                       orderby category.ColumnOrder
                       select category;

            List<Category> allCategories = cats.ToList();

            Category currentTab = null;
            var dbCategories = (from c in allCategories where c.ID == categoryId select c);
            if (dbCategories.Count() > 0)
            {
                //Probably only 1 thing will match, take the first.
                currentTab = dbCategories.First();
            }
            else if (dbCategories.Count() == 0)
            {
                //If there were no matches, then set currenTab to the main tab
                currentTab = (from c in allCategories select c).First();
            }

            int numDropped = currentTab.dropX;

            var customizeOption = (Category.GradeOptions)currentTab.Customize;

            //save to the session.  Needed later for AJAX-related updates.
            Session["CurrentCategoryId"] = currentTab.ID;

            //pull the gradables (columns) for the current weight (tab)
            //Pull the gradeAssignments
            List<AbstractAssignmentActivity> gradeAssignments = (from ga in db.AbstractAssignmentActivities
                                                                 where ga.AbstractAssignment.CategoryID == currentTab.ID &&
                                                                 (!(ga is StopActivity))
                                                                 orderby ga.ColumnOrder
                                                                 select ga).ToList();

            if (gradeAssignments.Count() == 0)
            {
                List<CourseUsers> Users = (from user in db.CourseUsers
                                            where user.AbstractCourseID == currentCourseId
                                            select user).ToList();

                foreach (CourseUsers u in Users)
                {
                    UserMember userMember = new UserMember()
                    {
                        UserProfile = u.UserProfile,
                        UserProfileID = u.UserProfileID
                    };
                    userMembers.Add(userMember);
                    db.TeamUsers.Add(userMember);
                }
                db.SaveChanges();

                StudioAssignment newAssignment = new StudioAssignment()
                {
                    Name = "Untitled",
                    //PointsPossible = 100,
                    AssignmentActivities = new List<AbstractAssignmentActivity>(),
                    CategoryID = categoryId,
                    ColumnOrder = 1,
                    Description = "No description",
                    IsDraft = false
                };
                db.StudioAssignments.Add(newAssignment);
                db.SaveChanges();

                GradeActivity newActivity = new GradeActivity()
                {
                    AbstractAssignmentID = newAssignment.ID,
                    Name = "Untitled",
                    PointsPossible = newAssignment.PointsPossible,
                    AbstractAssignment = newAssignment,
                    ColumnOrder = 1,
                    TeamUsers = userMembers
                    //Scores = new List<Score>()
                };
                db.AbstractAssignmentActivities.Add(newActivity);
                db.SaveChanges();

                //pull the gradables (columns) for the current weight (tab)
                //Pull the gradeAssignments
                gradeAssignments = (from ga in db.AbstractAssignmentActivities
                                    where ga.AbstractAssignment.CategoryID == currentTab.ID &&
                                    (!(ga is StopActivity))
                                    orderby ga.ColumnOrder
                                    select ga).ToList();

            }


            //Pull the dropped lowest
            var droppedCount = from scores in db.Scores
                               where scores.AssignmentActivity.AbstractAssignment.CategoryID == currentCourseId &&
                               scores.isDropped == true
                               select scores;

            //pull the students in the course.  Each student is a row.
            List<UserProfile> students = (from up in db.UserProfiles
                                          join cu in db.CourseUsers on up.ID equals cu.UserProfileID
                                          where cu.AbstractCourseID == currentCourseId && cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                          orderby up.LastName, up.FirstName
                                          select up).ToList();


            //Finally the scores for each student.
            List<Score> scor = (from score in db.Scores
                                where score.AssignmentActivity.AbstractAssignment.CategoryID == currentTab.ID &&
                                score.Points >= 0 /*&&
                               score.isDropped == false*/
                                select score).ToList();

            var userScore = from scores in scor
                            where scores.isDropped == false
                            && scores.Points >= 0
                            group scores by scores.TeamUserMember.Name into userScores
                            select userScores;

            if (userScore.Count() > 0)
            {
                for (int i = 0; i < userScore.Count(); i++)
                {
                    double currentPoints = 0;
                    double currentTotal = 0;
                    TeamUserMember currentUser = new UserMember();
                    var item = userScore.AsEnumerable().ElementAt(i);
                    var currentAssignment = item.First().AssignmentActivity;

                    foreach (Score a in item)
                    {
                        currentUser = a.TeamUserMember;
                        currentPoints += a.Points;
                        currentTotal += a.AssignmentActivity.PointsPossible;
                    }
                    var teamuser = from c in currentAssignment.TeamUsers where c.Name ==currentUser.Name select c;
                    if (teamuser.Count() > 0)
                    {
                        Score newscore = new Score()
                        {
                            TeamUserMember = teamuser.First(),
                            Points = ((currentPoints / currentTotal) * 100),
                            isDropped = false
                        };
                        studentScores.Add(newscore);
                    }
                }
            }
            
            //save everything that we need to the viewebag
            ViewBag.Categories = allCategories;
            ViewBag.Grades = scor;
            ViewBag.Assignments = gradeAssignments;
            ViewBag.Users = students;
            ViewBag.Percents = studentScores;
            ViewBag.Dropped = numDropped;
            ViewBag.Customize = customizeOption;
            ViewBag.MaxPoints = currentTab.MaxAssignmentScore;

            Session["isTab"] = 1;
        }

        /// <summary>
        /// This will initialize the page using the first weightIdfound in the database.  If
        /// No weightId exists, a new one will be created
        /// </summary>
        /// <returns></returns>
        private int GetDefaultWeightId()
        {
            //LINQ complains when we use this directly in our queries,so pull it beforehand
            int currentCourseId = ActiveCourse.AbstractCourseID;

            //List to store teamUserMembers
            List<TeamUserMember> userMembers = new List<TeamUserMember>();

            //By default, select the first tab
            var categoryQuery = from category in db.Categories
                                where category.CourseID == currentCourseId
                                orderby category.ColumnOrder ascending
                                select category;

            //if something was found, complete the query and get thefirst weight listed
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

                List<CourseUsers> Users = (from user in db.CourseUsers
                                            where user.AbstractCourseID == currentCourseId
                                            select user).ToList();

                foreach (CourseUsers u in Users)
                {
                    UserMember userMember = new UserMember()
                    {
                        UserProfile = u.UserProfile,
                        UserProfileID = u.UserProfileID
                    };
                    userMembers.Add(userMember);
                    db.TeamUsers.Add(userMember);
                }
                db.SaveChanges();

                StudioAssignment newAssignment = new StudioAssignment()
                {
                    Name = "Untitled",
                    //PointsPossible = 100,
                    AssignmentActivities = new List<AbstractAssignmentActivity>(),
                    CategoryID = newCategory.ID,
                    ColumnOrder = 1,
                    Description = "No description",
                    IsDraft = false
                };
                db.StudioAssignments.Add(newAssignment);
                db.SaveChanges();

                AbstractAssignmentActivity newActivity = new GradeActivity()
                {
                    AbstractAssignmentID = newAssignment.ID,
                    Name = "Untitled",
                    PointsPossible = newAssignment.PointsPossible,
                    AbstractAssignment = newAssignment,
                    TeamUsers = userMembers
                    /*ColumnOrder = 0,
                    Scores = new List<Score>()*/
                };
                db.AbstractAssignmentActivities.Add(newActivity);
                db.SaveChanges();

                //with a new weight / gradable combo created, we can
                //call Index() to finish
                //off the rendering
                return newCategory.ID;
            }
        }
    }
}
