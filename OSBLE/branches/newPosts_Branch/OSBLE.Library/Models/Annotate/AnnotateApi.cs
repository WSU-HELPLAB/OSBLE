using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using OSBLE.Models.Assignments;
using OSBLE.Models.Users;
using System.Web.Helpers;
using System.Web.Script.Serialization;

namespace OSBLE.Models.Annotate
{
    public class AnnotateApi
    {
        public string ApiKey { get; set; }
        public string ApiUser { get; set; }

        public AnnotateApi(string apiUser, string apiKey)
        {
            ApiKey = apiKey;
            ApiUser = apiUser;
        }

        public AnnotateResult ToggleCommentVisibility(int criticalReviewAssignmentID, int authorTeamID, bool makeVisible)
        {
            AnnotateResult result = new AnnotateResult();
            Assignment criticalReview;
            result.Result = ResultCode.ERROR;

            string webResult = "";
            long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            string apiKey = GenerateAnnotateKey("listNotes.php", ApiUser, epoch);
            WebClient client = new WebClient();

            using(OSBLEContext db = new OSBLEContext())
            {
                criticalReview = db.Assignments.Find(criticalReviewAssignmentID);

                //step 1: find all people reviewing the document
                var query = (from rt in db.ReviewTeams
                                               .Include("CourseUser")
                                               .Include("CourseUser.UserProfile")
                             where rt.AssignmentID == criticalReviewAssignmentID
                             && rt.AuthorTeamID == authorTeamID
                             select rt.ReviewingTeam)
                             .SelectMany(t => t.TeamMembers)
                             .Distinct();
                List<TeamMember> reviewers = query.ToList();

                //step 2: get all comments for each reviewer on the team
                AnnotateResult documentResult = UploadDocument((int)criticalReview.PrecededingAssignmentID, authorTeamID);
                string rawNoteUrl = "http://helplab.org/annotate/php/listNotes.php?" +
                                 "api-user={0}" +           //Annotate admin user name (see web config)
                                 "&api-requesttime={1}" +   //UNIX timestamp
                                 "&api-annotateuser={2}" +  //the current user (reviewer)
                                 "&api-auth={3}" +          //Annotate admin auth key
                                 "&d={4}" +                 //document date
                                 "&c={5}";                  //document code
                string noteUrl = "";
                foreach (TeamMember reviewer in reviewers)
                {
                    //always refresh our epoch and api key
                    epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                    apiKey = GenerateAnnotateKey("listNotes.php", reviewer.CourseUser.UserProfile.UserName, epoch);
                    noteUrl = string.Format(rawNoteUrl,
                                               ApiUser,
                                               epoch,
                                               reviewer.CourseUser.UserProfile.UserName,
                                               apiKey,
                                               documentResult.DocumentDate,
                                               documentResult.DocumentCode
                                               );
                    webResult = client.DownloadString(noteUrl);
                    dynamic jsonResult = Json.Decode(webResult);
                }

            }
            return result;
        }

        /// <summary>
        /// Sends a document that resides on OSBLE to the Annotate server.
        /// If the document already exists on annotate's servers, this function will not resubmit
        /// unless forceUpload is set to TRUE.
        /// </summary>
        /// <param name="assignmentID">The assignment that the document belongs to</param>
        /// <param name="authorTeamID">The document's author</param>
        /// <param name="forceUpload">If set to TRUE, will force a document upload to annotate's servers</param>
        /// <returns></returns>
        public AnnotateResult UploadDocument(int assignmentID, int authorTeamID, bool forceUpload = false)
        {
            bool needsUpload = true;
            AnnotateResult result = new AnnotateResult();
            result.Result = ResultCode.ERROR;

            //By default, we only upload documents to annotate if they haven't been uploaded 
            //already.  This can be overridden by setting forceUpload = true.
            if (forceUpload == false)
            {
                //check for existing document
                using (OSBLEContext db = new OSBLEContext())
                {
                    string docString = GetAnnotateDocumentName(assignmentID, authorTeamID);
                    AnnotateDocumentReference code = (from c in db.AnnotateDocumentReferences
                                                      where c.OsbleDocumentCode.CompareTo(docString) == 0
                                                      select c
                                                 ).FirstOrDefault();
                    if (code == null)
                    {
                        needsUpload = true;
                    }
                    else
                    {
                        needsUpload = false;
                        result.Result = ResultCode.OK;
                        result.DocumentDate = code.AnnotateDocumentDate;
                        result.DocumentCode = code.AnnotateDocumentCode;
                    }
                }
            }

            if (needsUpload)
            {
                long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                WebClient client = new WebClient();
                string sendResult = "";

                //Submit document to annotate
#if DEBUG
                string documentUrl = "https://osble.org/content/icershort.pdf";
#else
                string documentUrl = "https://osble.org/FileHandler/GetAnnotateDocument?assignmentID={0}&authorTeamID={1}&apiKey={2}";
                documentUrl = string.Format(documentUrl, assignmentID, authorTeamID, ApiKey);
#endif

                string apiKey = GenerateAnnotateKey("uploadDocument.php", ApiUser, epoch);
                string uploadString = "http://helplab.org/annotate/php/uploadDocument.php?" +
                                      "api-user={0}" +           //Annotate admin user name (see web config)
                                      "&api-requesttime={1}" +   //UNIX timestamp
                                      "&api-annotateuser={2}" +  //the current user (reviewer)
                                      "&api-auth={3}" +          //Annotate admin auth key
                                      "&url={4}";                //URL of the document to upload
                uploadString = string.Format(uploadString,
                                             ApiUser,
                                            epoch,
                                            ApiUser,
                                            apiKey,
                                            System.Web.HttpUtility.UrlEncode(documentUrl)
                                             );
                try
                {
                    sendResult = client.DownloadString(uploadString);
                }
                catch (Exception ex)
                {
                    result.RawMessage = ex.Message;
                    result.Result = ResultCode.ERROR;
                    return result;
                }
                string documentCode = "";
                string documentDate = "";

                result.RawMessage = sendResult;
                if (sendResult.Substring(0, 2) == "OK")
                {
                    result.Result = ResultCode.OK;
                    string[] pieces = sendResult.Split(' ');
                    documentDate = pieces[1];
                    documentCode = pieces[2];
                    result.DocumentCode = documentCode;
                    result.DocumentDate = documentDate;

                    //add DB entry into OSBLE so that we know something's been sent
                    using (OSBLEContext db = new OSBLEContext())
                    {
                        string docString = GetAnnotateDocumentName(assignmentID, authorTeamID);
                        AnnotateDocumentReference code = (from c in db.AnnotateDocumentReferences
                                                          where c.OsbleDocumentCode.CompareTo(docString) == 0
                                                          select c
                                                     ).FirstOrDefault();
                        if (code == null)
                        {
                            code = new AnnotateDocumentReference();
                            db.AnnotateDocumentReferences.Add(code);
                        }
                        else
                        {
                            db.Entry(code).State = System.Data.EntityState.Modified;
                        }
                        code.AnnotateDocumentCode = result.DocumentCode;
                        code.AnnotateDocumentDate = result.DocumentDate;
                        code.OsbleDocumentCode = docString;
                        db.SaveChanges();
                    }
                }

            }
            return result;
        }

        /// <summary>
        /// Creates an account for the specified OSBLE user.  If the user already exists
        /// on the annotate server, nothing will happen.  Any new user is created as an
        /// "annotating" user, which means that they can annotate a document, but cannot
        /// upload documents of thier own.
        /// </summary>
        /// <param name="osbleUser"></param>
        /// <returns></returns>
        public AnnotateResult CreateAccount(UserProfile osbleUser)
        {
            AnnotateResult result = new AnnotateResult();
            string webResult = "";
            long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            WebClient client = new WebClient();

            //create annotate account for user
            string apiKey = GenerateAnnotateKey("createAccount.php", osbleUser.UserName, epoch);
            string createString = "http://helplab.org/annotate/php/createAccount.php?" +
                                 "api-user={0}" +           //Annotate admin user name (see web config)
                                 "&api-requesttime={1}" +   //UNIX timestamp
                                 "&api-annotateuser={2}" +  //the current user (reviewer)
                                 "&api-auth={3}" +          //Annotate admin auth key
                                 "&licensed=0 " +
                                 "&firstname={4}" +         //User's first name
                                 "&lastname={5}";           //User's last name
            createString = string.Format(createString,
                                        ApiUser,
                                        epoch,
                                        osbleUser.UserName,
                                        apiKey,
                                        osbleUser.FirstName,
                                        osbleUser.LastName
                                        );
            webResult = client.DownloadString(createString);

            //downgrade user to unlicensed if not OSBLE admin
            if (osbleUser.IsAdmin == false)
            {
                apiKey = GenerateAnnotateKey("updateAccount.php", osbleUser.UserName, epoch);
                string updateString = "http://helplab.org/annotate/php/updateAccount.php?" +
                                     "api-user={0}" +           //Annotate admin user name (see web config)
                                     "&api-requesttime={1}" +   //UNIX timestamp
                                     "&api-annotateuser={2}" +  //the current user (reviewer)
                                     "&api-auth={3}" +          //Annotate admin auth key
                                     "&licensed=0 ";
                updateString = string.Format(updateString,
                                            ApiUser,
                                            epoch,
                                            osbleUser.UserName,
                                            apiKey
                                            );
                webResult = client.DownloadString(updateString);
                result.RawMessage = webResult;
            }
            result.RawMessage = webResult;
            if (webResult.Substring(0, 2) == "OK")
            {
                result.Result = ResultCode.OK;
            }
            else
            {
                result.Result = ResultCode.ERROR;
            }
            return result;
        }

        /// <summary>
        /// Gives the given OSBLE user access to the annotate document identified by the
        /// supplied docCode and docString.
        /// </summary>
        /// <param name="osbleUser"></param>
        /// <param name="docCode"></param>
        /// <param name="docDate"></param>
        /// <returns></returns>
        public AnnotateResult GiveAccessToDocument(UserProfile osbleUser, string docCode, string docDate)
        {
            AnnotateResult result = new AnnotateResult();
            result.Result = ResultCode.ERROR;
            string webResult = "";
            long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            WebClient client = new WebClient();

            //give user access to the new document
            string apiKey = GenerateAnnotateKey("authorizeReader.php", osbleUser.UserName, epoch);
            string authorizeString = "http://helplab.org/annotate/php/authorizeReader.php?" +
                                 "api-user={0}" +           //Annotate admin user name (see web config)
                                 "&api-requesttime={1}" +   //UNIX timestamp
                                 "&api-annotateuser={2}" +  //the current user (reviewer)
                                 "&api-auth={3}" +          //Annotate admin auth key
                                 "&d={4}" +                 //document upload date
                                 "&c={5}";                  //document code
            authorizeString = string.Format(authorizeString,
                                        ApiUser,
                                        epoch,
                                        osbleUser.UserName,
                                        apiKey,
                                        docDate,
                                        docCode
                                        );
            webResult = client.DownloadString(authorizeString);
            result.RawMessage = webResult;
            if (webResult.Substring(0, 2) == "OK")
            {
                result.Result = ResultCode.OK;
            }
            return result;
        }

        /// <summary>
        /// Generates a URL to annotate that will automatically log in the specified user
        /// and open the annotate document identified by the supplied docCode and docDate
        /// </summary>
        /// <param name="osbleUser"></param>
        /// <param name="docCode"></param>
        /// <param name="docDate"></param>
        /// <returns></returns>
        public string GetAnnotateLoginUrl(UserProfile osbleUser, string docCode, string docDate)
        {
            //log user into annotate
            long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            string apiKey = GenerateAnnotateKey("loginAs.php", osbleUser.UserName, epoch);
            string loginString = "http://helplab.org/annotate/php/loginAs.php?" +
                                 "api-user={0}" +           //Annotate admin user name (see web config)
                                 "&api-requesttime={1}" +   //UNIX timestamp
                                 "&loc=pdfnotate.php?{2}" + //redirect to annotate server
                                 "&remember=1" +          //store user info in cookie
                                 "&errloc=http://helplab.org/annotate/php/error.php" +
                                 "&api-annotateuser={3}" +  //the current user (reviewer)
                                 "&api-auth={4}";           //Annotate admin auth key (see web config)

            loginString = string.Format(loginString,
                                        ApiUser,
                                        epoch,
                                        System.Web.HttpUtility.UrlEncode(string.Format("d={0}&c={1}&nobanner=1", docDate, docCode)),
                                        osbleUser.UserName,
                                        apiKey
                                        );
            return loginString;
        }

        /// <summary>
        /// Generates an key for use with annotate
        /// </summary>
        /// <param name="phpFunction">The function that we're calling</param>
        /// <param name="annotateUser">The user that is responsible for the call.  For example, if we want to log in as bob@smith.com, we'd send "bob@smith.com".</param>
        /// <returns></returns>
        public string GenerateAnnotateKey(string phpFunction, string annotateUser, long unixEpoch)
        {
            if (annotateUser == null || annotateUser.Length == 0)
            {
                annotateUser = ApiUser;
            }
            string apiUser = ApiUser;

            //build our string, convert into bytes for sha1
            string rawString = string.Format("{0}\n{1}\n{2}\n{3}", phpFunction, apiUser, unixEpoch, annotateUser);
            byte[] rawBytes = Encoding.UTF8.GetBytes(rawString);

            //build our hasher
            byte[] seed = Encoding.UTF8.GetBytes(ApiKey);
            HMACSHA1 sha1 = new HMACSHA1(seed);

            //hash our bytes
            byte[] hashBytes = sha1.ComputeHash(rawBytes);

            string hashString = Convert.ToBase64String(hashBytes).Replace("\n", "");
            return hashString;
        }

        /// <summary>
        /// Used in annotate to build a unique string for each document in OSBLE
        /// </summary>
        /// <param name="assignmentID">The assignment on which the submission took place (NOT THE CRITICAL REVIEW)</param>
        /// <param name="authorTeamID">The team that submitted the document</param>
        /// <returns></returns>
        public static string GetAnnotateDocumentName(int assignmentID, int authorTeamID)
        {
            using (OSBLEContext db = new OSBLEContext())
            {
                Assignment assignment = db.Assignments.Find(assignmentID);
                AssignmentTeam assignmentTeam = (from at in db.AssignmentTeams
                                                 where at.AssignmentID == assignment.ID
                                                 &&
                                                 at.TeamID == authorTeamID
                                                 select at
                                                 ).FirstOrDefault();
                string fileName = string.Format(
                    "{0}-{1}-{2}-{3}",
                    assignment.CourseID,
                    assignment.ID,
                    assignmentTeam.TeamID,
                    assignment.Deliverables[0].ToString()
                    );
                return fileName;
            }
        }

        /// <summary>
        /// Sets the anonymity settings for the given document.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="docCode"></param>
        /// <param name="docDate"></param>
        /// <param name="isAnonymous"></param>
        /// <param name="anonName"></param>
        /// <returns></returns>
        public AnnotateResult SetDocumentAnonymity(UserProfile user, string docCode, string docDate, CriticalReviewSettings settings, string anonName = "anonymous")
        {
            AnnotateResult result = new AnnotateResult();
            result.Result = ResultCode.ERROR;
            WebClient client = new WebClient();
            string webResult = "";
            long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            string apiKey = GenerateAnnotateKey("apiAddUserMapping.php", user.UserName, epoch);

            Dictionary<string, string> mapping = new Dictionary<string, string>();
            mapping["other"] = "anonymous";
            mapping[user.UserName] = anonName;
            string jsonMapping = (new JavaScriptSerializer()).Serialize(mapping);
            int enable = 1;
            if (settings.AnonymizeComments == false)
            {
                enable = 0;
            }
            string anonString = "http://helplab.org/annotate/php/apiAddUserMapping.php?" +
                                 "api-user={0}" +           //Annotate admin user name (see web config)
                                 "&api-auth={1}" +          //Annotate admin auth key
                                 "&api-requesttime={2}" +   //UNIX timestamp
                                 "&api-annotateuser={3}" +  //current user (not used?)
                                 "&date={4}" +              //date for document
                                 "&code={5}" +              //code for document
                                 "&mapping={6}" +           //anonymous mapping
                                 "&enable={7}";             //enable or disable mapping
                                 //"&rm={8}";                //add or remove mapping

            anonString = string.Format(anonString,
                                        ApiUser,
                                        apiKey,
                                        epoch,
                                        user.UserName,
                                        docDate,
                                        docCode,
                                        jsonMapping,
                                        enable
                                        //""
                                        );
            webResult = client.DownloadString(anonString);
            result.RawMessage = webResult;
            if (webResult.Substring(0, 2) == "OK")
            {
                result.Result = ResultCode.OK;
            }
            return result;
        }
    }
}
