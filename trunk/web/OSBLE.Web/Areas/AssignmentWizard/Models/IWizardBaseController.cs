using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public enum WizardNavButtons
    {
        PreviousButton,
        NextButton,
        SaveAndExitButton,
        ResetFormButton,
        QuickNavButton
    }

    public interface IWizardBaseController : IComparable, INotifyPropertyChanged, IEquatable<IWizardBaseController>
    {
        /// <summary>
        /// Returns the controller's name.  Must be unique.  The GET method should
        /// return a static string.
        /// </summary>
        string ControllerName { get; }

        /// <summary>
        /// Returns the controller's pretty name.  By default, it's just the same as ControllerName.
        /// However, if you wanted to do something more fancy (ex: display 'foobar' as 'foo bar') you could 
        /// do that here.
        /// </summary>
        string PrettyName { get; }

        /// <summary>
        /// To be used by the WizardComponentManager to aid in component sorting.  It's okay to access
        /// the number if need be, but try not to set the value unless you know what you're doing.
        /// </summary>
        int SortOrder { get; set; }

        /// <summary>
        /// Returns a brief description of the controller's purpose.  Used mainly on the "Start"
        /// page
        /// </summary>
        string ControllerDescription { get; }

        /// <summary>
        /// Returns a concrete WizardBaseController that must preceed the current controller.  
        /// For example, most controllers will expect an assignment to have at least a name.  The
        /// "Basics" controller handles setting up assignment basics and so other controllers should
        /// list "Basics" as being a prerequisite.
        /// </summary>
        IWizardBaseController Prerequisite { get; }

        /// <summary>
        /// UI.  Whether or not the current component is selected by the user in the Assignment Wizard
        /// </summary>
        bool IsSelected {get; set;}

        /// <summary>
        /// UI Whether or not the current component is required during assignment creation.  
        /// By default, no component is required.  To make component required, override this 
        /// property in your subclass.
        /// </summary>
        bool IsRequired {get;}

        ActionResult Index();

        ActionResult QuickNav();
    }
}