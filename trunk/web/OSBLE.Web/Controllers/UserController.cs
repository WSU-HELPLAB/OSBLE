using OSBLE.Models.Users;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;

namespace OSBLE.Controllers
{
    public class UserController : OSBLEController
    {
        /// <summary>
        /// Identities the specified id.
        /// </summary>
        /// <param name="id">The username or anonymous identifier.</param>
        /// <param name="anon">if set to <c>true</c> then <paramref name="id"/> represents an anonymous identifier rather than a username.</param>
        /// <returns>The view to display.</returns>
        public ActionResult Identity(string id, bool anon)
        {
            if (!anon)
            {
                var redirect = this.RedirectIfNotNormalizedRequestUri(id);
                if (redirect != null)
                {
                    return redirect;
                }
            }

            if (Request.AcceptTypes != null && Request.AcceptTypes.Contains("application/xrds+xml"))
            {
                return View("Xrds");
            }

            if (!anon)
            {
                this.ViewData["username"] = id;
            }

            return View();
        }

        public ActionResult Xrds(string id)
        {
            return View();
        }

        private ActionResult RedirectIfNotNormalizedRequestUri(string user)
        {
            Uri normalized = OpenIdProviderMvc.Models.User.GetClaimedIdentifierForUser(user);
            if (Request.Url != normalized)
            {
                return Redirect(normalized.AbsoluteUri);
            }

            return null;
        }

        /// <summary>
        /// Returns the profile picture for the supplied user id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public FileStreamResult Picture(int id, int size = 128)
        {
            UserProfile user = db.UserProfiles.Where(u => u.ID == id).FirstOrDefault();
            System.Drawing.Bitmap userBitmap;
            if (user != null)
            {
                try
                {
                    userBitmap = user.ProfileImage.GetProfileImage();
                }
                catch (Exception)
                {
                    IdenticonRenderer renderer = new IdenticonRenderer();
                    userBitmap = renderer.Render(user.UserName.GetHashCode(), 128);
                    user.SetProfileImage(userBitmap);
                    db.SaveChanges();
                }
            }
            else
            {
                IdenticonRenderer renderer = new IdenticonRenderer();
                userBitmap = renderer.Render(1, 128);
            }

            if (size != 128)
            {
                Bitmap bmp = new Bitmap(userBitmap, size, size);
                Graphics graph = Graphics.FromImage(userBitmap);
                graph.InterpolationMode = InterpolationMode.High;
                graph.CompositingQuality = CompositingQuality.HighQuality;
                graph.SmoothingMode = SmoothingMode.AntiAlias;
                graph.DrawImage(bmp, new Rectangle(0, 0, size, size));
                userBitmap = bmp;
            }
            else
            {
            }

            MemoryStream stream = new MemoryStream();
            userBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            return new FileStreamResult(stream, "image/png");
        } 
    }
}
