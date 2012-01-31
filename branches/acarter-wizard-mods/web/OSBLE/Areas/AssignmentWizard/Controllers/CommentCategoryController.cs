using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.AbstractCourses;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CommentCategoryController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "CommentCategory"; }
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
                return base.AllAssignmentTypes;
            }
        }

        public override ActionResult Index()
        {
            base.Index();
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
            CommentCategoryConfiguration config = BuildCommentCategories();
            Assignment.CommentCategory = config;
            Assignment.CommentCategoryID = 0;
            db.SaveChanges();
            WasUpdateSuccessful = true;
            return base.Index(Assignment);
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
