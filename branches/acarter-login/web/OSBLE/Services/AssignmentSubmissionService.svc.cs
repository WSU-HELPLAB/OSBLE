using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Collections.Generic;
using System.Web;
using System.IO;
using OSBLE.Models.Users;
using OSBLE.Controllers;

namespace OSBLE.Services
{
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class AssignmentSubmissionService
    {
        private AuthenticationService authService = new AuthenticationService();

        /// <summary>
        /// AC: Needs testing.
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="data"></param>
        /// <param name="authToken"></param>
        [OperationContract]
        public void SubmitAssignment(int assignmentId, byte[][] data, string authToken)
        {
            if (authService.IsValidKey(authToken))
            {
                UserProfile currentUser = authService.GetActiveUser(authToken);

                //turn byte data into HttpPostedFileBase for file submission
                List<HttpPostedFileBase> files = new List<HttpPostedFileBase>();
                foreach (byte[] file in data)
                {
                    /*
                    HttpPostedFile assignmentFile = new HttpPostedFile();
                    StreamWriter writer = new StreamWriter(assignmentFile.InputStream);
                    writer.Write(file);
                    assignmentFile.InputStream.Position = 0;
                     * */
                }

                SubmissionController submission = new SubmissionController();
                submission.Create(assignmentId, files, currentUser);
            }
        }
    }
}
