﻿using System;
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
using Microsoft.Web.Helpers;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Utility;
using OSBLE.Attributes;

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

        public ActionResult LogOn()
        {
            setLogOnCaptcha();
            Assembly asm = Assembly.GetExecutingAssembly();
            if (asm.FullName != null)
            {
                AssemblyName assemblyName = new AssemblyName(asm.FullName);
                ViewBag.VersionNumber = assemblyName.Version.ToString();
            }
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
                    //they have a user name, but not a .net user name.  This is an oddity.  Correct and continue.
                    if (localUser.AspNetUserName == null)
                    {
                        localUser.AspNetUserName = localUser.UserName;
                        db.SaveChanges();
                    }

                    //AC: if the user doesn't have a password, it's probably because they have an old .NET account.  
                    //find this information and then update their profile with their password
                    if (localUser.Password.Length == 0)
                    {
                        if (Membership.ValidateUser(localUser.AspNetUserName, model.Password))
                        {
                            localUser.Password = UserProfile.GetPasswordHash(model.Password);
                            db.SaveChanges();
                        }
                    }

                    //do we have a valid password
                    if (UserProfile.ValidateUser(model.UserName, model.Password))
                    {
                        //is the user approved
                        if (localUser.IsApproved)
                        {
                            //log them in
                            OsbleAuthentication.LogIn(localUser);

                            //..then send them on their way
                            if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                            && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                            {
                                return Redirect(returnUrl);
                            }
                            else
                            {
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
                            sendVerificationEmail(true, "https://osble.org" + Url.Action("ActivateAccount", new { hash = randomHash }), localUser.FirstName, localUser.UserName);
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

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            OsbleAuthentication.LogOut();
            context.Session.Clear(); // Clear session on signout.

            return RedirectToAction("Index", "Home");
        }

        [OsbleAuthorize]
        [HttpPost]
        public ActionResult UpdateEmailSettings()
        {
            CurrentUser.EmailAllNotifications = Convert.ToBoolean(Request.Params["EmailallNotifications"]);
            CurrentUser.EmailAllActivityPosts = Convert.ToBoolean(Request.Params["EmailAllActivityPosts"]);

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
            }

            // Update the default course ID for log in.
            CurrentUser.DefaultCourse = Convert.ToInt32(Request.Params["defaultCourse"]);
            db.Entry(CurrentUser).State = EntityState.Modified;
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

        public ActionResult ActivateAccount(string hash)
        {
            if (hash == null)
            {
                throw new Exception("Hash cannot be null");
            }
            ViewBag.Hash = hash;
            LogOnModel model = new LogOnModel();
            model.Password = "foo";
            return View(model);
        }

        [HttpPost]
        public ActionResult ActivateAccount(LogOnModel model)
        {
            string hash = Request.Params["hash"];

            if (hash != null && hash != "")
            {
                UserProfile user = UserProfile.GetUser(model.UserName);
                if (user != null)
                {
                    if ((user.AuthenticationHash as string) == hash)
                    {
                        user.AuthenticationHash = null;
                        user.IsApproved = true;
                        db.SaveChanges();
                        OsbleAuthentication.LogIn(user);

                        //AC: Who wrote this?  I think that it updates pending assignment teams.
                        List<AssignmentTeam> atList = (from at in db.AssignmentTeams
                                                       select at).ToList();
                        UserProfile up = (from users in db.UserProfiles
                                          where users.UserName == user.UserName
                                          select users).FirstOrDefault();

                        foreach (AssignmentTeam at in atList)
                        {
                            if (!at.Assignment.HasTeams)
                            {
                                if (at.Team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.UserName == user.UserName)
                                {
                                    at.Team.Name = up.LastAndFirst();
                                }
                            }
                        }
                        db.SaveChanges();

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Either the e-mail address or the link used to access this page is no longer valid");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Either the e-mail address or the link used to access this page is no longer valid");
                }
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
            if (privatekey == null || ReCaptcha.Validate(privateKey: privatekey))
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

                    sendVerificationEmail(false, "https://osble.org" + Url.Action("ActivateAccount", new { hash = randomHash }), model.FirstName, model.Email);
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
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult AcademiaRegister(RegisterModel model)
        {
            string privatekey = GetReCaptchaPrivateKey();

            //Fall through if ReCaptcha is not set up correctly
            if (privatekey == null || ReCaptcha.Validate(privateKey: privatekey))
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

                    //try creating the account
                    try
                    {
                        UserProfile profile = new UserProfile();

                        profile.UserName = model.Email;
                        profile.Password = UserProfile.GetPasswordHash(model.Password);
                        profile.AuthenticationHash = randomHash;
                        profile.FirstName = model.FirstName;
                        profile.LastName = model.LastName;
                        profile.Identification = model.Identification;
                        profile.SchoolID = model.SchoolID;
                        profile.School = db.Schools.Find(model.SchoolID);

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

                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        FormsAuthentication.SignOut();
                        ModelState.AddModelError("", e.Message);

                        return AcademiaRegister();
                    }

                    sendVerificationEmail(true, "https://osble.org" + Url.Action("ActivateAccount", new { hash = randomHash }), model.FirstName, model.Email);

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
                    UserProfile currentUser = OsbleAuthentication.CurrentUser;
                    if (currentUser != null)
                    {
                        currentUser.Password = UserProfile.GetPasswordHash(model.NewPassword);
                        db.SaveChanges();
                        changePasswordSucceeded = true;
                    }
                    else
                    {
                        changePasswordSucceeded = false;
                    }
                }
                catch (Exception)
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
            if (privatekey == null || ReCaptcha.Validate(privateKey: privatekey))
            {
                UserProfile osbleProfile = db.UserProfiles.Where(m => m.UserName.CompareTo(model.EmailAddress) == 0).FirstOrDefault();
                if (osbleProfile != null)
                {
                    string newPass = GenerateRandomString(10);
                    osbleProfile.Password = UserProfile.GetPasswordHash(newPass);
                    db.SaveChanges();
#if !DEBUG
                    string body = "Your OSBLE password has been reset.\n Your new password is: " + newPass + "\n\nPlease change this password as soon as possible.";

                    MailMessage mm = new MailMessage(new MailAddress(ConfigurationManager.AppSettings["OSBLEFromEmail"], "OSBLE"),
                                new MailAddress(model.EmailAddress));

                    mm.Subject = "[OSBLE] Password Reset Request";
                    mm.Body = body;

                    //This will need to fixed whenever we get a Server that can send mail
                    SmtpClient sc = new SmtpClient();
                    sc.UseDefaultCredentials = true;

                    sc.Send(mm);
#endif

                    
                    return View("ResetPasswordSuccess");

                }
            }
            return View("ResetPasswordFailure");
        }

        [OsbleAuthorize]
        public ActionResult ProfilePicture()
        {
            return new FileStreamResult(FileSystem.GetProfilePictureOrDefault(CurrentUser), "image/jpeg");
        }

        [OsbleAuthorize]
        [HttpPost]
        public ActionResult UploadPicture(HttpPostedFileBase file)
        {
            if (Request.Params["remove"] != null) // Delete Picture
            {
                FileSystem.DeleteProfilePicture(CurrentUser);
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

                            // Write image to memory stream.
                            FileStream fs = FileSystem.GetProfilePictureForWrite(CurrentUser);
                            finalImage.Save(fs, ImageFormat.Jpeg);
                            fs.Close();
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
            if (privatekey == null || ReCaptcha.Validate(privateKey: privatekey))
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
                attempts = (int)context.Session["attempts"];
            }
            catch { }

            context.Session["attempts"] = ++attempts;
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

        private void sendVerificationEmail(bool acedemia, string link, string firstName, string to)
        {
            string subject = "Welcome to OSBLE";

            string message = "Dear " + firstName + @",<br/>
            <br/>
            Thank you for creating an account at osble.org. Please go <a href='" + link + @"'>here</a> in order to activate your account.<br/>
            <br/>
            ";
            if (acedemia)
            {
                message += @"If you are a teacher and would like to use OSBLE for one or more of your courses, please e-mail instructor_request@osble.org to request an instructor account.  In the e-mail, please include the name of your school and brief descriptions of the courses you'd like to teach using OSBLE.<br/>
                <br/>
                ";
            }
            message += @"Best regards,<br/>
            The OSBLE Team in the <a href='www.helplab.org'>HELP lab</a> at <a href='www.wsu.edu'>Washington State University</a>";

            Email.Send(subject, message, new List<MailAddress>() { new MailAddress(to) });
        }
    }
}