using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Gradables;

namespace OSBLE.Controllers
{
    [Authorize]
    [ActiveCourseAttribute]
    [NotForCommunity]
    public class DeliverableController : OSBLEController
    {
        //
        // GET: /Deliverable/

        public ViewResult Index()
        {
            return View(db.Deliverables.ToList());
        }

        //
        // GET: /Deliverable/Details/5

        public ViewResult Details(int id)
        {
            Deliverable deliverable = db.Deliverables.Find(id);
            return View(deliverable);
        }

        //
        // GET: /Deliverable/Create

        public ActionResult Create()
        {
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes());
            return View();
        }

        //
        // POST: /Deliverable/Create

        [HttpPost]
        public ActionResult Create(Deliverable deliverable)
        {
            if (ModelState.IsValid)
            {
                db.Deliverables.Add(deliverable);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(deliverable);
        }

        //
        // GET: /Deliverable/Edit/5

        public ActionResult Edit(int id)
        {
            Deliverable deliverable = db.Deliverables.Find(id);
            return View(deliverable);
        }

        //
        // POST: /Deliverable/Edit/5

        [HttpPost]
        public ActionResult Edit(Deliverable deliverable)
        {
            if (ModelState.IsValid)
            {
                db.Entry(deliverable).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(deliverable);
        }

        //
        // GET: /Deliverable/Delete/5

        public ActionResult Delete(int id)
        {
            Deliverable deliverable = db.Deliverables.Find(id);
            return View(deliverable);
        }

        //
        // POST: /Deliverable/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Deliverable deliverable = db.Deliverables.Find(id);
            db.Deliverables.Remove(deliverable);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private List<string> GetListOfDeliverableTypes()
        {
            List<string> fileTypes = new List<string>();
            int i = 0;
            DeliverableType deliverable = (DeliverableType)i;
            while (Enum.IsDefined(typeof(DeliverableType), i))
            {
                Type type = deliverable.GetType();

                FieldInfo fi = type.GetField(deliverable.ToString());

                //we get the attributes of the selected language
                FileExtensions[] attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

                //make sure we have more than (should be exactly 1)
                if (attrs.Length > 0 && attrs[0] is FileExtensions)
                {
                    //we get the first attributes value which should be the fileExtension
                    string s = deliverable.ToString();
                    s += " (";
                    s += string.Join(", ", attrs[0].Extensions);
                    s += ")";
                    fileTypes.Add(s);
                }
                else
                {
                    //throw and exception if not decorated with any attrs because it is a requirement
                    throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
                }

                i++;
                deliverable = (DeliverableType)i;
            }

            return fileTypes;
        }
    }
}