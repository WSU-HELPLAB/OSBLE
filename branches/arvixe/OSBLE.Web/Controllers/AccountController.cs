using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.Users;
using OSBLE.Utility;
using OSBLE.Attributes;
using OSBLE.Services;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Controllers
{
    public class AccountController : OSBLEController
    {
        public ActionResult ErrorPage()
        {
            return View();
        }

        public AccountController()
        {
            ViewBag.ReCaptchaPublicKey = getReCaptchaPublicKey();
        }

        //
        // GET: /Account/LogOn

        public ActionResult LogOn(string returnUrl = "")
        {
            setLogOnCaptcha();
            Assembly asm = Assembly.GetExecutingAssembly();
            if (asm.FullName != null)
            {
                AssemblyName assemblyName = new AssemblyName(asm.FullName);
                ViewBag.VersionNumber = assemblyName.Version.ToString();
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/LogOn

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                model.UserName = model.UserName.Trim();
                model.Password = model.Password.Trim();
                UserProfile localUser = db.UserProfiles.Where(m => m.UserName.CompareTo(model.UserName) == 0).FirstOrDefault();

                if (localUser != null)
                {
                    //do we have a valid password
                    if (UserProfile.ValidateUser(model.UserName, model.Password))
                    {
                        //is the user approved
                        if (localUser.IsApproved)
                        {
                            //log them in
                            OsbleAuthentication.LogIn(localUser);

                            //..then send them on their way
                            if (string.IsNullOrEmpty(returnUrl) == false)
                            {
                                return Redirect(returnUrl);
                            }
                            else
                            {
                                base.UpdateCacheInstance(localUser);
                                Cache["ActiveCourse"] = localUser.DefaultCourse;
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else
                        {
                            //...send an additional email verification
                            setLogOnCaptcha();
                            ModelState.AddModelError("", "This account has not been activated.  An additional verification letter has been sent to your email address.");
                            string randomHash = GenerateRandomString(40);
                            localUser.AuthenticationHash = randomHash;
                            db.SaveChanges();
                            sendVerificationEmail(true,
                                "https://osble.org" + Url.Action("ActivateAccount",
                                new { hash = randomHash }),
                                localUser.FirstName,
                                localUser.UserName,
                                randomHash
                                );
                        }

                    }
                    else
                    {
                        setLogOnCaptcha();
                        ModelState.AddModelError("", "The user name or password provided is incorrect.");
                    }
                }
                else
                {
                    setLogOnCaptcha();
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        /// <summary>
        /// Translates a webservice-based authentication key into an actual OSBLE web
        /// login.  Will redirect the user to the specified location.
        /// </summary>
        /// <param name="authToken">The auth token provided to you by the OSBLE web service</param>
        /// <param name="destinationUrl">The path in OSBLE that you'd like to visit.
        /// E.g. to get to the assignments page, you would pass in "/Assignment"
        /// </param>
        /// <returns></returns>
        public ActionResult TokenLogin(string authToken, string destinationUrl = "/")
        {
            AuthenticationService auth = new AuthenticationService();
            UserProfile profile = auth.GetActiveUser(authToken);
            if (profile == null || profile.UserName == null)
            {
                return RedirectToAction("LogOn", "Account");
            }
            OsbleAuthentication.LogIn(profile);

            //AC: Token logins are mostly commonly redirects to assignment pages, usually through external
            //services such as ChemProV.  Unfortunately, these redirect URLs don't work if the user
            //was last logged into a different course.  In these situations, we need to automatically
            //switch to the relevant assignment for the user.
            string[] pieces = destinationUrl.Split('/');

            //is the user trying to get to the assignment details page?
            if (pieces[pieces.Length - 2].ToLower().CompareTo("assignmentdetails") == 0)
            {
                int assignmentId = 0;

                //is the last nugget an assignment ID?
                if (Int32.TryParse(pieces[pieces.Length - 1], out assignmentId))
                {
                    Assignment destinationAssignment = db.Assignments.Where(a => a.ID == assignmentId).FirstOrDefault();
                    if (destinationAssignment != null)
                    {
                        if (destinationAssignment.CourseID != null)
                        {
                            HomeController hc = new HomeController();
                            hc.SetCourseID((int)destinationAssignment.CourseID);
                        }
                    }
                }
            }

            return Redirect(destinationUrl);
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            OsbleAuthentication.LogOut();
            context.Session.Clear(); // Clear session on signout.

            return RedirectToAction("Index", "Home");
        }

        //
        // POST: /Account/UpdateUserIdentification
        [OsbleAuthorize]
        [HttpPost]
        public ActionResult UpdateUserIdentification(string changeIdentification, string matchingIdentification)
        {
            bool changeIdentificationSucceeded = false;
            //verify matching changes
            bool matchingIdentifications = String.Equals(changeIdentification, matchingIdentification);

            //change identification
            if (!String.IsNullOrEmpty(changeIdentification) && matchingIdentifications)
            {
                try
                {
                    UserProfile currentUser = db.UserProfiles.Find(OsbleAuthentication.CurrentUser.ID);
                    //check to make sure the user is not changing their identification to one already existing in the db
                    UserProfile verificationUser =
                        db.UserProfiles.FirstOrDefault(vu => vu.Identification.Equals(changeIdentification) && vu.SchoolID == currentUser.SchoolID);
                    if (verificationUser == null)
                    {
                        if (currentUser != null)
                        {
                            //change and save identification
                            currentUser.Identification = changeIdentification;
                            db.Entry(currentUser).State = EntityState.Modified;
                            db.SaveChanges();
                            changeIdentificationSucceeded = true;
                        }    
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ChangeIdentification", "OSBLE was unable to change user identification. (Error saving changes)");
                }

            }

            if (changeIdentificationSucceeded)
            {
                return RedirectToAction("Profile");
            }
            else if (!matchingIdentifications)
            {
                ModelState.AddModelError("ChangeIdentification", "New identification and verification identification do not match!");
            }
            else
            {
                ModelState.AddModelError("ChangeIdentification", "OSBLE was unable to change user identification.");
            }
            //we were unable to update the user email
            return View("Profile");
        }

        //
        // POST: /Account/UpdateUserEmail
        [OsbleAuthorize]
        [HttpPost]
        public ActionResult UpdateUserEmail(string newEmail, string matchingEmail)
        {
            bool changeEmailSucceeded = false;
            //verify proper email syntax
            bool validEmail = new EmailAddressAttribute().IsValid(newEmail);
            //verify matching changes
            bool matchingEmails = String.Equals(newEmail, matchingEmail);

            if (validEmail && matchingEmails)
            {
                try
                {
                    UserProfile currentUser = db.UserProfiles.Find(OsbleAuthentication.CurrentUser.ID);
                    if (currentUser != null)
                    {
                        //TODO: check for matching email in the user profile db perhaps?

                        //change and save email
                        currentUser.UserName = newEmail;
                        db.Entry(currentUser).State = EntityState.Modified;
                        db.SaveChanges();
                        changeEmailSucceeded = true;
                    }

                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("NewEmail", "OSBLE was unable to change user email. (Error saving changes)");
                }
            }

            if (changeEmailSucceeded)
            {
                return RedirectToAction("Profile");
            }
            else if (!validEmail)
            {
                ModelState.AddModelError("NewEmail", "Please enter a valid email.");
            }
            else if (!matchingEmails)
            {
                ModelState.AddModelError("NewEmail", "New email and verification email do not match!");
            }
            else
            {
                ModelState.AddModelError("NewEmail", "OSBLE was unable to change user email.");
            }
            //we were unable to update the user email
            return View("Profile");
        }

        [OsbleAuthorize]
        [HttpPost]
        public ActionResult UpdateEmailSettings()
        {
            CurrentUser.EmailAllNotifications = Convert.ToBoolean(Request.Params["EmailallNotifications"]);
            CurrentUser.EmailAllActivityPosts = Convert.ToBoolean(Request.Params["EmailAllActivityPosts"]);
            CurrentUser.EmailNewDiscussionPosts = Convert.ToBoolean(Request.Params["EmailNewDiscussionPosts"]);

            db.Entry(CurrentUser).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Profile");
        }

        [OsbleAuthorize]
        [HttpPost]
        public ActionResult UpdateMenu()
        {
            foreach (CourseUser cu in currentCourses)
            {
                bool newHidden = Convert.ToBoolean(Request.Params["cu_hidden_" + cu.AbstractCourseID]);
                if (newHidden != cu.Hidden)
                {
                    cu.Hidden = newHidden;
                    db.Entry(cu).State = EntityState.Modified;
                }
                //yc if the users decided to withdraw from a course
                bool withdraw = Convert.ToBoolean(Request.Params["cu_withdraw_" + cu.AbstractCourseID]);
                if (withdraw)
                {
                    //user has checked this to remove themself from this course. leets update it!
                    WithdrawUserFromCourse(CurrentUser);
                }
            }

            // Update the default course ID for log in.
            UserProfile profile = db.UserProfiles.Find(CurrentUser.ID);
            profile.DefaultCourse = Convert.ToInt32(Request.Params["defaultCourse"]);
            db.Entry(profile).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Profile");
        }

        //
        // GET: /Account/ProfessionalRegister
        public ActionResult ProfessionalRegister()
        {
            ViewBag.ReCaptchaPublicKey = getReCaptchaPublicKey();
            return View();
        }

        public ActionResult AccountCreated()
        {
            return View();
        }

        public ActionResult ActivateAccount(string hash = "")
        {
            ViewBag.Hash = hash;
            LogOnModel model = new LogOnModel();
            model.Password = "foo";
            return View(model);
        }

        [HttpPost]
        public ActionResult ActivateAccount(LogOnModel model)
        {
            string hash = Request.Form["hash"];
            UserProfile user = db.UserProfiles.Where(up => up.UserName == model.UserName).FirstOrDefault();

            if (user != null)
            {

                if (user.IsApproved == false)
                {
                    //did the user request a new key?
                    if (Request.Form.AllKeys.Contains("newCode"))
                    {
                        //...send an additional email verification
                        setLogOnCaptcha();
                        ModelState.AddModelError("", "An additional verification letter has been sent to your email address.");
                        string randomHash = GenerateRandomString(40);
                        user.AuthenticationHash = randomHash;
                        db.SaveChanges();
                        sendVerificationEmail(true,
                            "https://osble.org" + Url.Action("ActivateAccount",
                            new { hash = randomHash }),
                            user.FirstName,
                            user.UserName,
                            randomHash
                            );
                    }
                    else
                    {
                        if (hash != null && hash != "")
                        {
                            if (user != null)
                            {
                                if ((user.AuthenticationHash as string) == hash)
                                {
                                    user.IsApproved = true;
                                    db.SaveChanges();
                                    OsbleAuthentication.LogIn(user);
                                    db.SaveChanges();

                                    return RedirectToAction("Index", "Home");
                                }
                                else
                                {
                                    ModelState.AddModelError("", "The authentication code \"" + hash + "\"");
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("", "A user with the specified email address does not exist the system.");
                            }
                        }
                    }
                }
                else
                {
                    //user is already authenticated
                    ModelState.AddModelError("", "This account has already been activated.");
                }
            }
            else
            {
                //user is not valid
                ModelState.AddModelError("", "A user with the specified email address does not exist the system.");
            }
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult ProfessionalRegister(RegisterModel model)
        {
            string privatekey = GetReCaptchaPrivateKey();

            //Fall through if ReCaptcha is not set up correctly
            if (privatekey == null)// || ReCaptcha.Validate(privateKey: privatekey))
            {
                model.School = (from c in db.Schools where c.Name == Constants.ProfessionalSchool select c).FirstOrDefault();
                model.SchoolID = model.School.ID;

                if (ModelState.IsValid)
                {
                    string randomHash = GenerateRandomString(40);
                    try
                    {
                        UserProfile profile = new UserProfile();
                        profile.Password = UserProfile.GetPasswordHash(model.Password);
                        profile.AuthenticationHash = randomHash;
                        profile.UserName = model.Email;
                        profile.FirstName = model.FirstName;
                        profile.LastName = model.LastName;
                        profile.Identification = model.Identification;
                        profile.SchoolID = model.SchoolID;
                        profile.School = db.Schools.Find(model.SchoolID);

                        db.UserProfiles.Add(profile);
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        return ProfessionalRegister();
                    }

                    sendVerificationEmail(
                        false,
                        "https://osble.org" + Url.Action("ActivateAccount", new { hash = randomHash }),
                        model.FirstName,
                        model.Email,
                        randomHash
                        );
                    return RedirectToAction("AccountCreated");
                }
                else
                {
                    ModelState.AddModelError("", "There was an error creating your account.  Please contact OSBLE support at support@osble.org");
                }

            }
            else
            {
                ModelState.AddModelError("", "The Captcha was incorrect try again.");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/AcademiaRegister

        public ActionResult AcademiaRegister()
        {
            ViewBag.ReCaptchaPublicKey = getReCaptchaPublicKey();
            ViewBag.SchoolID = new SelectList(from c in db.Schools
                                              where c.Name != Constants.ProfessionalSchool
                                              select c, "ID", "Name");

            var WTmodel = new RegisterModel
            {
                Email = Request.QueryString["email"]
                ,
                FirstName = Request.QueryString["firstname"]
                ,
                LastName = Request.QueryString["lastname"]
                ,
                Identification = Request.QueryString["identification"]
                ,
                SchoolID = Convert.ToInt32(Request.QueryString["schoolid"])
            };

            if (WTmodel != null)
                return View(WTmodel);
            else
                return View();



        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult AcademiaRegister(RegisterModel model)
        {
            string privatekey = GetReCaptchaPrivateKey();

            //Fall through if ReCaptcha is not set up correctly
            if (privatekey == null)// || ReCaptcha.Validate(privateKey: privatekey))
            {
                if (ModelState.IsValid)
                {
                    //used for email verification
                    string randomHash = GenerateRandomString(40);

                    //make sure that the user name isn't already taken
                    UserProfile takenName = db.UserProfiles.Where(u => u.UserName == model.Email).FirstOrDefault();
                    if (takenName != null)
                    {
                        //add an error message then get us out of here
                        ModelState.AddModelError("", "The email address provided is already associated with an OSBLE account.");
                        return AcademiaRegister();
                    }

                    takenName = (from d in db.UserProfiles
                                 where d.Identification == model.Identification &&
                                       d.SchoolID == model.SchoolID
                                 select d).FirstOrDefault();

                    if (takenName != null)
                    {
                        //add an error message then get us out of here
                        ModelState.AddModelError("", "The Student ID provided is already associated with an OSBLE account for the school you provided.");
                        return AcademiaRegister();
                    }

                    //try creating the account
                    UserProfile profile = new UserProfile();
                    try
                    {


                        profile.UserName = model.Email;
                        profile.Password = UserProfile.GetPasswordHash(model.Password);
                        profile.AuthenticationHash = randomHash;
                        profile.FirstName = model.FirstName;
                        profile.LastName = model.LastName;
                        profile.Identification = model.Identification;
                        profile.SchoolID = model.SchoolID;
                        profile.School = db.Schools.Find(model.SchoolID);

                        //AC: turned off email valiation as it didn't work a lot of the time
                        profile.IsApproved = true;

                        //check for stubs (accounts created through roster import function)
                        UserProfile up = db.UserProfiles.Where(c => c.SchoolID == profile.SchoolID && c.Identification == profile.Identification).FirstOrDefault();
                        if (up != null)
                        {
                            if (up.UserName == null) // Stub. Register to the account.
                            {
                                up.UserName = model.Email;
                                up.Password = UserProfile.GetPasswordHash(model.Password);
                                up.FirstName = model.FirstName;
                                up.LastName = model.LastName;
                                db.Entry(up).State = EntityState.Modified;
                            }
                        }
                        else // Profile does not exist.
                        {
                            db.UserProfiles.Add(profile);
                        }

                        //yc: profile made. Check white table for account information
                        List<WhiteTableUser> whitetableusers = db.WhiteTableUsers.Where(w => w.Identification == profile.Identification && w.SchoolID == profile.SchoolID).ToList();

                        if (whitetableusers.Count > 0)
                        {
                            foreach (WhiteTableUser wtu in whitetableusers)
                            {
                                //you have gathered all the users  time to add them to courses
                                //bug found, we need to have section location for now
                                //seetting them to 0 for now
                                //we have 
                                CourseUser newUser = new CourseUser();
                                newUser.UserProfile = profile;
                                newUser.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
                                newUser.AbstractCourseID = wtu.CourseID;
                                newUser.UserProfileID = profile.ID;
                                db.CourseUsers.Add(newUser);
                                db.WhiteTableUsers.Remove(wtu);
                            }
                        }

                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        FormsAuthentication.SignOut();
                        ModelState.AddModelError("", e.Message);

                        return AcademiaRegister();
                    }

                    /*
                    sendVerificationEmail(
                        true, "https://osble.org" +
                        Url.Action("ActivateAccount", new { hash = randomHash }),
                        profile.FirstName,
                        profile.UserName,
                        randomHash
                        );
                    */
                    return RedirectToAction("AccountCreated");
                }
            }
            else
            {
                ModelState.AddModelError("", "The Captcha was incorrect try again.");
            }

            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name");

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePassword

        [OsbleAuthorize]
        public ActionResult Profile()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword


        [OsbleAuthorize]
        [HttpPost]
        public ActionResult Profile(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded;
                try
                {
                    UserProfile currentUser = db.UserProfiles.Find(OsbleAuthentication.CurrentUser.ID);
                    if (currentUser != null)
                    {
                        currentUser.Password = UserProfile.GetPasswordHash(model.NewPassword);
                        db.Entry(currentUser).State = EntityState.Modified;
                        db.SaveChanges();
                        changePasswordSucceeded = true;
                    }
                    else
                    {
                        changePasswordSucceeded = false;
                    }
                }
                catch (Exception ex)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePasswordSuccess

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }

        public ActionResult ResetPassword()
        {
            ViewBag.ReCaptchaPublicKey = getReCaptchaPublicKey();
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            string privatekey = GetReCaptchaPrivateKey();

            //Fall through if ReCaptcha is not set up correctly
            if (privatekey == null)// || ReCaptcha.Validate(privateKey: privatekey))
            {
                UserProfile osbleProfile = db.UserProfiles.Where(m => m.UserName.CompareTo(model.EmailAddress) == 0).FirstOrDefault();
                if (osbleProfile != null)
                {
                    string newPass = GenerateRandomString(10);
                    osbleProfile.Password = UserProfile.GetPasswordHash(newPass);
                    db.SaveChanges();
#if !DEBUG
                    string body = "Your OSBLE password has been reset.\n Your new password is: " + newPass + "\n\nPlease change this password as soon as possible.";
                    List<MailAddress> to = new List<MailAddress>();
                    to.Add(new MailAddress(osbleProfile.UserName));

                    Email.Send("[OSBLE] Password Reset Request", body, to);
#endif
                    return View("ResetPasswordSuccess");

                }
            }
            return View("ResetPasswordFailure");
        }

        [HttpPost]
        public ActionResult FindUsername(OSBLE.Models.FindUserNameModel model)
        {
            UserProfile osbleProfile = null;
            List<UserProfile> PossibleProfiles = new List<UserProfile>();
            PossibleProfiles = db.UserProfiles.Where(m => m.School.Name.CompareTo(model.SchoolName) == 0).ToList();

            if (PossibleProfiles != null)
            {
                foreach (UserProfile possibleUser in PossibleProfiles)
                {
                    if (possibleUser.Identification == (model.SchoolID.ToString()))
                    {
                        osbleProfile = possibleUser;
                    }
                    if (osbleProfile != null)
                    {
                        string username = osbleProfile.UserName;
                        return PartialView("FindUsernameSuccess", username);
                    }
                }
            }
            return View("FindUsernameFailure");
        }

        public ActionResult FindUsername()
        {
            List<School> schools = new List<School>();
            FindUserNameModel getSchools = new FindUserNameModel();
            schools = (from u in db.Schools select u).ToList();

            if (schools != null)
            {
                getSchools.Schools = new List<string>();
                foreach (School thisSchool in schools)
                {
                    getSchools.Schools.Add(thisSchool.Name);
                }
            }

            return View(getSchools);
        }

        [OsbleAuthorize]
        [HttpPost]
        public ActionResult UploadPicture(HttpPostedFileBase file)
        {
            if (Request.Params["remove"] != null) // Delete Picture
            {
                // We just set the profile image to null and save
                UserProfile profile = db.UserProfiles.Where(u => u.ID == CurrentUser.ID).FirstOrDefault();
                profile.SetProfileImage(null);
                db.SaveChanges();
                return RedirectToAction("Profile");
            }

            if (file != null) // Upload Picture
            {
                Bitmap image;
                try
                {
                    image = new Bitmap(file.InputStream);
                }
                catch
                {   // If image format is invalid, discard it.
                    image = null;
                }

                if (image != null)
                {
                    int thumbSize = 50;

                    // Crop image to a square.
                    int square = Math.Min(image.Width, image.Height);
                    using (Bitmap cropImage = new Bitmap(square, square))
                    {
                        using (Bitmap finalImage = new Bitmap(thumbSize, thumbSize))
                        {
                            Graphics cropGraphics = Graphics.FromImage(cropImage);
                            Graphics finalGraphics = Graphics.FromImage(finalImage);

                            // Center cropped image horizontally, leave at the top vertically. (better focus on subject)
                            cropGraphics.DrawImage(image, -(image.Width - cropImage.Width) / 2, 0);

                            // Convert to thumbnail.
                            finalGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                            finalGraphics.DrawImage(cropImage,
                                                new Rectangle(0, 0, thumbSize, thumbSize),
                                                new Rectangle(0, 0, square, square),
                                                GraphicsUnit.Pixel);
                            UserProfile profile = db.UserProfiles.Where(u => u.ID == CurrentUser.ID).FirstOrDefault();
                            profile.SetProfileImage(finalImage);
                            db.SaveChanges();
                        }
                    }
                }
            }
            return RedirectToAction("Profile");
        }

        public ActionResult ContactUs()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ContactUs(ContactUsModel model)
        {
            string privatekey = GetReCaptchaPrivateKey();

            //Fall through if ReCaptcha is not set up correctly
            if (privatekey == null)// || ReCaptcha.Validate(privateKey: privatekey))
            {
                if (ModelState.IsValid)
                {
                    //craft & send the email
                    string subject = "[OSBLE] Support Request from " + model.Name;
                    string body = model.Message;
                    body += "<br />reply to: " + model.Email;
                    List<MailAddress> to = new List<MailAddress>();
                    to.Add(new MailAddress("support@osble.org"));
                    Email.Send(subject, body, to);
                    ViewBag.CUName = model.Name;
                    return View("ContactUsSuccess");
                }
            }
            else
            {
                ModelState.AddModelError("", "The Captcha was incorrect try again.");
            }
            return View(model);
        }

        public bool ThumbnailCallback()
        {
            return false;
        }

        #region Status Codes

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }

        #endregion Status Codes

        private string getReCaptchaPublicKey()
        {
            string key = null;
            if (ConfigurationManager.AppSettings.AllKeys.Contains("RecaptchaPublicKey"))
            {
                key = ConfigurationManager.AppSettings["RecaptchaPublicKey"];
            }
            return key;
        }

        private string GetReCaptchaPrivateKey()
        {
            string key = null;
            if (ConfigurationManager.AppSettings.AllKeys.Contains("RecaptchaPrivateKey"))
            {
                key = ConfigurationManager.AppSettings["RecaptchaPrivateKey"];
            }
            return key;
        }

        private string GenerateRandomString(int size)
        {
            Random random = new Random();
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }

        private void setLogOnCaptcha()
        {
            int attempts = 0;
            try
            {
                attempts = (int)Session["attempts"];
            }
            catch { }

            Session["attempts"] = ++attempts;
            if (attempts > 3)
            {
                ViewBag.UseCaptcha = true;
                ViewBag.ReCaptchaPublicKey = getReCaptchaPublicKey();
            }
            else
            {
                ViewBag.UseCaptcha = false;
            }
        }

        /// <summary>
        /// Only if we are in debug does it get activated right away
        /// </summary>
        /// <returns>returns true if in debug mode else returns false</returns>
        private bool isActivited()
        {
#if Debug
                return true;
#endif

            return false;
        }

        private void sendVerificationEmail(bool acedemia, string link, string firstName, string to, string hashCode)
        {
            string subject = "Welcome to OSBLE";

            string message = "Dear " + firstName + @",<br/>
            <br/>
            Thank you for creating an account at osble.org. Before you can log in, you must activate your 
            account by <a href='" + link + @"'>visiting this link</a>.  Alternatively, you can visit the url: " + link + @"
            and enter the code &quot;" + hashCode + @"&quot;. 
            <br/>
            <br/>
            ";
            if (acedemia)
            {
                message += @"If you are a teacher and would like to use OSBLE for one or more of your courses, please e-mail support@osble.org to request an instructor account.  In the e-mail, please include the name of your school and brief descriptions of the courses you'd like to teach using OSBLE.<br/>
                <br/>
                ";
            }
            message += @"Best regards,<br/>
            The OSBLE Team in the <a href='www.helplab.org'>HELP lab</a> at <a href='www.wsu.edu'>Washington State University</a>";

            Email.Send(subject, message, new List<MailAddress>() { new MailAddress(to) });
        }

        //yc: Need to make a view and controller removing oneself from the course.
        //initalie link click
        [OsbleAuthorize]
        public ActionResult WithdrawFromCourse()
        {

            WithdrawUserFromCourse(CurrentUser);
            return RedirectToAction("Index", "Home");
        }



    }
}