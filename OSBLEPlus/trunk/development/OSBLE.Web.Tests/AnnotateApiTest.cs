using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLE.Models.Annotate;


namespace OSBLE.UnitTests
{
    [TestClass]
    public class AnnotateApiTest
    {
        //TODO: change these to use appsettings... not sure why it isn't working right now.
        //testing annotation 
        private const string AnnotateURL = "http://plus.osble.org:8088";
        private const string ApiUser = "support@osble.org";
        private const string username = ApiUser;
        private const string ApiKey = ""; // need to include test key here, do not check in.
        private const string first = "bob";
        private const string last = "smith";
        private const bool OsbleUserAdmin = false;

        //initialize test instance of AnnotateApi
        private AnnotateApi annotateApi = new AnnotateApi(ApiUser, ApiKey);

        [TestMethod]
        public void AnnotateApi_ToggleCommentVisibilityTest()
        {
           
        }

        [TestMethod]
        public void CreateAccount()
        {
            AnnotateResult result = new AnnotateResult();
            string webResult = "";
            long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            WebClient client = new WebClient();

            string apiKey = annotateApi.GenerateAnnotateKey("createAccount.php", username, epoch);
            
            string createString = AnnotateURL + "/annotate/php/createAccount.php?" +
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
                                        username,
                                        apiKey,
                                        first,
                                        last
                                        );
            webResult = client.DownloadString(createString);

            //downgrade user to unlicensed if not OSBLE admin
            if (OsbleUserAdmin == false)
            {
                apiKey = annotateApi.GenerateAnnotateKey("updateAccount.php", username, epoch);
                
                string updateString = AnnotateURL + "/annotate/php/updateAccount.php?" +
                                     "api-user={0}" +           //Annotate admin user name (see web config)
                                     "&api-requesttime={1}" +   //UNIX timestamp
                                     "&api-annotateuser={2}" +  //the current user (reviewer)
                                     "&api-auth={3}" +          //Annotate admin auth key
                                     "&licensed=0 ";
                updateString = string.Format(updateString,
                                            ApiUser,
                                            epoch,
                                            username,
                                            apiKey
                                            );
                try
                {
                    webResult = client.DownloadString(updateString);
                    result.RawMessage = webResult;
                }
                catch (Exception)
                {

                    throw;
                }

            }
            result.RawMessage = webResult;

            Assert.AreEqual(webResult.Substring(0, 2), "OK");
            
        }

        [TestMethod]
        public void UploadDocument()
        {

            AnnotateResult result = new AnnotateResult();
            result.Result = ResultCode.ERROR;

            long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            WebClient client = new WebClient();
            string sendResult = "";

            //Submit document to annotate

            const string documentUrl = "http://osble.org/content/icershort.pdf";

            string apiKey = annotateApi.GenerateAnnotateKey("uploadDocument.php", ApiUser, epoch);
            
            string uploadString = AnnotateURL + "/annotate/php/uploadDocument.php?" +
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
                string temp = "";
                for (int i = 0; i < pieces.Length; i++)
                {
                    if (i > 2)
                    {
                        temp = temp + "\n " + pieces[i];
                    }
                }
            }

            Assert.AreEqual(ResultCode.OK, result.Result);
        }

        [TestMethod]
        public void GetAnnotateLoginUrl()
        {
            string docCode = "IPAeBEy5";
            string docDate = "2015-08-21";

            //log user into annotate
            long epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            string apiKey = annotateApi.GenerateAnnotateKey("loginAs.php", username, epoch);
            
            string loginString = AnnotateURL + "/annotate/php/loginAs.php?" +
                                 "api-user={0}" +           //Annotate admin user name (see web config)
                                 "&api-requesttime={1}" +   //UNIX timestamp
                                 "&loc=pdfnotate.php?{2}" + //redirect to annotate server
                                 "&remember=1" +          //store user info in cookie
                                 "&errloc=" + AnnotateURL + "/annotate/php/error.php" +
                                 "&api-annotateuser={3}" +  //the current user (reviewer)
                                 "&api-auth={4}";           //Annotate admin auth key (see web config)

            loginString = string.Format(loginString,
                                        ApiUser,
                                        epoch,
                                        System.Web.HttpUtility.UrlEncode(string.Format("d={0}&c={1}&nobanner=1", docDate, docCode)),
                                        username,
                                        apiKey
                                        );

            Assert.IsNotNull(loginString);
            
        }
    }
}
