using OSBLE.Areas.AssessmentWizard.Controllers;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Models.Assessments;
using OSBLE.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Areas.AssessmentWizard.Models
{
    public class AssessmentWizardComponentManager : WizardComponentManagerBase
    {
        protected const string _isNewAssessmentKey = "_awcm_isNewAssessment";
        protected const string _assessmentKey = "_awcm_assessmentKey";
        protected const string _activeAssessmentTypeKey = "_awcm_activeAssessmentTypeKey";

        protected override string CacheRegion
        {
            get { return "AssessmentWizardComponentManager"; }
        }

        protected override string WizardComponentNamespace
        {
            get { return "OSBLE.Areas.AssessmentWizard.Controllers"; }
        }

        public AssessmentWizardComponentManager(UserProfile profile)
            : base(profile)
        {
        }

        #region properties

        public bool IsNewAssessment
        {
            get
            {
                if (ManagerCache[_isNewAssessmentKey] != null)
                {
                    return Convert.ToBoolean(ManagerCache[_isNewAssessmentKey]);
                }
                else
                {
                    ManagerCache[_isNewAssessmentKey] = true.ToString();
                    return true;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    ManagerCache[_isNewAssessmentKey] = value.ToString();
                }
            }
        }

        public int ActiveAssessmentId
        {
            get
            {
                if (ManagerCache[_assessmentKey] != null)
                {
                    return Convert.ToInt32(ManagerCache[_assessmentKey]);
                }
                else
                {
                    ManagerCache[_assessmentKey] = "0";
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    ManagerCache[_assessmentKey] = value.ToString();
                }
            }
        }

        public AssessmentType ActiveAssessmentType
        {
            get
            {
                if (ManagerCache[_activeAssessmentTypeKey] != null)
                {
                    return (AssessmentType)Convert.ToInt32(ManagerCache[_activeAssessmentTypeKey]);
                }
                else
                {
                    ManagerCache[_activeAssessmentTypeKey] = ((int)AssessmentType.AggregateAssessment).ToString();
                    return AssessmentType.AggregateAssessment;
                }
            }
            private set
            {
                if (HttpContext.Current != null)
                {
                    ManagerCache[_activeAssessmentTypeKey] = ((int)value).ToString();
                }
            }
        }

        #endregion

        #region public methods

        public ICollection<AssessmentBaseController> GetComponentsForAssignmentType(AssessmentType type)
        {
            return AllComponents
                .Cast<AssessmentBaseController>()
                .Where(a => a.ValidAssessmentTypes.Contains(type))
                .ToList();
        }

        /// <summary>
        /// Sets the active assignment type by trying to match the supplied parameter with possible assignment types
        /// listed in the AssignmentTypes enumeration.  Will default to AssignmentTypes.Basic if no match was found.
        /// </summary>
        /// <param name="assignmentType"></param>
        /// <returns>True if a good match was found, false otherwise.</returns>
        public bool SetActiveAssessmentType(string assessmentType)
        {
            IList<AssessmentType> possibleTypes = Assessment.AllAssessmentTypes;
            foreach (AssessmentType type in possibleTypes)
            {
                if (assessmentType == type.ToString())
                {
                    return SetActiveAssessmentType(type);
                }
            }

            //default to basic
            SetActiveAssessmentType(AssessmentType.CommitteeDiscussion);
            return false;
        }

        /// <summary>
        /// Sets the active assignment type based on the supplied parameter.
        /// </summary>
        /// <param name="assignmentType"></param>
        /// <returns>Always true</returns>
        public bool SetActiveAssessmentType(AssessmentType assessmentType)
        {
            ActiveAssessmentType = assessmentType;
            return true;
        }

        #endregion
    }
}