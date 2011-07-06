using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using OSBLE.Models.Courses.Rubrics;
using System.Web;

using OSBLE.Models;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;
using System.Collections.ObjectModel;
using OSBLE.Models.Services.Rubric;
using OSBLE.Models.AbstractCourses;
using System.Data.Entity.Infrastructure;

namespace OSBLE.Services
{
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class RubricService
    {
        [OperationContract]
        public void DoWork()
        {
            // Add your operation implementation here
            return;
        }

        [OperationContract]
        public CellDescription GetFakeCellDescription()
        {
            return new CellDescription();
        }

        [OperationContract]
        public Criterion GetFakeCriterion()
        {
            return new Criterion();
        }

        [OperationContract]
        public Level GetFakeLevel()
        {
            return new Level();
        }

        [OperationContract]
        public Rubric GetFakeRubric()
        {
            return new Rubric();
        }

        [OperationContract]
        public List<AbstractCourse> GetCourses()
        {
            OSBLEContext db = new OSBLEContext();
            var result = from ac in db.AbstractCourses
                                             join cu in db.CoursesUsers on ac.ID equals cu.CourseID
                                             /*
                                             where
                                                (cu.CourseRole.CanModify || ac is Community)
                                                &&
                                                cu.UserProfileID == userId
                                              * */
                                             select new { Course = (AbstractCourse)ac };
            List<AbstractCourse> retVal = new List<AbstractCourse>();
            foreach (var item in result.ToList())
            {
                retVal.Add(item.Course);
            }
            return retVal;
        }

        /// <summary>
        /// Returns a list of rubrics to be used with the course.  Note that the rubrics only
        /// contain a name and an ID.  To obtain a full rubric, please use GetRubric()
        /// </summary>
        /// <param name="course"></param>
        /// <returns></returns>
        [OperationContract]
        public ObservableCollection<Rubric> GetRubricsForCourse(SimpleCourse course)
        {
            OSBLEContext db = new OSBLEContext();
            db.Configuration.LazyLoadingEnabled = false;
           
            List<Rubric> rubrics = new List<Rubric>();

            var result = from cr in db.CourseRubrics
                         join r in db.Rubrics on cr.RubricID equals r.ID
                         where cr.CourseID == course.CourseID
                         select r;
            rubrics = result.ToList();
            ObservableCollection<Rubric> finalRubrics = new ObservableCollection<Rubric>();
            foreach (Rubric r in rubrics)
            {
                Rubric miniRubric = new Rubric();
                miniRubric.ID = r.ID;
                miniRubric.Description = r.Description;
                finalRubrics.Add(r);
            }
            return finalRubrics;
        }

        /// <summary>
        /// Returns a key value pair of courses and their associated rubrics
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        public Dictionary<SimpleCourse, ObservableCollection<Rubric>> GetRubricList()
        {
            //pull the current user
            OSBLEContext db = new OSBLEContext();
            UserProfile currentUser = ValidateUser();
            if (currentUser == null)
            {
                //return new Dictionary<SimpleCourse, ObservableCollection<Rubric>>();
            }
            
            //pull all courses in which the current user has modify access or is associated
            //with a community
            //int userId = currentUser.ID;
            var result = from cr in db.CourseRubrics
                         join ac in db.AbstractCourses on cr.CourseID equals ac.ID
                         join cu in db.CoursesUsers on ac.ID equals cu.CourseID
                         join ru in db.Rubrics on cr.RubricID equals ru.ID
                         /*
                         where
                            (cu.CourseRole.CanModify || ac is Community)
                            &&
                            cu.UserProfileID == userId
                          * */
                         select new {Course = ac, Rubric = ru };

            //create the necessary return dictionary
            Dictionary<SimpleCourse, ObservableCollection<Rubric>> courseRubrics = new Dictionary<SimpleCourse, ObservableCollection<Rubric>>();
            foreach (var item in result.ToList())
            {
                SimpleCourse simpleCourse = new SimpleCourse();
                simpleCourse.CourseID = item.Course.ID;
                simpleCourse.Name = item.Course.Name;
                if (!courseRubrics.ContainsKey(simpleCourse))
                {
                    courseRubrics.Add(simpleCourse, new ObservableCollection<Rubric>());
                }
                if (!courseRubrics[simpleCourse].Contains(item.Rubric))
                {
                    courseRubrics[simpleCourse].Add(item.Rubric);
                }
            }
            return courseRubrics;
        }

        /// <summary>
        /// Will create a new rubric or save an existing rubric based on the supplied information.  
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="rubric"></param>
        /// <param name="descriptions"></param>
        /// <returns></returns>
        [OperationContract]
        public int SaveRubric(int courseId, Rubric rubric)
        {
            OSBLEContext db = new OSBLEContext();

            //pull the current user
            UserProfile currentUser = ValidateUser();
            
            //no user = no save
            if (currentUser == null)
            {
                return 0;
            }

            //If the rubric has an ID greater than 0, then we must be saving changes to an existing
            //rubric.  When this is the case, it's easier to just delete the existing one from the DB
            //and create a new one.
            if (rubric.ID > 0)
            {
                Rubric r = new Rubric();
                r.ID = rubric.ID;
                db.Rubrics.Attach(r);
                db.Rubrics.Remove(r);
                db.SaveChanges();
            }

            //the rubric sent over the wire has several caveats:
            // 1: Levels and Criteria aren't associated with the rubrics.
            // 2: Cell descriptions aren't tied to to Levels and Criteria.  Instead, the ID
            //    fields in the cell descriptions point to the IDs of their respective
            //    Levels & Criteria.  
            //
            //What this means: we need to tweak the rubric so that it is structured in a way
            //that matches the expectations of our DB.
            //***

            //start building links for the cell descriptions
            foreach (CellDescription desc in rubric.CellDescriptions)
            {
                //use LINQ to find the referenced object
                Criterion crit = (from c in rubric.Criteria
                                  where c.ID == desc.CriterionID
                                  select c).FirstOrDefault();
                
                if (crit != null)
                {
                    desc.Criterion = crit;
                }

                Level level = (from l in rubric.Levels
                               where l.ID == desc.LevelID
                               select l).FirstOrDefault();
                if(level != null)
                {
                    desc.Level = level;
                }

                //zero out the IDs so that the DB will give us new ones
                desc.CriterionID = 0;
                desc.LevelID = 0;
            }

            //do something similar for criteria and levels
            foreach (Level l in rubric.Levels)
            {
                l.Rubric = rubric;
                l.ID = 0;
            }
            foreach (Criterion crit in rubric.Criteria)
            {
                crit.Rubric = rubric;
                crit.RubricID = 0;
            }

            //finally, make sure that the rubric's ID is 0 as well
            rubric.ID = 0;

            //now, save to the DB
            db.Rubrics.Add(rubric);
            db.SaveChanges();

            //with that saved, we can create a rubric / course assocation
            CourseRubric cr = new CourseRubric();
            cr.RubricID = rubric.ID;
            cr.CourseID = courseId;
            db.CourseRubrics.Add(cr);
            db.SaveChanges();

            return rubric.ID;
        }

        /// <summary>
        /// Determines whether or not the current user is authenticated
        /// </summary>
        /// <returns></returns>
        private UserProfile ValidateUser()
        {
            //pull the current user
            HttpContext context = HttpContext.Current;
            OSBLEContext db = new OSBLEContext();
            UserProfile currentUser = (from u in db.UserProfiles
                                       where u.UserName == context.User.Identity.Name
                                       select u).FirstOrDefault();
            return currentUser;
        }

    }
}
