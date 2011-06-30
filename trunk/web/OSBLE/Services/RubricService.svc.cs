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
        public Dictionary<AbstractCourse, IEnumerable<Rubric>> GetRubricList()
        {
            //pull the current user
            HttpContext context = HttpContext.Current;
            OSBLEContext db = new OSBLEContext();
            UserProfile currentUser = (from u in db.UserProfiles
                                       where u.UserName == context.User.Identity.Name
                                       select u).FirstOrDefault();
            if (currentUser == null)
            {
                return new Dictionary<AbstractCourse, IEnumerable<Rubric>>();
            }
            
            //pull all courses in which the current user has modify access or is associated
            //with a community
            int userId = currentUser.ID;
            var result = from cr in db.CourseRubrics
                         join ac in db.AbstractCourses on cr.CourseID equals ac.ID
                         join cu in db.CoursesUsers on ac.ID equals cu.CourseID
                         join ru in db.Rubrics on cr.RubricID equals ru.ID
                         where
                            (cu.CourseRole.CanModify || ac is Community)
                            &&
                            cu.UserProfileID == userId
                         select new {Course = ac, Rubric = ru };

            //create the necessary return dictionary
            Dictionary<AbstractCourse, IEnumerable<Rubric>> courseRubrics = new Dictionary<AbstractCourse, IEnumerable<Rubric>>();
            foreach (var item in result.ToList())
            {
                if (!courseRubrics.ContainsKey(item.Course))
                {
                    //courseRubrics.Add(item, new List<Rubric>());
                }
            }


            return new Dictionary<AbstractCourse, IEnumerable<Rubric>>();
        }

        // Add more operations here and mark them with [OperationContract]
    }
}
