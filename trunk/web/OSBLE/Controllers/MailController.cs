using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [IsNotAnonymized]
    public class MailController : OSBLEController
    {
        //
        // GET: /Mail/

        public ViewResult Index()
        {
            var mails = from c in db.Mails where c.ToUserProfileID == CurrentUser.ID && c.CourseReferenceID == activeCourse.CourseID select c;
            return View(mails.ToList());
        }

        //
        // GET: /Mail/Details/5

        public ViewResult View(int id)
        {
            Mail mail = db.Mails.Find(id);
            mail.Read = true;
            db.SaveChanges();
            return View(mail);
        }

        //
        // GET: /Mail/Create

        public ActionResult Create()
        {
            ViewBag.FromUserProfileID = new SelectList(db.UserProfiles, "ID", "UserName");
            ViewBag.ToUserProfileID = new SelectList(db.UserProfiles, "ID", "UserName");
            return View();
        }

        //
        // POST: /Mail/Create

        [HttpPost]
        public ActionResult Create(Mail mail)
        {
            if (ModelState.IsValid)
            {
                mail.FromUserProfileID = CurrentUser.ID;
                mail.FromUserProfile = CurrentUser;
                mail.CourseReferenceID = activeCourse.CourseID;
                mail.Read = false;
                db.Mails.Add(mail);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.FromUserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", mail.FromUserProfileID);
            ViewBag.ToUserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", mail.ToUserProfileID);
            return View(mail);
        }

        //
        // GET: /Mail/Delete/5

        public ActionResult Delete(int id)
        {
            Mail mail = db.Mails.Find(id);
            return View(mail);
        }

        //
        // POST: /Mail/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Mail mail = db.Mails.Find(id);
            db.Mails.Remove(mail);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
