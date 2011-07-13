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

           var students = from student in db.CoursesUsers
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
                                   AbstractAssignmentActivity assign = (from g in db.AbstractAssignmentActivities where g.ID == currentAssignmentID select g).First();
                                   if (Session["radio"].ToString() == "r")
                                   {
                                       
                                       var position = from pos in db.AbstractAssignmentActivities
                                                      where pos.ColumnOrder > assign.ColumnOrder
                                                      select pos;

                                       if (position.Count() > 0)
                                       {

                                           foreach (AbstractAssignmentActivity col in position)
                                           {
                                               col.ColumnOrder += 1;
                                           }
                                           db.SaveChanges();

                                           AddColumn(assignment.ToString(), 10, assign.ColumnOrder + assignmentNumber);
                                           positionList.Add(assign.ColumnOrder + assignmentNumber);
                                           index.Add(count);
                                           currentAssignmentID = (from g in db.AbstractAssignments where g.ColumnOrder == (assign.ColumnOrder + 1) select g.ID).First();
                                       }
                                       else
                                       {
                                           AddColumn(assignment.ToString(), 10, assign.ColumnOrder + assignmentNumber);
                                           positionList.Add(assign.ColumnOrder + assignmentNumber);
                                           index.Add(count);
                                           currentAssignmentID = (from g in db.AbstractAssignments where g.ColumnOrder == (assign.ColumnOrder + 1) select g.ID).First();
                                       }
                                       
                                   }
                                   else if (Session["radio"].ToString() == "l")
                                   {

                                       var position = from pos in db.AbstractAssignmentActivities
                                                      where pos.ColumnOrder >= assign.ColumnOrder
                                                      select pos;

                                       if (position.Count() > 0)
                                       {
                                           
                                           foreach (AbstractAssignmentActivity col in position)
                                           {
                                               col.ColumnOrder += 1;
                                           }
                                           db.SaveChanges();

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
                               var student = from stu in db.CoursesUsers
                                             where stu.UserProfile.Identification == studentId
                                             select stu;

                               if (student.Count() > 0)
                               {
                                   int col = positionList.ElementAt(currentColOrder);
                                   currentColOrder++;
                                   var assign = from a in db.AbstractAssignments
                                                where a.ColumnOrder == col
                                                select a;
                                   var assignmentQuery = from a in db.AbstractAssignmentActivities
                                                         where a.AbstractAssignment.ColumnOrder == col
                                                         select a;

                                   var currentAssignment = assignmentQuery.FirstOrDefault();

                                   UserProfile user = (from u in db.UserProfiles
                                                       where u.Identification == studentId
                                                       select u).FirstOrDefault();

                                   if (assign.Count() > 0)
                                   {
                                       if (user != null)
                                       {
                                           var teamuser = from c in currentAssignment.TeamUsers where c.Contains(user) select c;

                                           if (teamuser.Count() > 0)
                                           {
                                               Score newScore = new Score()
                                               {
                                                   TeamUserMember = teamuser.First(),
                                                   Points = Convert.ToDouble(assignment),
                                                   AssignmentActivityID = currentAssignment.ID,
                                                   PublishedDate = DateTime.Now,
                                                   isDropped = false
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

       [HttpPost]
       public void ExportGadesToCSV()
       {
           int currentCourseId = activeCourse.AbstractCourseID;
           FileStream fileStream = new FileStream("C:/Users/Andrew/Desktop/newCSV.csv", FileMode.OpenOrCreate, FileAccess.Write);

           StreamWriter streamWriter = new StreamWriter(fileStream);
           
           //Create a List of strings
           List<string> stringList = new List<string>();

           string weights = ",Weights,,";
           string perfectScore = ",Perfect Score,";
           string averageScore = ",Average Score,";
           string headerString = "Section,Name,Grade,Total Grade";
           string averageLetter = "";

           double totalColumnGrade = 0;
           double totalPossibleGrade = 0;

           List<Category> categories = (from cat in db.Categories
                                        where cat.CourseID == currentCourseId
                                        select cat).ToList();

           if (categories.Count() > 0)
           {
               foreach (Category item in categories)
               {
                   List<AbstractAssignmentActivity> assignments = (from assignment in db.AbstractAssignmentActivities
                                                                   where assignment.AbstractAssignment.CategoryID == item.ID
                                                                   select assignment).ToList();
                   if (item.Name != Constants.UnGradableCatagory)
                   {
                       headerString += "," + item.Name.ToUpper();
                       weights += "," + item.Points.ToString();
                   }
                   foreach (AbstractAssignmentActivity assign in assignments)
                   {
                       headerString += "," + assign.Name;
                       weights += ",";
                   }
               }
           }

           //Add the weights to the string list
           stringList.Add(weights);
           //streamWriter.WriteLine(weights);
           
           //streamWriter.WriteLine(perfectScore);
           //streamWriter.WriteLine(averageScore);

           //Add the header string to the string list
           stringList.Add(headerString);
           //streamWriter.WriteLine(headerString);

           List<LetterGrade> letterGrades = ((activeCourse.AbstractCourse as Course).LetterGrades).ToList();
           
           

           //pull the students in the course.  Each student is a row.
           List<UserProfile> studentList = (from up in db.UserProfiles
                                            join cu in db.CoursesUsers on up.ID equals cu.UserProfileID
                                            where cu.AbstractCourseID == currentCourseId && cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                            orderby up.LastName, up.FirstName
                                            select up).ToList();

           double totalCoursePointsPossible = (from points in db.AbstractAssignmentActivities
                                            where points.AbstractAssignment.Category.CourseID == currentCourseId
                                            select points.PointsPossible).Sum();

           double totaCourseScore = (from score in db.Scores
                                  where score.AssignmentActivity.AbstractAssignment.Category.CourseID == currentCourseId
                                  select score.Points).Sum();

           double totalScore = ((Convert.ToDouble(totaCourseScore) / (Convert.ToDouble(totalCoursePointsPossible * studentList.Count()))) * 100);
           string totalScoreString = "," + totalScore.ToString(".##") + "%";

           foreach (LetterGrade letter in letterGrades)
           {
               if (totalScore > letter.MinimumRequired)
               {
                   averageLetter = letter.Grade;
                   break;
               }
           }


           if (studentList.Count() > 0)
           {
               double totalGradePercet = 0;
               double totalGradePossible = 0;
               string studentString = "";
               foreach (UserProfile user in studentList)
               {
                   

                   //Section
                   string section;
                   //Name
                   string name;
                   //User Letter Grade
                   string userLetterGrade;
                   //User Total Grade
                   string userTotalGrade;
                   //CategoryTotals
                   string categoryTotals = "";

                   perfectScore = "";
                   perfectScore = ",Perfect Score";

                   averageScore = "";
                   averageScore = ",Average Score" + "," + averageLetter + totalScoreString; 

                   string HighestLetterGrade = letterGrades.First().Grade;
                   perfectScore += "," + HighestLetterGrade + "," + 100 + "%";

                   int sect = (from sec in db.CoursesUsers
                                  where sec.AbstractCourseID == currentCourseId &&
                                  sec.UserProfileID == user.ID
                                  select sec.Section).FirstOrDefault();
                   
                   //Section
                   section = sect.ToString();

                   //Name
                   name = "," + user.FirstName + " " + user.LastName;
                   

                   //Grade
                   userLetterGrade = ",";

                   //CATEGORY
                   foreach (Category c in categories)
                   {
                       //Category TOTAL
                       if (c.Name != Constants.UnGradableCatagory)
                       {
                           
                           List<Score> TotalScores = (from s in db.Scores
                                                      where s.AssignmentActivity.AbstractAssignment.CategoryID == c.ID
                                                      select s).ToList();

                           var TotalUserScore = (from scor in TotalScores
                                                 where scor.TeamUserMember.Contains(user)
                                                 select scor.Points).Sum();

                           var totalCategoryScore = (from score in TotalScores
                                                     where score.Points >= 0
                                                     select score.Points).Sum();

                           var totalCategoryPointsPossible = (from pointsPossible in db.AbstractAssignmentActivities
                                                              where pointsPossible.AbstractAssignment.CategoryID == c.ID
                                                              select pointsPossible.PointsPossible).Sum();

                           double displayUserCategoryTotal = Convert.ToDouble(TotalUserScore) / Convert.ToDouble(totalCategoryPointsPossible);
                           totalGradePercet += TotalUserScore;
                           totalGradePossible += totalCategoryPointsPossible;

                           totalColumnGrade += totalCategoryScore;
                           totalPossibleGrade += totalCategoryPointsPossible;

                           categoryTotals += "," + (displayUserCategoryTotal * 100).ToString(".##") + "%";
                           perfectScore += "," + 100 + "%";
                           averageScore += "," + (Convert.ToDouble(totalCategoryScore) / Convert.ToDouble(totalCategoryPointsPossible * studentList.Count()) * 100).ToString(".##") + "%";
                       }

                       //Assignments
                       List<AbstractAssignmentActivity> assignments = (from assignment in db.AbstractAssignmentActivities
                                                                       where assignment.AbstractAssignment.CategoryID == c.ID
                                                                       select assignment).ToList();
                      
                       foreach (AbstractAssignmentActivity assign in assignments)
                       {
                           double totalPoints = 0;
                           double totalPointsPossible = 0;

                           List<Score> scores = (from score in db.Scores
                                                 where score.AssignmentActivityID == assign.ID &&
                                                 score.Points >= 0
                                                 select score).ToList();

                           Score userScore = (from s in scores
                                              where s.TeamUserMember.Contains(user)
                                              select s).FirstOrDefault();

                           totalPoints = (from s in scores
                                          select s.Points).Sum();

                           //Find the points possible of the assignment and multiply that by the
                           //number of students in the class who have scores
                           totalPointsPossible = (assign.PointsPossible * scores.Count());
                           
                           categoryTotals += "," + userScore.Points;
                           perfectScore += "," + assign.PointsPossible;
                           averageScore += "," + ((totalPoints / totalPointsPossible) * 100);
                       }
                       
                   }
                   double total = ((totalGradePercet / totalGradePossible) * 100);
                   userTotalGrade = "," + total.ToString(".##") + "%";
                   //Add the studentString to the file

                   foreach (LetterGrade min in letterGrades)
                   {
                       if (total > min.MinimumRequired)
                       {
                           userLetterGrade += min.Grade;
                       }
                   }

                   string finalString = string.Concat(section, name, userLetterGrade, userTotalGrade, categoryTotals);
                   stringList.Add(finalString);
               }

               stringList.Insert(1, averageScore);
               stringList.Insert(1, perfectScore);
               
           }
           for (int i = 0; i < stringList.Count(); i++)
           {
               streamWriter.WriteLine(stringList.ElementAt(i));
           }

           streamWriter.Close();
           
       }

       
       private void AddColumn(string columnName, int pointsPossible, int position)
       {
           int categoryId = Convert.ToInt32(Session["CurrentCategoryID"]);

           List<TeamUserMember> userMembers = new List<TeamUserMember>();

           int currentCourseId = ActiveCourse.AbstractCourseID;
           List<CoursesUsers> Users = (from user in db.CoursesUsers
                                       where user.AbstractCourseID == currentCourseId
                                       select user).ToList();

           foreach (CoursesUsers u in Users)
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

           GradeAssignment newAssignment = new GradeAssignment()
           {
               Name = columnName,
               PointsPossible = pointsPossible,
               AssignmentActivities = new List<AbstractAssignmentActivity>(),
               CategoryID = categoryId,
               ColumnOrder = position
           };
           db.GradeAssignments.Add(newAssignment);
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
           AbstractAssignmentActivity assignment = db.AbstractAssignmentActivities.Find(assignmentId);
           List<TeamUserMember> teamMember = (from a in assignment.TeamUsers select a).ToList();
           foreach (UserMember item in teamMember)
           {
               db.TeamUsers.Remove(item);
           }
           db.SaveChanges();
           db.AbstractAssignmentActivities.Remove(assignment);
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
                                       && scores.TeamUserMemberID == userId
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
       public ActionResult DropLowest(int categoryId, string userId)
       {
           if (ModelState.IsValid)
           {
               double lowest = 10000;
               int id = 0;
               if (categoryId > 0)
               {
                   UserProfile user = (from u in db.UserProfiles
                                       where u.Identification == userId
                                       select u).FirstOrDefault();

                   List<Score> scoreList = (from scores in db.Scores
                                            where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId
                                            select scores).ToList();

                   var studentScores = (from scores in scoreList
                                        where scores.TeamUserMember.Name == user.LastName + ", " + user.FirstName
                                        select scores);

                   if (studentScores.Count() > 0)
                   {
                       foreach (Score score in studentScores)
                       {
                            double temp = score.Points / score.AssignmentActivity.AbstractAssignment.PointsPossible;
                            if (temp < lowest)
                            {
                                lowest = temp;
                                id = score.ID;
                            }
                       }
                        foreach (Score score in studentScores)
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
           BuildGradebook((int)Session["categoryId"]);
           return View("_Gradebook");
       }


       [HttpPost]
       public ActionResult AllDropLowest(int categoryId, int dropX)
       {
           if (ModelState.IsValid)
           {
               //storing the amount of assignments wanted to drop
               var currentCatagory = (from cat in db.Categories where cat.ID == categoryId select cat).FirstOrDefault();
               currentCatagory.dropX = dropX;
               db.SaveChanges();

               int i = 0;
               if (categoryId > 0)
               {
                   List<Score> scoreList = (from s in db.Scores
                                            where s.AssignmentActivity.AbstractAssignment.CategoryID == categoryId &&
                                            s.Points >= 0
                                            select s).ToList();

                   var studentScores = (from scores in scoreList
                                        select scores).GroupBy(s => s.TeamUserMember.Name);
                   
                   if (studentScores.Count() > 0)
                   {
                       for (i = 0; i < studentScores.Count(); i++)
                       {
                           List<double> lowest = new List<double>();
                           List<int> id = new List<int>();

                           var item = studentScores.AsEnumerable().ElementAt(i);
                           
                           foreach (Score score in item)
                           {
                               //Find the assignment that corresponds because it wouldn't give me access
                               //to points possible within the scores.
                               var assignmentScore = from a in db.AbstractAssignments
                                                     where a.ID == score.AssignmentActivity.AbstractAssignmentID
                                                     select a;
                               // create an ascending list of scores per student
                               int j = 0;
                               double temp = score.Points / assignmentScore.FirstOrDefault().PointsPossible;

                               while (lowest.Count() > j && temp > lowest.ElementAt(j))
                               {
                                   j++;
                               }
                               lowest.Insert(j, temp);
                               id.Insert(j, score.ID);
                           }

                           // Dropping X lowest ( if there are less assignments than specified to drop, it only drops what is graded)
                           for (int k = 0; k < dropX; k++)
                           {
                               if (k < lowest.Count()) // only drops the amount of graded assignments per student maximum
                               {
                                   foreach (Score newScore in item)
                                   {
                                       if (newScore.ID == id.ElementAt(k))
                                       {
                                           newScore.isDropped = true;
                                       }
                                   }
                               }
                           }

                           db.SaveChanges();
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
                   item.AssignmentActivity.PointsPossible = -1;
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
                                       where a.AbstractAssignmentID == assignmentId
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
           return View("Index");
       }

       [HttpPost]
       public ActionResult DeleteCategory(int categoryId)
       {
           List<AbstractAssignmentActivity> assignmentList = (from assignments in db.AbstractAssignmentActivities
                                                              where assignments.AbstractAssignment.CategoryID == categoryId
                                                              select assignments).ToList();
           for (int i = 0; i < assignmentList.Count(); i++)
           {
               List<TeamUserMember> teamMember = (from a in assignmentList.ElementAt(i).TeamUsers select a).ToList();
               foreach (UserMember item in teamMember)
               {
                   db.TeamUsers.Remove(item);
               }
               db.SaveChanges();
           }

           Category category = db.Categories.Find(categoryId);   
           db.Categories.Remove(category);
           db.SaveChanges();

           return View();
       }

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
                                         where grade.AssignmentActivity.AbstractAssignmentID == assignmentId &&
                                         grade.Points >= 0
                                         select grade).ToList();

                   var assignment = (from assigns in db.AbstractAssignmentActivities
                                     where assigns.ID == assignmentId
                                     select assigns).FirstOrDefault();

                   if (grades.Count() > 0)
                   {
                       foreach (Score item in grades)
                       {
                           if (assignment.addedPoints > 0)
                           {
                               item.Points -= assignment.addedPoints;
                           }
                           item.Points += number;
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

                   AbstractAssignment assignment = (from assign in db.AbstractAssignments where assign.ID == assignmentId select assign).FirstOrDefault();
                   Category category = (from cat in db.Categories where cat.Name == categoryName && cat.CourseID == currentCourseId select cat).FirstOrDefault();
                   int newCategoryLastAssignment = (from a in db.AbstractAssignments where a.CategoryID == category.ID orderby a.ColumnOrder descending select a.ColumnOrder).FirstOrDefault();

                   if (assignment != null && category != null)
                   {
                       assignment.CategoryID = category.ID;
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
           return View("Index");
       }


       [HttpPost]
       public ActionResult ModifyCell(double value, string userId, int assignmentId)
       {

           //Continue if we have a valid gradable ID
           if (assignmentId != 0)
           {
               //Get student
               var user = (from u in db.UserProfiles where u.Identification == userId select u).FirstOrDefault();
               
               if (user != null)
               {
                   List<Score> gradableQuery = (from g in db.Scores
                                                where g.AssignmentActivityID == assignmentId
                                                select g).ToList();

                   Score grades = (from grade in gradableQuery
                                   where grade.TeamUserMember.Contains(user)
                                   select grade).FirstOrDefault();

                   var assignmentQuery = from a in db.AbstractAssignmentActivities
                                         where a.AbstractAssignmentID == assignmentId
                                         select a;

                   var currentAssignment = assignmentQuery.FirstOrDefault();
                   if (currentAssignment.addedPoints > 0)
                   {
                       value += currentAssignment.addedPoints;
                   }

                   if (grades != null)
                   {
                       grades.Points = value;
                       db.SaveChanges();
                   }
                   else
                   {
                       var teamuser = from c in currentAssignment.TeamUsers where c.Contains(user) select c;

                       if (teamuser.Count() > 0)
                       {
                           Score newScore = new Score()
                           {
                               TeamUserMember = teamuser.First(),
                               Points = value,
                               AssignmentActivityID = currentAssignment.ID,
                               PublishedDate = DateTime.Now,
                               isDropped = false
                           };

                           db.Scores.Add(newScore);
                           db.SaveChanges();
                       }
                   }
               }
           }

           BuildGradebook((int)Session["categoryId"]);
           return View("_Gradebook");
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
       public void SetTabStudent( string studentId )
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
           return View("_Gradebook");
       }

       


       /// <summary>
       /// This will initialize the page using the supplied weightId (tab).
       /// </summary>
       /// <param name="weightId">The weight to load</param>
       /// <returns></returns>
       public ActionResult Index()
       {
           //LINQ complains when we use this directly in our queries,so pull it beforehand
           int currentCourseId = ActiveCourse.AbstractCourseID;
           List<Score> percentList = new List<Score>();

           var letterGrades = from letters in db.Courses
                              where letters.ID == currentCourseId
                              select letters.LetterGrades;

           //pull the students in the course.  Each student is a row.
           List<UserProfile> studentList = (from up in db.UserProfiles
                                            join cu in db.CoursesUsers on up.ID equals cu.UserProfileID
                                            where cu.AbstractCourseID == currentCourseId && cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                            orderby up.LastName, up.FirstName
                                            select up).ToList();

           List<Score> scor = (from s in db.Scores
                               where s.AssignmentActivity.AbstractAssignment.Category.CourseID == currentCourseId
                               select s).ToList();

           
           List<CoursesUsers> courseUsers = (from users in db.CoursesUsers
                                             where users.AbstractCourseID == currentCourseId
                                             select users).ToList();

           List<Category> categories = (from category in db.Categories
                                        where category.CourseID == currentCourseId
                                        orderby category.ColumnOrder 
                                        select category).ToList();

           List<GradeAssignment> gradeAssignments = (from ga in db.GradeAssignments
                                                     where ga.Category.CourseID == currentCourseId
                                                     select ga).ToList();

           List<LetterGrade> letterGradeList = ((activeCourse.AbstractCourse as Course).LetterGrades).ToList();

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
           if (Session["StudentID"] != null)
           {
               ViewBag.StudentId = Session["StudentID"];
           }
           Session["StudentID"] = null;
           BuildGradebook((int)categoryId);
           if (Convert.ToInt32(Session["isTab"]) == 0)
           {
               return RedirectToRoute("Index");
           }
           return View();
       }


       public void AddLetterGrade(string grade, int minReq)
       {
           int currentCourseId = ActiveCourse.AbstractCourseID;

           LetterGrade newLetterGrade = new LetterGrade()
           {
               Grade = grade,
               MinimumRequired = minReq
           };

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

           Category currentTab = (from c in allCategories where c.ID == categoryId select c).First();

           if (currentTab == null)
           {
               currentTab = (from c in allCategories select c).First();
           }

           List<int> numDropped = new List<int>();
           numDropped.Add(currentTab.dropX);

           //save to the session.  Needed later for AJAX-related updates.
           Session["CurrentCategoryId"] = currentTab.ID;

           //pull the gradables (columns) for the current weight (tab)
           List<AbstractAssignmentActivity> assignments = (from a in db.AbstractAssignmentActivities
                                                   where a.AbstractAssignment.CategoryID == currentTab.ID
                                                   orderby a.ColumnOrder
                                                   select a).ToList();

           if (assignments.Count() == 0)
           {
               List<CoursesUsers> Users = (from user in db.CoursesUsers
                                           where user.AbstractCourseID == currentCourseId
                                           select user).ToList();

               foreach (CoursesUsers u in Users)
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

               GradeAssignment newAssignment = new GradeAssignment()
               {
                   Name = "Untitled",
                   PointsPossible = 100,
                   AssignmentActivities = new List<AbstractAssignmentActivity>(),
                   CategoryID = categoryId,
                   ColumnOrder = 1
               };
               db.AbstractAssignments.Add(newAssignment);
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
           }

           //Pull the gradeAssignments
           List<AbstractAssignmentActivity> gradeAssignments = (from ga in db.AbstractAssignmentActivities
                                                                where ga.AbstractAssignment.CategoryID == currentTab.ID &&
                                                                (!(ga is StopActivity))
                                                                orderby ga.ColumnOrder
                                                                select ga).ToList();

           //Pull the dropped lowest
           var droppedCount = from scores in db.Scores
                              where scores.AssignmentActivity.AbstractAssignment.CategoryID == currentCourseId &&
                              scores.isDropped == true
                              select scores;

           //pull the students in the course.  Each student is a row.
           List<UserProfile> students = (from up in db.UserProfiles
                                         join cu in db.CoursesUsers on up.ID equals cu.UserProfileID
                                         where cu.AbstractCourseID == currentCourseId && cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                         orderby up.LastName, up.FirstName
                                         select up).ToList();


           //Finally the scores for each student.
           List<Score> scor = (from score in db.Scores
                               where score.AssignmentActivity.AbstractAssignment.CategoryID == currentTab.ID
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
                       currentTotal += a.AssignmentActivity.AbstractAssignment.PointsPossible;
                   }
                   UserMember user = currentUser as UserMember;
                   
                   var teamuser = from c in currentAssignment.TeamUsers where c.Contains(user.UserProfile) select c;
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
           ViewBag.Assignments = assignments;
           ViewBag.GradeAssignments = gradeAssignments;
           ViewBag.Users = students;
           ViewBag.Percents = studentScores;
           ViewBag.Dropped = numDropped;

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

               List<CoursesUsers> Users = (from user in db.CoursesUsers
                                           where user.AbstractCourseID == currentCourseId
                                           select user).ToList();

               foreach (CoursesUsers u in Users)
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

               GradeAssignment newAssignment = new GradeAssignment()
               {
                   Name = "Untitled",
                   PointsPossible = 100,
                   AssignmentActivities = new List<AbstractAssignmentActivity>(),
                   CategoryID = newCategory.ID,
                   ColumnOrder = 0
               };
               db.GradeAssignments.Add(newAssignment);
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
