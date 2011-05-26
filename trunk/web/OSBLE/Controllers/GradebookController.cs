using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class GradebookController : OSBLEController
    {
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
        [HttpPost]
        public ActionResult AddColumn(string colunmName, int pointsPossible, int position)
        {
            int weightId = Convert.ToInt32(Session["CurrentWeightID"]);
            Gradable g = new Gradable()
            {
                Position = position,
                PossiblePoints = pointsPossible,
                WeightID = weightId,
                Name = colunmName,
            };
            db.Gradables.Add(g);
            db.SaveChanges();
            return Json("complete");
        }

        /// <summary>
        /// Updates a preexisting column (gradable)
        /// </summary>
        /// <param name="gradableId">The ID of the gradable to be updated</param>
        /// <param name="colunmName">The gradables new column name</param>
        /// <param name="pointsPossible">The total points possible for the gradable</param>
        /// <param name="position">The relative position of the gradable with respect to other gradables for the current Weight</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult EditColumn(int gradableId, string colunmName, int pointsPossible, int position)
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
                    return Json("complete");
                }
                else
                {
                    return Json("error");
                }
            }
            return Json("error");
        }

        /// <summary>
        /// Deletes a column (gradable) from the current table
        /// </summary>
        /// <param name="gradableId">The ID of the gradable to remove</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DeleteColumn(int gradableId)
        {
            Gradable gradable = db.Gradables.Find(gradableId);
            db.AbstractGradables.Remove(gradable);
            db.SaveChanges();
            return Json("complete");
        }

        /// <summary>
        /// This will initialize the page using the supplied weightId (tab).
        /// </summary>
        /// <param name="weightId">The weight to load</param>
        /// <returns></returns>
        public ActionResult Index(int? weightId)
        {
            if (weightId == null)
            {
                weightId = GetDefaultWeightId();
            }

            //LINQ complains when we use this directly in our queries, so pull it beforehand
            int currentCourseId = ActiveCourse.CourseID;

            //pull all weights (tabs) for the current course
            var weights = from weight in db.Weights
                         where weight.CourseID == currentCourseId && weight.ID == weightId
                         select weight;
            List<Weight> allWeights = weights.ToList();
            Weight currentTab = (from w in allWeights where w.ID == weightId select w).First();
            
            //save to the session.  Needed later for AJAX-related updates.
            Session["CurrentWeightID"] = currentTab.ID;

            //pull the gradables (columns) for the current weight (tab)
            List<Gradable> gradables = (from g in db.Gradables
                                        where g.WeightID == currentTab.ID
                                        select g).ToList();

            //pull the students in the course.  Each student is a row.
            List<UserProfile> students = (from up in db.UserProfiles
                                         join cu in db.CoursesUsers on up.ID equals cu.UserProfileID
                                         where cu.CourseID == currentCourseId && cu.CourseRoleID == (int)CourseRole.OSBLERoles.Student
                                         select up).ToList();

            //Finally the scores for each student.
            List<GradableScore> scores = (from gs in db.GradableScores
                                          where gs.Gradable.WeightID == currentTab.ID
                                          select gs).ToList();
            
            //save everything that we need to the viewebag
            ViewBag.Tabs = allWeights;
            ViewBag.Gradables = gradables;
            ViewBag.Grades = scores;
            ViewBag.Users = students;
            return View();
        }

        /// <summary>
        /// This will initialize the page using the first weightId found in the database.  If
        /// No weightId exists, a new one will be created
        /// </summary>
        /// <returns></returns>
        public int GetDefaultWeightId()
        {
            //LINQ complains when we use this directly in our queries, so pull it beforehand
            int currentCourseId = ActiveCourse.CourseID;

            //By default, select the first tab
            var weightQuery = from weight in db.Weights
                              where weight.CourseID == currentCourseId
                              orderby weight.Position ascending
                              select weight;
            
            //if something was found, complete the query and get the first weight listed
            if (weightQuery.Count() > 0)
            {
                Weight weight = weightQuery.First();
                return weight.ID;
            }
            //ELSE: create a new tab and an anitial gradable
            else
            {

                Weight newWeight = new Weight()
                {
                    Name = "Untitled",
                    CourseID = currentCourseId,
                    Course = ViewBag.ActiveCourse.Course,
                    Points = 0,
                    Position = 0,
                    Gradables = new List<AbstractGradable>()
                };
                db.Weights.Add(newWeight);
                db.SaveChanges();

                Gradable newGradable = new Gradable()
                {
                    Name = "Untitled",
                    PossiblePoints = 0,
                    GradableScores = new List<GradableScore>(),
                    WeightID = newWeight.ID,
                    Position = 0
                };
                db.Gradables.Add(newGradable);
                db.SaveChanges();

                //with a new weight / gradable combo created, we can call Index() to finish
                //off the rendering
                return newWeight.ID;
            }
        }
    }
}
