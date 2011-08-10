using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using OSBLE.Models.Wizard;
using OSBLE.Models.Assignments;

namespace OSBLE.Controllers.Assignments.Wizard
{
    public abstract class WizardBaseController : OSBLEController, IComparable
    {
        /// <summary>
        /// Returns the controller's name.  Must be unique.  The GET method should
        /// return a static string.
        /// </summary>
        public abstract string ControllerName { get; }

        /// <summary>
        /// Returns a brief description of the controller's purpose.  Used mainly on the "Start"
        /// page
        /// </summary>
        public abstract string ControllerDescription { get; }

        /// <summary>
        /// Returns a list of WizardBaseControllers that must preceed the current controller.  
        /// For example, most controllers will expect an assignment to have at least a name.  The
        /// "Basics" controller handles setting up assignment basics and so other controllers should
        /// list "Basics" as being a prerequisite.
        /// </summary>
        public abstract ICollection<WizardBaseController> Prerequisites { get; }

        public StudioAssignment Assignment { get; set; }

        public bool WasUpdateSuccessful { get; set; }

        public WizardBaseController()
        {
            WasUpdateSuccessful = true;
        }

        private string BuildViewPath(string action)
        {
            string name = ControllerName.Replace("Controller", "");
            return string.Format("{0}/{1}", name, action);
        }

        public ActionResult Index()
        {
            object model = IndexAction();
            return View(BuildViewPath("Index"), model);
        }

        public ActionResult Index(HttpRequestBase request)
        {
            object modifiedModel = IndexActionPostback(request);
            return View(BuildViewPath("Index"), modifiedModel);
        }

        /// <summary>
        /// Put all code needed for your index action into this method
        /// </summary>
        /// <param name="assignmentId"></param>
        protected abstract object IndexAction();

        /// <summary>
        /// Put all code needed for your index postbacks into this method.
        /// </summary>
        /// <param name="model"></param>
        protected abstract object IndexActionPostback(HttpRequestBase request);

        public int CompareTo(object obj)
        {
            return this.ToString().CompareTo(obj.ToString());   
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.ControllerName, this.ControllerDescription);
        }
    }
}
