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

           List<string[]> parsedData = new List<string[]>();
           List<int> positionList = new List<int>();
           List<int> doNotAdd = new List<int>();
           int currentCourseId = ActiveCourse.CourseID;
           int studentID = 0;
           int studentIdPosition = 0;
           int doNotIncludeCols = 0;
           

           var students = from student in db.CoursesUsers
                          where student.CourseID == currentCourseId
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

           #region
           /*if (parsedData.Count > 0)
           {
               for (int i = 0; i < parsedData.Count(); i++)
               {
                   int count = 0;
                   int currentColOrder = 0;
                   var item = parsedData.ElementAt(i);
                   foreach (var num in item)
                   {
                       if (i == 0)
                       {
                           if (num == "Last Name")
                           {
                               doNotIncludeCols++;
                           }
                           else if (num == "First Name")
                           {
                               doNotIncludeCols++;
                           }
                           else if (num == "Student ID")
                           {
                               studentIdPosition = count;
                           }
                           else if (num == "Remote ID")
                           {
                               doNotIncludeCols++;
                           }
                           else if (num == "Aggregate Performance")
                           {
                               doNotIncludeCols++;
                           }
                           else if (num == "Aggregate Participation")
                           {
                               doNotIncludeCols++;
                           }
                           else if (num == "Aggregate Total")
                           {
                               doNotIncludeCols++;
                           }
                           else if (num == "Total")
                           {
                               doNotIncludeCols++;
                           }

                           else
                           {
                               var position = (from pos in db.AbstractAssignments
                                               orderby pos.ColumnOrder descending
                                               select pos).First();
                               AddColumn(num.ToString(), 10, position.ColumnOrder + 1);
                               positionList.Add(position.ColumnOrder + 1);
                           }
                       }
                       else
                       {
                           if (count <= doNotIncludeCols)
                           {
                               if (count == studentIdPosition)
                               {
                                   studentID = Convert.ToInt32(num);
                               }
                           }
                           else
                           {
                               var student = from stu in db.CoursesUsers
                                             where stu.UserProfileID == studentID
                                             select stu;

                               if (student.Count() > 0)
                               {
                                   int col = positionList.ElementAt(currentColOrder);
                                   currentColOrder++;
                                   var assignment = from a in db.AbstractAssignments
                                                    where a.ColumnOrder == col
                                                    select a;

                                   var assignmentQuery = from a in db.AssignmentActivities
                                                         where a.AbstractAssignment.ColumnOrder == col
                                                         select a;

                                   var currentAssignment = assignmentQuery.FirstOrDefault();

                                   if (assignment.Count() > 0)
                                   {
                                       Score newScore = new Score()
                                       {
                                           UserProfileID = studentID,
                                           Points = Convert.ToDouble(num),
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
                       count++;
                   }
               }
           }
           BuildGradebook((int)Session["categoryId"]);
           return View("_Gradebook");*/
           #endregion
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
           int studentId = 0;

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
                                   var position = (from pos in db.AbstractAssignments
                                                   orderby pos.ColumnOrder descending
                                                   select pos).First();
                                   AddColumn(assignment.ToString(), 10, position.ColumnOrder + 1);
                                   positionList.Add(position.ColumnOrder + 1);
                                   index.Add(count);
                               }
                           }
                       }
                       else
                       {
                           if (count == studentPosition)
                           {
                               studentId = Convert.ToInt32(assignment);
                           }
                           else if (index.Contains(count))
                           {
                               var student = from stu in db.CoursesUsers
                                             where stu.UserProfileID == studentId
                                             select stu;
                               if (student.Count() > 0)
                               {
                                   int col = positionList.ElementAt(currentColOrder);
                                   currentColOrder++;

                                   var assign = from a in db.AbstractAssignments
                                                where a.ColumnOrder == col
                                                select a;
                                   var assignmentQuery = from a in db.AbstractAssignmentActivity
                                                         where a.AbstractAssignment.ColumnOrder == col
                                                         select a;

                                   var currentAssignment = assignmentQuery.FirstOrDefault();

                                   if (assign.Count() > 0)
                                   {
                                       Score newScore = new Score()
                                       {
                                           UserProfileID = studentId,
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
                       count++;
                   }
               }
                        
           }
           return RedirectToAction("Tab", "Gradebook", new { categoryId = (int)Session["categoryId"] });
       }


       private void AddColumn(string columnName, int pointsPossible, int position)
       {
           int categoryId = Convert.ToInt32(Session["CurrentCategoryID"]);

           GradeAssignment newAssignment = new GradeAssignment()
           {
               Name = columnName,
               PointsPossible = pointsPossible,
               AssignmentActivities = new List<AbstractAssignmentActivity>(),
               CategoryID = categoryId,
               ColumnOrder = position,
               addedPoints = 0
           };
           db.GradeAssignments.Add(newAssignment);
           db.SaveChanges();

           AbstractAssignmentActivity newActivity = new GradeActivity()
           {
               AbstractAssignmentID = newAssignment.ID,
               AbstractAssignment = newAssignment,
               Name = "Untitled",
               PointsPossible = newAssignment.PointsPossible

           };
           db.AbstractAssignmentActivity.Add(newActivity);
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
                               double temp = score.Points / score.AssignmentActivity.AbstractAssignment.PointsPossible;
                               if (temp < lowest)
                               {
                                   lowest = temp;
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
                   var studentScores = (from scores in db.Scores
                                        where scores.AssignmentActivity.AbstractAssignment.CategoryID == categoryId &&
                                        scores.Points >= 0
                                        select scores).GroupBy(s => s.UserProfileID);
                   
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

                   var activityQuery = from a in db.AbstractAssignmentActivity
                                       where a.AbstractAssignmentID == assignmentId
                                       select a;

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
       public ActionResult AddCategory()
       {
           var currentCourseId = ActiveCourse.CourseID;
           var numTabs = from cats in db.Categories
                         where cats.CourseID == currentCourseId
                         select cats;
           int colorCount = numTabs.Count();
           string color = null;
           string name = null;
           switch (colorCount)
           {
               case 0:
                   color = "#74FEF8";
                   name = "Category 1";
                   break;
               case 1:
                   color = "Tomato";
                   name = "Category 2";
                   break;
               case 2:
                   color = "LightBlue";
                   name = "Category 3";
                   break;
               case 3:
                   color = "SpringGreen";
                   name = "Category 4";
                   break;
               case 4:
                   color = "BurlyWood";
                   name = "Category 5";
                   break;
               case 5:
                   color = "Yellow";
                   name = "Category 6";
                   break;
               case 6:
                   color = "Plum";
                   name = "Category 7";
                   break;
               case 7:
                   color = "Orange";
                   name = "Category 8";
                   break;
               case 8:
                   color = "Pink";
                   name = "Category 9";
                   break;
               case 9:
                   color = "Purple";
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
               Course = ViewBag.ActiveCourse.Course,
               Points = 0,
               ColumnOrder = 0,
               Assignments = new List<AbstractAssignment>(),
               TabColor = color,
           };
           db.Categories.Add(newCategory);
           db.SaveChanges();
           Tab(newCategory.ID);

           Tab((int)Session["categoryId"]);
           return View("_Tabs");
       }

       [HttpPost]
       public ActionResult DeleteCategory(int categoryId)
       {
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
                                         where grade.AssignmentActivity.AbstractAssignmentID == assignmentId
                                         select grade).ToList();
                   var assignment = (from assigns in db.AbstractAssignments
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
       public ActionResult ModifyCell(double value, int userId, int assignmentId)
       {

           //Continue if we have a valid gradable ID
           if (assignmentId != 0)
           {
               var gradableQuery = from g in db.Scores
                                   join a in db.AbstractAssignments on g.AssignmentActivity.AbstractAssignmentID equals a.ID
                                   where g.UserProfileID == userId &&
                                   a.ID == assignmentId
                                   select g;

               var assignmentQuery = from a in db.AbstractAssignmentActivity
                                     where a.AbstractAssignmentID == assignmentId
                                     select a;

               var currentAssignment = assignmentQuery.FirstOrDefault();

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
                       AssignmentActivityID = currentAssignment.ID,
                       PublishedDate = DateTime.Now,
                       isDropped = false
                   };

                   db.Scores.Add(newScore);
                   db.SaveChanges();
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
                   case ColumnAction.ImportCSV:
                       
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
           //LINQ complains when we use this directly in our queries,so pull it beforehand
           int currentCourseId = ActiveCourse.CourseID;
           List<Score> percentList = new List<Score>();

           var letterGrades = from letters in db.Courses
                              where letters.ID == currentCourseId
                              select letters.LetterGrades;

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
                               score.isDropped == false && 
                               score.Points >= 0
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
                       PointsPossible = scores.perfectScore,
                       addedPoints = 0
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

           List<LetterGrade> letterGradeList = ((activeCourse.Course as Course).LetterGrades).ToList();

           ViewBag.Students = studentList;
           ViewBag.Scores = percentList;
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
           BuildGradebook((int)categoryId);
           if (Convert.ToInt32(Session["isTab"]) == 0)
           {
               return RedirectToRoute("Index");
           }
           return View();
       }


       public void AddLetterGrade(string grade, int minReq)
       {
           int currentCourseId = ActiveCourse.CourseID;

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
           int currentCourseId = ActiveCourse.CourseID;

           //List of students scores
           List<Score> studentScores = new List<Score>();

           //pull all weights (tabs) for the current course
           var cats = from category in db.Categories
                            where category.CourseID == currentCourseId
                            select category;

           List<Category> allCategories = cats.ToList();

           Category currentTab = (from c in allCategories where c.ID == categoryId select c).First();
           List<int> numDropped = new List<int>();
           numDropped.Add(currentTab.dropX);

           if (currentTab == null)
           {
               currentTab = (from c in allCategories select c).First();
           }

           //save to the session.  Needed later for AJAX-related updates.
           Session["CurrentCategoryId"] = currentTab.ID;

           //pull the gradables (columns) for the current weight (tab)
           List<AbstractAssignment> assignments = (from a in db.AbstractAssignments
                                                   where a.CategoryID == currentTab.ID
                                                   select a).ToList();

           if (assignments.Count() == 0)
           {
               AbstractAssignment newAssignment = new GradeAssignment()
               {
                   Name = "Untitled",
                   PointsPossible = 100,
                   AssignmentActivities = new List<AbstractAssignmentActivity>(),
                   CategoryID = categoryId,
                   ColumnOrder = 0,
                   addedPoints = 0
               };
               db.AbstractAssignments.Add(newAssignment);
               db.SaveChanges();

               AbstractAssignmentActivity newActivity = new GradeActivity()
               {
                   AbstractAssignmentID = newAssignment.ID,
                   Name = "Untitled",
                   PointsPossible = newAssignment.PointsPossible,
                   AbstractAssignment = newAssignment
                   /*ColumnOrder = 0,
                   Scores = new List<Score>()*/
               };
               db.AbstractAssignmentActivity.Add(newActivity);
               db.SaveChanges();
           }

           //Pull the gradeAssignments
           List<AbstractAssignment> gradeAssignments = (from ga in db.AbstractAssignments
                                                     where ga.CategoryID == currentTab.ID
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
           int currentCourseId = ActiveCourse.CourseID;

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

               GradeAssignment newAssignment = new GradeAssignment()
               {
                   Name = "Untitled",
                   PointsPossible = 100,
                   AssignmentActivities = new List<AbstractAssignmentActivity>(),
                   CategoryID = newCategory.ID,
                   ColumnOrder = 0,
                   addedPoints = 0
               };
               db.GradeAssignments.Add(newAssignment);
               db.SaveChanges();

               AbstractAssignmentActivity newActivity = new GradeActivity()
               {
                   AbstractAssignmentID = newAssignment.ID,
                   Name = "Untitled",
                   PointsPossible = newAssignment.PointsPossible,
                   AbstractAssignment = newAssignment
                   /*ColumnOrder = 0,
                   Scores = new List<Score>()*/
               };
               db.AbstractAssignmentActivity.Add(newActivity);
               db.SaveChanges();

               //with a new weight / gradable combo created, we can
               //call Index() to finish
               //off the rendering
               return newCategory.ID;
           }
       }
   }
}
