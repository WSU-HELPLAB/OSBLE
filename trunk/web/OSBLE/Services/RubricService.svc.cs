using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using OSBLE.Models.Courses.Rubrics;
namespace OSBLE.Services
{
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
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

        // Add more operations here and mark them with [OperationContract]
    }
}
