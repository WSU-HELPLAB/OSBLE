using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.AbstractCourses;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CommentCategoryController : WizardBaseController
    {
        private string previousButtonText = "LoadPreviousConfiguration";
        private string previousSelect = "PreviousSelect";

        public override string ControllerName
        {
            get { return "CommentCategory"; }
        }

        public override string PrettyName
        {
            get
            {
                return "Comment Categories";
            }
        }

        public override string ControllerDescription
        {
            get { return "The instructor can mark-up student submissions with annotations"; }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                prereqs.Add(new BasicsController());
                prereqs.Add(new TeamController());
                return prereqs;
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                List<AssignmentTypes> types = base.AllAssignmentTypes.ToList();
                types.Remove(AssignmentTypes.DiscussionAssignment);
                types.Remove(AssignmentTypes.TeamEvaluation);
                return types;
            }
        }

        private void BuildViewBag()
        {
            ViewBag.PreviousAssignments = (from assignment in db.Assignments
                                          where assignment.Category.CourseID == activeCourse.AbstractCourseID
                                          select assignment).ToList();
            ViewBag.PreviousAssignmentButton = previousButtonText;
            ViewBag.PreviousSelectName = previousSelect;
        }

        public override ActionResult Index()
        {
            base.Index();
            BuildViewBag();
            if (Assignment.CommentCategory == null)
            {
                Assignment.CommentCategory = new CommentCategoryConfiguration();
            }
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = db.Assignments.Find(model.ID);

            //are we loading a previous config?
            if (Request.Form.AllKeys.Contains(previousButtonText))
            {
                //prevent wizard advancement
                WasUpdateSuccessful = false;

                //load previous comment categories
                int assignmentId = 0;
                Int32.TryParse(Request.Form[previousSelect], out assignmentId);
                if (assignmentId != 0)
                {
                    Assignment oldAssignment = db.Assignments.Find(assignmentId);
                    Index();
                    Assignment.CommentCategory = oldAssignment.CommentCategory;
                    return View(Assignment);
                }
                else
                {
                    return Index();
                }
            }
            else
            {
                //normal postback
                CommentCategoryConfiguration config = BuildCommentCategories();
                Assignment.CommentCategory = config;
                Assignment.CommentCategoryID = 0;
                db.SaveChanges();
                WasUpdateSuccessful = true;
                return base.PostBack(Assignment);
            }
        }

        private CommentCategoryConfiguration BuildCommentCategories()
        {
            CommentCategoryConfiguration config = new CommentCategoryConfiguration();

            //all keys that we care about start with "category_"
            List<string> keys = (from key in Request.Form.AllKeys
                                 where key.Contains("category_")
                                 select key).ToList();

            //we know this one for sure so no need to loop
            config.Name = Request.Form["category_config_name"].ToString();

            //but the rest are variable, so we need to loop
            foreach (string key in keys)
            {
                //All category keys go something like "category_BLAH1_BLAH2_...".  Based on how
                //many underscores the current key has, we can determine what data it is
                //providing to us
                string[] pieces = key.Split('_');

                //length of 2 is a category name
                if (pieces.Length == 2)
                {
                    int catId = 0;
                    Int32.TryParse(pieces[1], out catId);

                    //does the comment category already exist?
                    CommentCategory category = GetOrCreateCategory(config, catId);
                    category.Name = Request.Form[key].ToString();
                }
                //length of 4 is a category option
                else if (pieces.Length == 4)
                {
                    int catId = 0;
                    int order = 0;
                    Int32.TryParse(pieces[2], out catId);
                    Int32.TryParse(pieces[3], out order);
                    CommentCategory category = GetOrCreateCategory(config, catId);
                    CommentCategoryOption option = new CommentCategoryOption();
                    option.Name = Request.Form[key].ToString();
                    category.Options.Insert(order, option);
                }
            }

            //when we're all done, zero out the category IDs to ensure that the items get
            //added to the DB correctly
            foreach (CommentCategory c in config.Categories)
            {
                c.ID = 0;
            }

            return config;
        }

        private CommentCategory GetOrCreateCategory(CommentCategoryConfiguration config, int categoryId)
        {
            //does the comment category already exist?
            CommentCategory category = (from c in config.Categories
                                        where c.ID == categoryId
                                        select c).FirstOrDefault();
            if (category == null)
            {
                category = new CommentCategory();
                category.ID = categoryId;
                config.Categories.Add(category);
            }
            return category;
        }
    }
}
