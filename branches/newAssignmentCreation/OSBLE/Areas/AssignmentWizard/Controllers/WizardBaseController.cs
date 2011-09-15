using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using OSBLE.Models.Assignments;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Remoting;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Controllers.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public abstract class WizardBaseController : OSBLEController, IComparable
    {
        public static string previousWizardButton = "PreviousWizardButton";
        public static string nextWizardButton = "NextWizardButton";
        private WizardComponentManager manager;

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

        public Assignment Assignment { get; set; }

        public bool WasUpdateSuccessful { get; set; }

        public WizardBaseController()
        {
            WasUpdateSuccessful = true;
            ViewBag.PreviousWizardButton = WizardController.previousWizardButton;
            ViewBag.NextWizardButton = WizardController.nextWizardButton;
            ViewBag.Title = "Assignment Creation Wizard";
        }

        public virtual ActionResult Index()
        {
            Assignment = new Assignment();
            manager = WizardComponentManager.GetInstance();

            if (manager.ActiveAssignmentId != 0)
            {
                Assignment = db.Assignments.Find(manager.ActiveAssignmentId);
            }
            else
            {
                Assignment = new Assignment();
            }
            return View();
        }

        /*
        [HttpPost]
        public ActionResult Index(dynamic model)
        {
            if (WasUpdateSuccessful)
            {
                //update the assignment ID.  Probably not necessary when working
                //with existing assignments, but needed for new assignments.
                manager.ActiveAssignmentId = Assignment.ID;

                string errorPath = "";
                WizardComponent comp = null;
                if (Request.Form.AllKeys.Contains(previousWizardButton))
                {
                    comp = manager.GetPreviousComponent();
                    errorPath = "Start";
                }
                else
                {
                    comp = manager.GetNextComponent();
                    errorPath = "WizardSummary";
                }

                if (comp != null)
                {
                    return RedirectToRoute("AssignmentWizard", new { controller = manager.ActiveComponent.Name });
                }
                else
                {
                    return RedirectToRoute("AssignmentWizard", new { controller = errorPath });
                }
            }

            return View(modifiedModel);
        }
         * */

        public int CompareTo(object obj)
        {
            return this.ToString().CompareTo(obj.ToString());   
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.ControllerName, this.ControllerDescription);
        }


        #region scheduled_for_removal

        /*
        /// <summary>
        /// Uses reflection to inject values from a web request into the supplied data
        /// store.
        /// </summary>
        /// <param name="store">The object that will store the results from the web request</param>
        /// <returns>A modified store with updated values</returns>
        protected dynamic ParseFormValues(dynamic store)
        {
            Type storeType = store.GetType();
            
            //pull all properties
            foreach (PropertyInfo property in storeType.GetProperties())
            {
                //get all attributes for the current property.  We're mostly interested in
                //the MVC3-related attributes of [Key] and [Required]
                object[] attributes = property.GetCustomAttributes(false);
                
                //check the web request to see if we have a value for the current property
                if (Requeset.Form.AllKeys.Contains(property.Name))
                {
                    //AC: At some point we may want to examine other attributes to validate our data
                    //    more rigorously (e.g. check string length, max value, etc.) but right
                    //    now, I'm just in test implementation mode.  This kind of error checking
                    //    should probably be done in the ParseDynamicProperty method.
                    BindPropertyToValue(store, property, Requeset.Form[property.Name]);
                }
                else if (attributes.Where(m => m is RequiredAttribute).Count() > 0 && property.GetValue(store, null) == null)
                {
                    //if it wasn't found and it was required, then add a model state error
                    RequiredAttribute attr = attributes.Where(m => m is RequiredAttribute).First() as RequiredAttribute;
                    ModelState.AddModelError(property.Name, attr.ErrorMessage);
                }
            }

            return store;
        }
         * 
         * /// <summary>
        /// Will attempt to cast the supplied value into the correct data type
        /// as defined by the supplied property
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private void BindPropertyToValue(dynamic store, PropertyInfo propertyInfo, dynamic value)
        {
            if (propertyInfo.PropertyType == typeof(Int32))
            {
                int retVal = 0;
                Int32.TryParse(value, out retVal);
                propertyInfo.SetValue(store, retVal, null);
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                propertyInfo.SetValue(store, value.ToString(), null);
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                bool retVal = false;
                bool.TryParse(value, out retVal);
                propertyInfo.SetValue(store, retVal, null);
            }
            else if (propertyInfo.PropertyType == typeof(DateTime))
            {
                DateTime retVal = DateTime.MinValue;
                DateTime.TryParse(value, out retVal);
                propertyInfo.SetValue(store, retVal, null);
            }
            else
            {
                //AC: Often, our model will have some sort of other object that it's referring to.
                //    In these cases, the view will only reference the [Key] attribute of these
                //    extra models.  We can kind of cheat and look at the current object's properties
                //    to see if any [Key]s exist.  If they do, set the value.  If not, play it safe
                //    and return null
                Type propertyType = propertyInfo.PropertyType;
                ObjectHandle handle = Activator.CreateInstance(propertyInfo.PropertyType.Assembly.FullName, propertyInfo.PropertyType.FullName);
                dynamic propertyAsObject = handle.Unwrap();
                foreach (PropertyInfo subProperty in propertyType.GetProperties())
                {
                    if (subProperty.GetCustomAttributes(typeof(KeyAttribute), false).Count() > 0)
                    {
                        BindPropertyToValue(propertyAsObject, subProperty, value);
                    }
                }
                propertyInfo.SetValue(store, propertyAsObject, null);
                
                //No [Key] attributes found.  Return null.
                //property = null;
            }
        }
        */
        #endregion

    }
}
