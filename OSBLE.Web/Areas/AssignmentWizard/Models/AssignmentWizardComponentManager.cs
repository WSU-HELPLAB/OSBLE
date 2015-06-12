using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentWizard.Controllers;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Caching;
using OSBLE.Utility;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.IO;
using OSBLE.Models.Users;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    //TODO: Properly evaluate the change to FileCache (see note below)
    //AC: We were having issues with the selected assignment type randomly defaulting to "Basic."  Previously,
    //    I had been using JS cookies to track component settings.  I think that this might have been part of
    //    the problem so I converted the WCM over fo the FileCache.  However, the conversion was mostly a
    //    replacement of Cookie[value] to Cache[value].  This might need to be revisited in a future date.
    [Serializable]
    public class AssignmentWizardComponentManager : WizardComponentManagerBase
    {
        protected const string assignmentKey = "ComponentManagerStudioAssignmentKey";
        protected const string activeAssignmentTypeKey = "_wcm_activeAssignmentType";
        protected const string isNewAssignmentKey = "_wcm_isNewAssignment";

        protected override string CacheRegion
        {
            get { return "AssignmentWizardComponentManager"; }
        }

        protected override string WizardComponentNamespace
        {
            get { return "OSBLE.Areas.AssignmentWizard.Controllers"; }
        }

        #region constructor

        public AssignmentWizardComponentManager(UserProfile profile)
            : base(profile)
        {
        }

        #endregion

        #region properties

        public bool IsNewAssignment
        {
            get
            {
                if (ManagerCache[isNewAssignmentKey] != null)
                {
                    return Convert.ToBoolean(ManagerCache[isNewAssignmentKey]);
                }
                else
                {
                    ManagerCache[isNewAssignmentKey] = true.ToString();
                    return true;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    ManagerCache[isNewAssignmentKey] = value.ToString();
                }
            }
        }

        public int ActiveAssignmentId
        {
            get
            {
                if (ManagerCache[assignmentKey] != null)
                {
                    return Convert.ToInt32(ManagerCache[assignmentKey]);
                }
                else
                {
                    ManagerCache[assignmentKey] = "0";
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    ManagerCache[assignmentKey] = value.ToString();
                }
            }
        }

        public AssignmentTypes ActiveAssignmentType
        {
            get
            {
                if (ManagerCache[activeAssignmentTypeKey] != null)
                {
                    return (AssignmentTypes)Convert.ToInt32(ManagerCache[activeAssignmentTypeKey]);
                }
                else
                {
                    ManagerCache[activeAssignmentTypeKey] = ((int)AssignmentTypes.Basic).ToString();
                    return AssignmentTypes.Basic;
                }
            }
            private set
            {
                if (HttpContext.Current != null)
                {
                    ManagerCache[activeAssignmentTypeKey] = ((int)value).ToString();
                }
            }
        }

        #endregion

        #region public methods

        public ICollection<WizardBaseController> GetComponentsForAssignmentType(AssignmentTypes type)
        {
            return AllComponents
                .Cast<WizardBaseController>()
                .Where(a => a.ValidAssignmentTypes.Contains(type))
                .ToList();
        }

        /// <summary>
        /// Sets the active assignment type by trying to match the supplied parameter with possible assignment types
        /// listed in the AssignmentTypes enumeration.  Will default to AssignmentTypes.Basic if no match was found.
        /// </summary>
        /// <param name="assignmentType"></param>
        /// <returns>True if a good match was found, false otherwise.</returns>
        public bool SetActiveAssignmentType(string assignmentType)
        {
            IList<AssignmentTypes> possibleTypes = Assignment.AllAssignmentTypes;
            foreach (AssignmentTypes type in possibleTypes)
            {
                if (assignmentType == type.ToString())
                {
                    return SetActiveAssignmentType(type);
                }
            }

            //default to basic
            SetActiveAssignmentType(AssignmentTypes.Basic);
            return false;
        }

        /// <summary>
        /// Sets the active assignment type based on the supplied parameter.
        /// </summary>
        /// <param name="assignmentType"></param>
        /// <returns>Always true</returns>
        public bool SetActiveAssignmentType(AssignmentTypes assignmentType)
        {
            ActiveAssignmentType = assignmentType;
            return true;
        }

        #endregion
    }
}