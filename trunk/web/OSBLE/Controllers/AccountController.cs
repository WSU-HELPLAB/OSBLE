﻿using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using OSBLE.Models;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Drawing;
using System.Web;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace OSBLE.Controllers
{
    public class AccountController : OSBLEController
    {
        //
        // GET: /Account/LogOn

        public ActionResult LogOn()
        {
            return View();
        }

        //
        // POST: /Account/LogOn

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.UserName, model.Password))
                {
                    context.Session.Clear(); // Clear session variables.
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
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
            FormsAuthentication.SignOut();
            context.Session.Clear(); // Clear session on signout.

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        public ActionResult Register()
        {
            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name");

            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                MembershipCreateStatus createStatus;
                Membership.CreateUser(model.Email, model.Password, model.Email, null, null, true, null, out createStatus);

                if (createStatus == MembershipCreateStatus.Success)
                {
                    FormsAuthentication.SetAuthCookie(model.Email, false /* createPersistentCookie */);

                    try
                    {
                        UserProfile profile = new UserProfile();

                        profile.UserName = model.Email;
                        profile.FirstName = model.FirstName;
                        profile.LastName = model.LastName;
                        profile.Identification = model.Identification;
                        profile.SchoolID = model.SchoolID;
                        profile.School = db.Schools.Find(model.SchoolID);
                        
                        UserProfile up = db.UserProfiles.Where(c => c.SchoolID == profile.SchoolID && c.Identification == profile.Identification).FirstOrDefault();

                        if (up != null) // User profile exists. Is it a stub?
                        {
                            if (up.UserName == null) // Stub. Register to the account.
                            {
                                up.UserName = model.Email;
                                up.FirstName = model.FirstName;
                                up.LastName = model.LastName;
                                db.Entry(up).State = EntityState.Modified;
                            }
                            else // Existing Account. Throw validation error.
                            {
                                throw new Exception("You have entered an ID number that is already in use for your school.");
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
                        Membership.DeleteUser(model.Email);

                        ModelState.AddModelError("", e.Message);

                        return Register();
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", ErrorCodeToString(createStatus));
                }
            }

            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name");

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePassword

        [Authorize]
        public ActionResult Profile()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword

        [Authorize]
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
                    MembershipUser currentUser = Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
                    changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
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
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            var user = Membership.GetUser(model.EmailAddress);

            string newPass = user.ResetPassword();

            MailMessage mm = new MailMessage("DoNotReply@Osble.org", model.EmailAddress, "PassordReset", newPass);

            //This will need to fixed whenever we get a Server that can send mail
            SmtpClient sc = new SmtpClient();
            sc.Send(mm);
            

            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult UploadPicture(HttpPostedFileBase file)
        {
            if (file != null)
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
                    int thumbSize = 64;
                                        
                    // Crop image to a square.
                    int square = Math.Min(image.Width,image.Height);
                    Bitmap cropImage = new Bitmap(square, square);
                    Bitmap finalImage = new Bitmap(thumbSize, thumbSize);
                    Graphics cropGraphics = Graphics.FromImage(cropImage);
                    Graphics finalGraphics = Graphics.FromImage(finalImage);

                    // Center cropped image horizontally, leave at the top vertically. (better focus on subject)
                    cropGraphics.DrawImage(image,-(image.Width - cropImage.Width)/2,0);

                    // Convert to thumbnail.
                    finalGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    finalGraphics.DrawImage(cropImage,
                                        new Rectangle(0, 0, thumbSize, thumbSize),
                                        new Rectangle(0, 0, square, square),
                                        GraphicsUnit.Pixel);

                    // Write image to memory stream.
                    MemoryStream s = new MemoryStream();
                    finalImage.Save(s, ImageFormat.Jpeg);
                    
                    // Back up to beginning of stream.
                    s.Seek(0, 0);

                    // TODO: Replace this with file system saving mechanism.
                    FileResult outFile = new FileStreamResult(s, "image/jpeg");

                    return outFile;
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
            if (ModelState.IsValid)
            {
                ViewBag.ContactUsName = model.Name;
                return RedirectToAction("ContactUsSuccess");
            }
            return View(model);
        }


        [HttpPost]
        public ActionResult ContactUsSuccess(ContactUsModel model)
        {
            ViewBag.CUName = model.Name;
            ViewBag.CUEmail = model.Email;
            ViewBag.CUMsg = model.Message;
            return View();
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
    }
}
