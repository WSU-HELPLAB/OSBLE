using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using LumenWorks.Framework.IO.Csv;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.AbstractCourses;
using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.HomePage; //yc: added for notifcations
using OSBLE.Utility;
using System.Net.Mail;

namespace OSBLE.Controllers
{
#if !DEBUG
    [RequireHttps]
#endif
    [OsbleAuthorize]
    [RequireActiveCourse]
    public class RosterController : OSBLEController
    {
        public RosterController()
        {
            ViewBag.CurrentTab = "Users";
            ViewBag.ActiveCourseUser = ActiveCourseUser;
        }

        public class RosterEntry
        {
            public string Identification
            {
                get;
                set;
            }

            public int Section
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }
            public string Email
            {
                get;
                set;
            }
        } 

        public class UsersBySection
        {
            public string SectionNumber
            {
                get;
                set;
            }

            public List<UsersByRole> UsersByRole
            {
                get;
                set;
            }

        } 

        public class UsersByRole
        {
            public string RoleName
            {
                get;
                set;
            }

            public List<UserProfile> Users
            {
                get;
                set;
            }

            public int Count
            {
                get;
                set;
            }
        }
        // GET: /Roster/
        [CanModifyCourse]
        public ActionResult Index()
        {
            //Get all users for the current class
            var users = (from c in db.CourseUsers
                         where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                         select c);

            var usersGroupedBySection = users.GroupBy(CourseUser => CourseUser.Section).OrderBy(CourseUser => CourseUser.Key).ToList();

            List<UsersBySection> usersBySections = new List<UsersBySection>();


            //yc this portion is used to populate a white table relative to the current course 
            //this information should only be visible to instructors/admins (currently all students see this information

           //Get all the WhiteTabled Users for the current class 
            var WTusers = (from d in db.WhiteTableUsers
                           where d.CourseID == ActiveCourseUser.AbstractCourseID
                           select d);
 
            var WTusersGroupedByCourseID = WTusers.GroupBy(WhiteTableUsers => WhiteTableUsers.CourseID).OrderBy(WhiteTableUsers => WhiteTableUsers.Key).ToList();

            List<WhiteTableUser> WTup = new List<WhiteTableUser>();

            foreach (var WTu in WTusers)
            {
                WTup.Add(WTu);
            }
            
            //Remove duplicates that may slip in 
            WTup = WTup.Distinct().ToList();
            ViewBag.WhiteTableUsers = WTup;

           //\FW

            foreach (var section in usersGroupedBySection)
            {
                UsersBySection userBySection = new UsersBySection();
                userBySection.SectionNumber = section.Key.ToString();
                List<UsersByRole> usersByRoles = new List<UsersByRole>();

                //Get all the users for each role
                List<AbstractRole> roles = new List<AbstractRole>();

                if (ActiveCourseUser.AbstractCourse is Course)
                {
                    // Set custom role order for display
                    List<CourseRole.CourseRoles> rolesOrder = new List<CourseRole.CourseRoles>();

                    foreach (CourseRole.CourseRoles role in Enum.GetValues(typeof(CourseRole.CourseRoles)).Cast<CourseRole.CourseRoles>())
                    {
                        rolesOrder.Add(role);
                    }

                    foreach (CourseRole.CourseRoles r in rolesOrder)
                    {
                        roles.Add(db.CourseRoles.Find((int)r));
                        
                    }
                }
                else
                { // Community
                    // Set custom role order for display
                    List<CommunityRole.OSBLERoles> rolesOrder = new List<CommunityRole.OSBLERoles>();

                    int i = (int)CommunityRole.OSBLERoles.Leader;
                    while (Enum.IsDefined(typeof(CommunityRole.OSBLERoles), i))
                    {
                        rolesOrder.Add((CommunityRole.OSBLERoles)i);
                        i++;
                    }

                    foreach (CommunityRole.OSBLERoles r in rolesOrder)
                    {
                        roles.Add(db.CommunityRoles.Find((int)r));
                    }
                }

                foreach (AbstractRole role in roles)
                {
                    UsersByRole usersByRole = new UsersByRole();
                    usersByRole.RoleName = role.Name;
                    usersByRole.Users = new List<UserProfile>(from c in section
                                                              where role.ID == c.AbstractRole.ID
                                                              orderby c.UserProfile.LastName
                                                              select c.UserProfile
                                                              );
                    usersByRole.Count = usersByRole.Users.Count;

                    usersByRoles.Add(usersByRole);
                }

                //reverse it so the least important people are first

                userBySection.UsersByRole = usersByRoles;

                usersBySections.Add(userBySection);
            }

            ViewBag.UsersBySections = usersBySections;

            ViewBag.CanEditSelf = CanModifyOwnLink(ActiveCourseUser);

            if (Request.Params["notice"] != null)
            {
                ViewBag.Notice = Request.Params["notice"];
            }

            return View();
        } 

        [HttpPost]
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult ImportRoster(HttpPostedFileBase file)
        {
            if ((file != null) && (file.ContentLength > 0))
            {
                // Save file into session
                MemoryStream memStream = new MemoryStream();
                file.InputStream.CopyTo(memStream);
                Cache["RosterFile"] = memStream.ToArray();

                //reset position after read
                file.InputStream.Position = 0;
                Stream s = file.InputStream;
                List<string> headers = getRosterHeaders(s);
                file.InputStream.Seek(0, 0);

                string guessedSection = null;
                string guessedIdentification = null;
                string guessedName = null;
                string guessedName2 = null;
                string guessedEmail = null;                

                // Guess headers for section and identification
                foreach (string header in headers)
                {
                    if (guessedSection == null)
                    {
                        if (Regex.IsMatch(header, "section", RegexOptions.IgnoreCase))
                        {
                            guessedSection = header;
                        }
                    }

                    if (guessedIdentification == null)
                    {
                        if (Regex.IsMatch(header, "\\bident", RegexOptions.IgnoreCase)
                            || Regex.IsMatch(header, "\\bid\\b", RegexOptions.IgnoreCase)
                            || Regex.IsMatch(header, "\\bnumber\\b", RegexOptions.IgnoreCase)
                            )
                        {
                            guessedIdentification = header;
                        }
                    }

                    if (guessedName == null)
                    {
                        if (Regex.IsMatch(header, "name", RegexOptions.IgnoreCase))
                        {
                            guessedName = header;
                        }
                    }
                    else if (guessedName2 == null)
                    {
                        if (Regex.IsMatch(header, "name", RegexOptions.IgnoreCase))
                        {
                            guessedName2 = header;
                        }
                    }
                    if (guessedEmail == null)
                    {
                        if (Regex.IsMatch(header, "email", RegexOptions.IgnoreCase) || Regex.IsMatch(header, "e-mail", RegexOptions.IgnoreCase))
                        {
                            guessedEmail = header;
                        }
                    }
                }

                ViewBag.Headers = headers;
                ViewBag.GuessedSection = guessedSection;
                ViewBag.GuessedIdentification = guessedIdentification;
                ViewBag.GuessedName = guessedName;
                ViewBag.GuessedName2 = guessedName2;
                ViewBag.GuessedEmail = guessedEmail;

                return View();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult ApplyRoster(string idColumn, string sectionColumn, string nameColumn, string name2Column, string emailColumn)
        {
            byte[] rosterContent = (byte[])Cache["RosterFile"];
            int rosterCount = 0;
            int wtCount = 0;

            if ((rosterContent != null) && (idColumn != null) && (nameColumn != null))
            {
                MemoryStream memStream = new MemoryStream();
                memStream.Write(rosterContent, 0, rosterContent.Length);
                memStream.Position = 0;

                List<RosterEntry> rosterEntries = parseRoster(memStream, idColumn, sectionColumn, nameColumn, name2Column, emailColumn);

                if (rosterEntries.Count > 0)
                {
                   
                    // First check to make sure there are no duplicates in the ID table.
                    List<string> usedIdentifications = new List<string>();
                    foreach (RosterEntry entry in rosterEntries)
                    {
                        if (usedIdentifications.Contains(entry.Identification))
                        {
                            ViewBag.Error = "There are duplicate Student IDs in your roster. Please ensure the proper Student ID header was selected and check your roster file.";
                            return View("RosterError");
                        }
                        else
                        {
                            usedIdentifications.Add(entry.Identification);
                        }
                    }

                    // Make sure no student has the same ID as existing non-student members.
                    List<CourseUser> otherMembers = db.CourseUsers.Where(c => c.AbstractCourseID == ActiveCourseUser.AbstractCourseID && c.AbstractRoleID != (int)CourseRole.CourseRoles.Student).ToList();
                    foreach (CourseUser member in otherMembers)
                    {
                        
                        if (usedIdentifications.Contains(member.UserProfile.Identification) && member.AbstractRoleID != (int)CourseRole.CourseRoles.Pending)
                        {
                            ViewBag.Error = "There is a " + "[" + member.AbstractRole.Name + "]" + " non-student (" + member.UserProfile.FirstName + " " + member.UserProfile.LastName + ") in the course with the same School ID as a student on the roster. Please check your roster and try again.";
                            return View("RosterError");
                        }
                    }

                    //Use the list of our old students to track changes between the current and new class roster.
                    //Students that exist on the old roster but do not appear on the new roster will
                    //be given the "withdrawn" status
                    var oldRoster = from c in db.CourseUsers
                                    where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                                    &&
                                    c.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                    select c;
                    List<UserProfile> orphans = oldRoster.Select(cu => cu.UserProfile).ToList();
                    List<CourseUser> newRoster = new List<CourseUser>();
                    List<WhiteTable> newTable = new List<WhiteTable>();
                    string[] names = new string[2];
                    // Attach to users or add new user profile stubs.

                    //on new roster import clear the whitetable
                    clearWhiteTableOnRosterImport();
                   
                    foreach (RosterEntry entry in rosterEntries)
                    {

                        UserProfile userWithAccount = getEntryUserProfile(entry);
                        

                        if(userWithAccount != null)
                        {
                            CourseUser userIsPending = getPendingUserOnRoster(entry);
                            if(userIsPending != null)
                            {
                                userIsPending.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
                                orphans.Remove(userIsPending.UserProfile);
                                db.Entry(userIsPending).State = EntityState.Modified;
                                continue;                            
                            }
                            CourseUser existingUser = new CourseUser(); 
                            
                            //yc: before using create course user, you must set the following
                            existingUser.UserProfile = userWithAccount;
                            existingUser.AbstractRoleID = (int)CourseRole.CourseRoles.Student;

                            newRoster.Add(existingUser);
                            createCourseUser(existingUser);
                            rosterCount++;
                            //email the user notifying them that they have been added to this course 
                            if(entry.Email != String.Empty)
                                emailCourseUser(existingUser);
        

                         
                            orphans.Remove(existingUser.UserProfile);


                        }
                        //else the entry does not have a user profile, so WT them 
                        else
                        {
                            //create the WhiteTable that will hold the whitetableusers
                            WhiteTable whitetable = new WhiteTable();
                            whitetable.WhiteTableUser = new WhiteTableUser();


                            whitetable.Section = entry.Section;

                            whitetable.WhiteTableUser.Identification = entry.Identification;

                            if (entry.Name != null)
                            {
                                if (entry.Name.Contains(',')) //Assume "LastName, FirstName" format.
                                {
                                    names = entry.Name.Split(',');
                                    string[] parseFirstName = names[1].Trim().Split(' ');

                                    if (parseFirstName != null)
                                    {
                                        whitetable.WhiteTableUser.Name1 = parseFirstName[0];
                                        whitetable.WhiteTableUser.Name2 = names[0].Trim();
                                    }
                                    else
                                    {
                                        whitetable.WhiteTableUser.Name1 = names[1].Trim();
                                        whitetable.WhiteTableUser.Name2 = names[0].Trim();
                                    }
                                }
                                else //Assume "FirstName LastName" format. and No middle names.
                                {

                                    names = entry.Name.Trim().Split(' '); //Trimming trialing and leading spaces to avoid conflicts below
                                    if (names.Count() == 1) //Assume only last name
                                    {

                                        whitetable.WhiteTableUser.Name1 = string.Empty;
                                        whitetable.WhiteTableUser.Name2 = names[0];
                                    }
                                    else if (names.Count() == 2) //Only first and last name exist
                                    {

                                        whitetable.WhiteTableUser.Name1 = names[0];
                                        whitetable.WhiteTableUser.Name2 = names[1];
                                    }
                                    else //at least 1 Middle name exists. Use first and last entries in names
                                    {

                                        whitetable.WhiteTableUser.Name1 = names[0];
                                        whitetable.WhiteTableUser.Name2 = names[names.Count() - 1];
                                    }
                                }
                            }
                            else// the are nameless so the user will need to add this upon being added to a course 
                            {
                                whitetable.WhiteTableUser.Name1 = "Pending";
                                whitetable.WhiteTableUser.Name2 = string.Format("({0})", entry.Identification);
                            }
                            //check for emails

                            if (entry.Email != "")
                            {
                                whitetable.WhiteTableUser.Email = entry.Email;
                            }
                            else
                            {
                                whitetable.WhiteTableUser.Email = String.Empty;
                            }

                            createWhiteTableUser(whitetable);
                            wtCount++;
                            //will send email to white table user notifying them that they need to create an account to be added to the course 
                            //yc another check for emails
                            if (entry.Email != "")
                                emailWhiteTableUser(whitetable);
                        }

                    }// end foreach loop for whitetables

                  

                    db.SaveChanges();

                    //withdraw all orphans
                    foreach (UserProfile orphan in orphans)
                    {
                        WithdrawUserFromCourse(orphan);
                    }
                }
            }
            else if ((idColumn == null))
            {
                ViewBag.Error = "You did not specify headers for Student ID. Please try again.";
                return View("RosterError");
            }
            else
            {
                ViewBag.Error = "Your roster file was not properly loaded. Please try again.";
                return View("RosterError");
            }

            Course thisCourse = (from d in db.Courses
                                 where d.ID == ActiveCourseUser.AbstractCourseID
                                 select d).FirstOrDefault();
            //if there is at least one assignmnet in the course that has teams/is team based
            if (thisCourse != null && thisCourse.Assignments.Count(a => a.HasTeams) > 0)
            {
                return RedirectToAction("Index", new { notice = "Roster imported with " + rosterCount.ToString() + " students and " + wtCount.ToString() + " whitelisted students. Please note that this course has an ongoing team-based assignment, and you will need to manually add the newly enrolled students to a team." });
            }

            return RedirectToAction("Index", new { notice = "Roster imported with " + rosterCount.ToString() + " students and " + wtCount.ToString() + " whitelisted students" });
        }

        //
        // GET: /Roster/Create
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult Create()
        {
            ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name");
            return View();
        }

        //
        // POST: /Roster/Create
        [CanModifyCourse]
        [NotForCommunity]
        [HttpPost]
        public ActionResult Create(CourseUser courseuser)
        {

            string SchoolID = Request.Form["CurrentlySelectedSchool"];
            if (string.IsNullOrEmpty(SchoolID))
            {
                ModelState.AddModelError("School", "The School field is required.");
                ModelState.AddModelError("SchoolID", "");
                ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
                ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name");
                return View(); 
            }
            else
            {
                courseuser.UserProfile.SchoolID = Convert.ToInt32(SchoolID);
            }
            
            //if modelState isValid
            if (ModelState.IsValid && courseuser.AbstractRoleID != 0)
            {
                try
                {
                    createCourseUser(courseuser);
                }
                catch(Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                    ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
                    ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name");
                    return View();  
                }
            }

            Course thisCourse = ActiveCourseUser.AbstractCourse as Course;
            //if there is at least one assignmnet in the course that has teams/is team based
            if(thisCourse != null && thisCourse.Assignments.Count(a => a.HasTeams) > 0)
            {
                return RedirectToAction("Index", new { notice = "You have successfully added " + courseuser.UserProfile.LastAndFirst() + " to the course. Please note that this course has an ongoing team-based assignment, and " + courseuser.UserProfile.LastAndFirst() + " will need to be manually added to a team."});
            }

            return RedirectToAction("Index", new { notice = "You have successfully added " + courseuser.UserProfile.LastAndFirst() + " to the course." });

        }

        //
        // GET: /Roster/CreateByEmail
        [CanModifyCourse]
        public ActionResult CreateByEmail()
        {
            if (!(ActiveCourseUser.AbstractCourse is Community))
            {
                ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
            }
            else // Community Roles
            {
                ViewBag.AbstractRoleID = new SelectList(db.CommunityRoles, "ID", "Name");
            }
            return View();
        }

        //
        // POST: /Roster/CreateByEmail

        [HttpPost]
        [CanModifyCourse]
        public ActionResult CreateByEmail(CourseUser courseuser)
        {
            Course thisCourse = ActiveCourseUser.AbstractCourse as Course;
            //if modelState isValid
            if (ModelState.IsValid && courseuser.AbstractRoleID != 0)
            {
                try
                {
                    //yc this check for multiple emails added in. 
                    string temp = courseuser.UserProfile.UserName;
                    char[] delim = new char[] { ' ', ',' , ';' };
                    string[] emails = temp.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    string[] invalidEmails = new string[emails.Count()];
                    int invalidCount = 0;

                    if (emails.Count() > 1)
                    {
                        // more than one 


                        foreach (String username in emails)
                        {
                            //find teh user profile
                            UserProfile user = (from u in db.UserProfiles
                                                where u.UserName == username
                                                select u).FirstOrDefault();
                            if (user != null)
                            {
                                CourseUser newUser = courseuser;
                                newUser.UserProfile = user;
                                newUser.UserProfileID = user.ID;

                                attachCourseUserByEmail(newUser);
                            }
                            else
                            {
                                //userprofile doenst exist.
                                invalidEmails[invalidCount] = username;
                                invalidCount++;
                            }
                            //create a copy of the course user
                        }
                        if (invalidCount > 0)//caught at least one invalid
                        {
                            //create a notice
                            string message = "The following email(s) could not be added because these users do not exist: ";
                            foreach (string invalid in invalidEmails)
                            {
                                if (invalid != "")
                                    message += invalid + ", ";
                            }
                            string noticeMessage = message.Substring(0, message.Length - 2);
                            noticeMessage += ".";

                            return RedirectToAction("Index", new { notice = noticeMessage });
                        }
                        else
                        {
                           
                            //if there is at least one assignmnet in the course that has teams/is team based
                            if (thisCourse != null && thisCourse.Assignments.Count(a => a.HasTeams) > 0)
                            {
                                return RedirectToAction("Index", new { notice = emails.Count().ToString() + " users have been added to the course. Please note that this course has an ongoing team-based assignment, and you will need to manually add these users to a team." });
                            }
                            return RedirectToAction("Index", new { notice = emails.Count().ToString() + " users have been added to the course" });
                        }
                    }
                    else
                    {
                        //only one. do what you are originally intended for
                        attachCourseUserByEmail(courseuser);
                    }
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                    ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
                    return View();
                }
            }
          
            //if there is at least one assignmnet in the course that has teams/is team based
            if (thisCourse != null && thisCourse.Assignments.Count(a => a.HasTeams) > 0)
            {
                return RedirectToAction("Index", new { notice = "You have successfully added " + courseuser.UserProfile.LastAndFirst() + " to the course. Please note that this course has an ongoing team-based assignment, and you will need to manually add these users to a team." });
            }
            return RedirectToAction("Index", new { notice = "You have successfully added " + courseuser.UserProfile.LastAndFirst() + " to the course." });
        }

        //yc: get the white table user, wrote getWhiteTableUser to narrow results in function, so do need to organize here
        //get
        [CanModifyCourse]
        public ActionResult EditWTUser(int wtuID)
        {
            WhiteTableUser wtUser = getWhiteTableUser(wtuID);
            //wtu has been loaded up
            //setup views
            
            return View(wtUser);
        }

        [HttpPost]
        [CanModifyCourse]
        public ActionResult EditWTUser(WhiteTableUser wtUser)
        {
            //wtu has been loaded up
            if (ModelState.IsValid)
            {
                if (wtUser.Email == null)
                    wtUser.Email = String.Empty;

                db.Entry(wtUser).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

    
        /// <summary>
        /// yc: get- approve pending user for current course enrollment, clean up notifcation to instructor, change student status
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>back to index with notice the current pending user has been enrolled</returns>
        [CanModifyCourse]
        public ActionResult ApprovePending(int userId)
        {
            CourseUser pendingUser = getCourseUser(userId);
            Course thisCourse = ActiveCourseUser.AbstractCourse as Course;
            Notification n = db.Notifications.Where(item => item.SenderID == pendingUser.ID && item.RecipientID == ActiveCourseUser.ID).FirstOrDefault();
            //there is not always a notification for a pending user, say a instructor manually adds them to the pending list?
            if(n != null)
            {
                n.Read = true;
                db.SaveChanges();
            }
           

            //set user to active student
            if (pendingUser.AbstractRoleID == (int)CourseRole.CourseRoles.Pending)
            {
                
                pendingUser.Hidden = false;
                pendingUser.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
                db.Entry(pendingUser).State = EntityState.Modified;
                db.SaveChanges();
                addNewStudentToTeams(pendingUser);

                //if there is at least one assignmnet in the course that has teams/is team based
                if (thisCourse != null && thisCourse.Assignments.Count(a => a.HasTeams) > 0)
                {
                    return RedirectToAction("Index", "Roster", new { notice = pendingUser.UserProfile.FirstName + " " + pendingUser.UserProfile.LastName + " has been enrolled into this course. Please note that this course has an ongoing team-based assignment, and you will need to manually add " +pendingUser.UserProfile.FirstName + " " + pendingUser.UserProfile.LastName + " to a team." });
                }

                return RedirectToAction("Index", "Roster", new { notice = pendingUser.UserProfile.FirstName + " " + pendingUser.UserProfile.LastName + " has been enrolled into this course." });
            }
            else if (pendingUser.AbstractRoleID == (int)CommunityRole.OSBLERoles.Pending)
            {
                pendingUser.Hidden = false;
                pendingUser.AbstractRoleID = (int)CommunityRole.OSBLERoles.Participant;
                db.Entry(pendingUser).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", "Roster", new { notice = pendingUser.UserProfile.FirstName + " " + pendingUser.UserProfile.LastName + " is now a participant of this community." });
            }
            else
            {
                return RedirectToAction("Index", "Roster", new { notice = pendingUser.UserProfile.FirstName + " " + pendingUser.UserProfile.LastName + " IS NOT A PENDING USER." });
            }
            
        }
        /// <summary>
        /// yc: get- deny pending user for current course enrollmenet, clean up notifications to instructor
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [CanModifyCourse]
        public ActionResult DenyPending(int userId)
        {
            CourseUser pendingUser = getCourseUser(userId);
            //db entry will no longer exists, save names for notice
            string firstName = pendingUser.UserProfile.FirstName;
            string lastName = pendingUser.UserProfile.LastName;

            //mark notification read
            Notification n = db.Notifications.Where(item => item.SenderID == pendingUser.ID && item.RecipientID == ActiveCourseUser.ID).FirstOrDefault();
            if(n != null)
                n.Read = true;

            //remove the kid from the db
            db.CourseUsers.Remove(pendingUser);
            db.SaveChanges();

            if (pendingUser.AbstractRoleID == (int)CourseRole.CourseRoles.Pending)
                return RedirectToAction("Index", "Roster", new { notice = firstName + " " + lastName + " has been denied enrollment into this course." });
            else
                return RedirectToAction("Index", "Roster", new { notice = firstName + " " + lastName + " has been denied the ability to participate in this community." });
        }
        
        /// <summary>
        /// yc: creating a batch approval on pending users based on the current course
        /// </summary>
        /// <returns> to users page reflecting the changes</returns>
        [CanModifyCourse]
        public ActionResult BatchApprove()
        {
            Course thisCourse = ActiveCourseUser.AbstractCourse as Course;
            int count = 0;
            List<CourseUser> pendingUsers;
            //find all pending users for current course
            if (ActiveCourseUser.AbstractCourse.GetType() != typeof(Community)) //of type course
            {
                pendingUsers = (from c in db.CourseUsers
                                where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID &&
                                c.AbstractRoleID == (int)CourseRole.CourseRoles.Pending
                                select c).ToList();

                count = pendingUsers.Count();

                foreach (CourseUser p in pendingUsers)
                {
                    p.Hidden = false;
                    p.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
                    db.Entry(p).State = EntityState.Modified;
                    addNewStudentToTeams(p);
                }

                //get all notifications
                List<Notification> allUnreadNotifications = (from n in db.Notifications
                                                             where n.RecipientID == ActiveCourseUser.ID && !n.Read
                                                             select n).ToList();
                //get all notifications pertaining to the pendingUsers List
                List<Notification> pendingUsersNotifications = allUnreadNotifications.Where(item => pendingUsers.Contains(item.Sender)).ToList();
                //Mark them all as read
                foreach (Notification n in pendingUsersNotifications)
                {
                    n.Read = true;
                    db.Entry(n).State = EntityState.Modified;
                }

                
                db.SaveChanges();
                if (thisCourse != null && thisCourse.Assignments.Count(a => a.HasTeams) > 0)
                {
                    return RedirectToAction("Index", "Roster", new { notice = count.ToString() + " student(s) have been enrolled into this course. Please note that this course has an ongoing team-based assignment, and you will need to manually add the newly enrolled users to a team." });
                }
                return RedirectToAction("Index", "Roster", new { notice = count.ToString() + " student(s) have been enrolled into this course." });
            }
            else
            {
                pendingUsers = (from c in db.CourseUsers
                                where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID &&
                                c.AbstractRoleID == (int)CommunityRole.OSBLERoles.Pending
                                select c).ToList();

                count = pendingUsers.Count();

                foreach (CourseUser p in pendingUsers)
                {
                    p.Hidden = false;
                    p.AbstractRoleID = (int)CommunityRole.OSBLERoles.Participant;
                    db.Entry(p).State = EntityState.Modified;
                }

                //get all notifications
                List<Notification> allUnreadNotifications = (from n in db.Notifications
                                                             where n.RecipientID == ActiveCourseUser.ID && !n.Read
                                                             select n).ToList();
                //get all notifications pertaining to the pendingUsers List
                List<Notification> pendingUsersNotifications = allUnreadNotifications.Where(item => pendingUsers.Contains(item.Sender)).ToList();
                //Mark them all as read
                foreach (Notification n in pendingUsersNotifications)
                {
                    n.Read = true;
                    db.Entry(n).State = EntityState.Modified;
                }

                db.SaveChanges();

                return RedirectToAction("Index", "Roster", new { notice = count.ToString() + " participant(s) have been added to the community." });
            }


        }

        /// <summary>
        /// yc: creating a batch denial on pending users based on the current course.
        /// </summary>
        /// <returns> to users page reflect the changes</returns>
        [CanModifyCourse]
        public ActionResult BatchDeny()
        {
            int count = 0;
            //find all pending users for current course
            List<CourseUser> pendingUsers;
            //find all pending users for current course
            if (ActiveCourseUser.AbstractCourse.GetType() != typeof(Community))
            {

                pendingUsers = (from c in db.CourseUsers
                                             where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID &&
                                             c.AbstractRoleID == (int)CourseRole.CourseRoles.Pending
                                             select c).ToList();
            }
            else
            {
                pendingUsers = (from c in db.CourseUsers
                                where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID &&
                                c.AbstractRoleID == (int)CommunityRole.OSBLERoles.Pending
                                select c).ToList();
            }

            //get all notifications
            List<Notification> allUnreadNotifications = (from n in db.Notifications
                                                         where n.RecipientID == ActiveCourseUser.ID && !n.Read
                                                         select n).ToList();
            //get all notifications pertaining to the pendingUsers List
            List<Notification> pendingUsersNotifications = allUnreadNotifications.Where(item => pendingUsers.Contains(item.Sender)).ToList();
            //Mark them all as read
            foreach (Notification n in pendingUsersNotifications)
            {
                n.Read = true;
                db.Entry(n).State = EntityState.Modified;
            }

            count = pendingUsers.Count();
            foreach (CourseUser p in pendingUsers)
            {
                db.CourseUsers.Remove(p);
            }
            db.SaveChanges();

            if (ActiveCourseUser.AbstractCourse.GetType() != typeof(Community))
                return RedirectToAction("Index", "Roster", new { notice = count.ToString() + " student(s) have been denied enrollment into this course." });
            else
                return RedirectToAction("Index", "Roster", new { notice = count.ToString() + " participants(s) have been denied the ability to join the community." });
        }


        /// <summary>
        /// yc: this function finds all students currently enrolled, and will turn them all into withdrawn students.
        /// </summary>
        /// <returns></returns>
        [CanModifyCourse]
        public ActionResult BatchWithdraw()
        {
            int count = 0;

            //find all students for current course
            List<CourseUser> students = (from c in db.CourseUsers
                                             where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID &&
                                             c.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                             select c).ToList();
            count = students.Count();

            foreach (CourseUser p in students)
            {
                p.AbstractRoleID = (int)CourseRole.CourseRoles.Withdrawn;
                db.Entry(p).State = EntityState.Modified;
                db.SaveChanges();
            }

            return RedirectToAction("Index", "Roster", new { notice = count.ToString() + " student(s) have been withdrawn from this course" });
        }

        [CanModifyCourse]
        public ActionResult BatchClearWhiteTable()
        {
            int count = 0;

            //find all whitelisted students
            List<WhiteTableUser> students = (from c in db.WhiteTableUsers
                                             where c.CourseID == ActiveCourseUser.AbstractCourseID
                                             select c).ToList();
            count = students.Count();

            foreach (WhiteTableUser p in students)
            {
                db.WhiteTableUsers.Remove(p);
            }
            db.SaveChanges();
            return RedirectToAction("Index", "Roster", new { notice = count.ToString() + " whitelisted student(s) have been removed from this course" });
        }

        [CanModifyCourse]
        public ActionResult BatchDeleteWithdrawn()
        {
            int count = 0;

            //find all withdrawn students for current course
            List<CourseUser> students = (from c in db.CourseUsers
                                         where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID &&
                                         c.AbstractRoleID == (int)CourseRole.CourseRoles.Withdrawn
                                         select c).ToList();
            count = students.Count();

            foreach (CourseUser p in students)
            {
                db.CourseUsers.Remove(p);
            }
            db.SaveChanges();
            return RedirectToAction("Index", "Roster", new { notice = count.ToString() + " withdrawn students have been removed from the course" });
        }

        [CanModifyCourse]
        public ActionResult ChangeWithdrawnToStudentRole(int userProfileID)
        {
            CourseUser CourseUser = getCourseUser(userProfileID);
            
            if (CanModifyOwnLink(CourseUser))
            {
                if (ModelState.IsValid)
                {
                    CourseUser.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
                    db.Entry(CourseUser).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", CourseUser.UserProfileID);
                ViewBag.AbstractCourse = new SelectList(db.Courses, "ID", "Prefix", CourseUser.AbstractCourseID);
                ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name", CourseUser.AbstractRoleID);
                return View(CourseUser);
            }
            return RedirectToAction("Index");
        }

        [CanModifyCourse]
        public ActionResult ChangeStudentToWithdrawnRole(int userProfileID)
        {
            CourseUser CourseUser = getCourseUser(userProfileID);

            if (CanModifyOwnLink(CourseUser))
            {
                if (ModelState.IsValid)
                {
                    CourseUser.AbstractRoleID = (int)CourseRole.CourseRoles.Withdrawn;
                    db.Entry(CourseUser).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", CourseUser.UserProfileID);
                ViewBag.AbstractCourse = new SelectList(db.Courses, "ID", "Prefix", CourseUser.AbstractCourseID);
                ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name", CourseUser.AbstractRoleID);
                return View(CourseUser);
            }
            return RedirectToAction("Index");
        }

        //Students
        //

        //
        // GET: /Roster/Edit/5
        [CanModifyCourse]
        public ActionResult Edit(int userProfileID)
        {
            CourseUser CourseUser = getCourseUser(userProfileID);
            if (CanModifyOwnLink(CourseUser))
            {
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", CourseUser.UserProfileID);
                if (ActiveCourseUser.AbstractCourse is Course)
                {
                    ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name", CourseUser.AbstractRoleID);
                }
                else // Community Roles
                {
                    ViewBag.AbstractRoleID = new SelectList(db.CommunityRoles, "ID", "Name");
                }
                return View(CourseUser);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Roster/Edit/5

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Edit(CourseUser CourseUser)
        {
            if (CanModifyOwnLink(CourseUser))
            {
                if (ModelState.IsValid)
                {
                    db.Entry(CourseUser).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", CourseUser.UserProfileID);
                ViewBag.AbstractCourse = new SelectList(db.Courses, "ID", "Prefix", CourseUser.AbstractCourseID);
                ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name", CourseUser.AbstractRoleID);
                return View(CourseUser);
            }
            return RedirectToAction("Index");
        }


        //yc: not an inline remove
        //
        [CanModifyCourse]
        public ActionResult DeleteWTUser(int wtuID)
        {
            WhiteTableUser wtUser = getWhiteTableUser(wtuID);
            string name1 = wtUser.Name1;
            string name2 = wtUser.Name2;
            if (wtUser != null)
            {
                db.WhiteTableUsers.Remove(wtUser);
                db.SaveChanges();
            }

            return RedirectToAction("Index", "Roster", new { notice = name1 + " " + name2 + " has been removed" });
        }
        // POST: /Roster/Delete/5

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Delete(int userProfileID)
        {
            CourseUser CourseUser = getCourseUser(userProfileID);
            if ((CourseUser != null) && CanModifyOwnLink(CourseUser))
            {
                RemoveUserFromCourse(CourseUser.UserProfile);
            }
            else
            {
                Response.StatusCode = 403;
            }
            return View("_AjaxEmpty");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private WhiteTableUser getWhiteTableUser(int wtuID)
        {
            return (from w in db.WhiteTableUsers
                    where w.ID == wtuID && w.CourseID == ActiveCourseUser.AbstractCourseID
                    select w).FirstOrDefault();
        }
        private CourseUser getCourseUser(int userProfileId)
        {
            return (from c in db.CourseUsers
                    where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                    && c.UserProfileID == userProfileId
                    select c).FirstOrDefault();
        }

        /// <summary>
        /// This says can the passed courseUser Modify the course and if so is there another teacher
        /// that can also modify this course if so it returns true else returns false
        /// Reason for check: Do not want instructors to delete themselves out of a course or remove their instructor status if there are no
        /// other instructors to take their place.
        /// </summary>
        /// <param name="courseUser"></param>
        /// <returns></returns>
        private bool CanModifyOwnLink(CourseUser courseUser)
        {
            var diffTeacher = (from c in db.CourseUsers
                               where (c.AbstractCourseID == courseUser.AbstractCourseID
                               && c.AbstractRole.CanModify == true
                               && c.UserProfileID != courseUser.UserProfileID)
                               select c);

            if (courseUser.UserProfileID != CurrentUser.ID || diffTeacher.Count() > 0)
            {   
                return true;
            }
            else
            {
                return false;
            }
        }

        private List<string> getRosterHeaders(Stream roster)
        {
            StreamReader sr = new StreamReader(roster);
            CachedCsvReader csvReader = new CachedCsvReader(sr, true);

            return csvReader.GetFieldHeaders().ToList();
        }

        private List<RosterEntry> parseRoster(Stream roster, string idNumberColumnName, string sectionColumnName, string nameColumnName, string name2ColumnName, string emailColumn)
        {
            StreamReader sr = new StreamReader(roster);
            CachedCsvReader csvReader = new CachedCsvReader(sr, true);

            List<RosterEntry> rosterData = new List<RosterEntry>();

            bool hasSectionInfo = false;

            if (sectionColumnName != null)
            {
                hasSectionInfo = csvReader.GetFieldHeaders().Contains(sectionColumnName);
            }

            csvReader.MoveToStart();
            while (csvReader.ReadNextRecord())
            {
                int sectionNum;
                RosterEntry entry = new RosterEntry();
                entry.Identification = csvReader[csvReader.GetFieldIndex(idNumberColumnName)];

                if (nameColumnName != "")
                {
                    if (name2ColumnName != "")
                    {
                        entry.Name = csvReader[csvReader.GetFieldIndex(name2ColumnName)] + ", " + csvReader[csvReader.GetFieldIndex(nameColumnName)];
                    }
                    else
                    {
                        entry.Name = csvReader[csvReader.GetFieldIndex(nameColumnName)];
                    }
                }
                else
                {
                    entry.Name = null;
                }
                if (hasSectionInfo)
                {
                    int.TryParse(csvReader[csvReader.GetFieldIndex(sectionColumnName)], out sectionNum);
                    entry.Section = sectionNum;
                }
                else
                {
                    entry.Section = 0;
                }
                if(emailColumn != "" && emailColumn != "None")
                {
                    entry.Email = csvReader[csvReader.GetFieldIndex(emailColumn)];
                }

                rosterData.Add(entry);
            }

            return rosterData;
        }

        [HttpGet, FileCache(Duration = 3600)]
        [Obsolete("Use UserController/Picture instead")]
        public ActionResult ProfilePicture(int userProfile)
        {
            return RedirectToAction("Picture", "User", new { id = userProfile });
        }

        /// <summary>
        /// This sets up everything for the courseUser and will create a new UserProfile if it doesn't not exist.
        /// </summary>
        /// <param name="courseuser">It must have section, role set, and a reference to UserProfile with Identification set</param>
        private void createCourseUser(CourseUser courseuser)
        {
            //This will return a user if they exist already or null if they don't


            var user = (from c in db.UserProfiles
                        where c.Identification == courseuser.UserProfile.Identification
                        && c.SchoolID == courseuser.UserProfile.SchoolID
                        select c).FirstOrDefault();
            if (user == null)
            {

                throw new Exception("No user exists with that Student ID!");

                //user doesn't exist so we got to make a new one
                //Create userProfile with the new ID
                //UserProfile up = new UserProfile();
                //up.CanCreateCourses = false;
                //up.IsAdmin = false;
                //up.SchoolID = CurrentUser.SchoolID;
                //up.Identification = courseuser.UserProfile.Identification;

                //if (courseuser.UserProfile.FirstName != null)
                //{
                //    up.FirstName = courseuser.UserProfile.FirstName;
                //    up.LastName = courseuser.UserProfile.LastName;
                //}
                //else
                //{
                //    up.FirstName = "Pending";
                //    up.LastName = string.Format("({0})", up.Identification);
                //}
                //db.UserProfiles.Add(up);
                //db.SaveChanges();

                ////Set the UserProfileID to point to our new student
                //courseuser.UserProfile = up;
                //courseuser.UserProfileID = up.ID;
                //courseuser.AbstractCourseID = ActiveCourseUser.AbstractCourseID;
            }
            else //If the CourseUser already has a UserProfile..
            {
                if (courseuser.UserProfile.FirstName != null) 
                {
                    user.FirstName = courseuser.UserProfile.FirstName;
                    user.LastName = courseuser.UserProfile.LastName;
                    db.SaveChanges();
                }
                courseuser.UserProfile = user;
                courseuser.UserProfileID = user.ID;
                db.SaveChanges();
            }
            courseuser.AbstractCourseID = ActiveCourseUser.AbstractCourseID;
            //Check uniqueness before adding the CourseUser and adding them to the Teams
            if ((from c in db.CourseUsers
                 where c.AbstractCourseID == courseuser.AbstractCourseID && c.UserProfileID == courseuser.UserProfileID
                 select c).Count() == 0)
            {
                db.CourseUsers.Add(courseuser);
                db.SaveChanges();
                addNewStudentToTeams(courseuser);
            }
        }
       
        /// <summary>
        /// This method will add the new courseUser to all the various types of teams they need to be on for each assignment type.
        /// </summary>
        /// <param name="courseUser">A newly added courseUser, must of be role student</param>
        private void addNewStudentToTeams(CourseUser courseUser)
        {

            if (courseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student)
            {
                //If we already have assignments in the course, we need to add the new student into these assignments
                int currentCourseId = ActiveCourseUser.AbstractCourseID;
                List<Assignment> assignments = (from a in db.Assignments
                                                where a.CourseID == currentCourseId
                                                select a).ToList();


                foreach (Assignment a in assignments)
                {
                    TeamMember userMember = new TeamMember()
                    {
                        CourseUserID = courseUser.ID
                    };

                    Team team = new Team();
                    team.Name = courseUser.UserProfile.LastName + "," + courseUser.UserProfile.FirstName;
                    team.TeamMembers.Add(userMember);

                    db.Teams.Add(team);
                    db.SaveChanges();

                    AssignmentTeam assignmentTeam = new AssignmentTeam()
                    {
                        AssignmentID = a.ID,
                        Team = team,
                        TeamID = team.ID
                    };

                    db.AssignmentTeams.Add(assignmentTeam);
                    db.SaveChanges();

                    //If the assignment is a discussion assignment they must be on a discussion team.
                    if (a.Type == AssignmentTypes.DiscussionAssignment || a.Type == AssignmentTypes.CriticalReviewDiscussion)
                    {
                        DiscussionTeam dt = new DiscussionTeam();
                        dt.AssignmentID = a.ID;
                        dt.TeamID = assignmentTeam.TeamID;
                        a.DiscussionTeams.Add(dt);

                        //If the assignment is a CRD, the discussion team must also have an author team\
                        //Since this CRD will already be completely invalid for use (as its a CRD with only 1 member..) 
                        //we will do a small hack and have them be the author team and review team.
                        if(a.Type == AssignmentTypes.CriticalReviewDiscussion)
                        {
                            dt.AuthorTeamID = assignmentTeam.TeamID;
                        }
                        db.SaveChanges();
                    }

                    
                }
            }
        }

        /// <summary>
        /// Attempts to find an email address to match requested, and adds them to the course if it is found.
        /// </summary>
        /// <param name="courseUser"></param>
        private void attachCourseUserByEmail(CourseUser courseuser)
        {
            UserProfile up = db.UserProfiles.Where(u => u.UserName == courseuser.UserProfile.UserName).FirstOrDefault();

            if (up != null)
            {
                courseuser.UserProfile = up;
                courseuser.UserProfileID = up.ID;
            }
            else
            {
                throw new Exception("No user exists with that email address!");
            }

            courseuser.AbstractCourseID = ActiveCourseUser.AbstractCourseID;

            if ((from c in db.CourseUsers
                 where c.AbstractCourseID == courseuser.AbstractCourseID && c.UserProfileID == courseuser.UserProfileID
                 select c).Count() == 0)
            {
                db.CourseUsers.Add(courseuser);
                db.SaveChanges();

                //Adding the course user to teams so that they can access assignments
                addNewStudentToTeams(courseuser);
            }
            else
            {
                throw new Exception("This user is already in the course!");
            }
        }

        private void createWhiteTableUser(WhiteTable whitetable)
        {
            //do the same thing as createCourseUser but make the function work with our whitetable
            //This will return one if they exist already or null if they don't
            var user = (from c in db.WhiteTableUsers
                        where c.Identification == whitetable.WhiteTableUser.Identification 
                        && c.SchoolID == ActiveCourseUser.UserProfile.SchoolID
                        select c).FirstOrDefault();
            if (user == null || user.CourseID != ActiveCourseUser.AbstractCourseID)
            {
                //user doesn't exist so we got to make a new one or the user exists, but not in this course, create a new user
                //Create userProfile with the new ID
                WhiteTableUser up = new WhiteTableUser();
                up.SchoolID = CurrentUser.SchoolID;
                up.Identification = whitetable.WhiteTableUser.Identification; //courseuser.UserProfile.Identification;
                up.CourseID = whitetable.WhiteTableUser.CourseID;
                

                if (whitetable.WhiteTableUser.Name1 != null)
                {
                    up.Name1 = whitetable.WhiteTableUser.Name1;
                    if (whitetable.WhiteTableUser.Name2 != null)
                        up.Name2 = whitetable.WhiteTableUser.Name2;
                    else
                        up.Name2 = null;
                }
                else
                {
                    up.Name1 = "Pending";
                    up.Name2 = string.Format("({0})", up.Identification);
                }
                if (whitetable.WhiteTableUser.Email != "")
                    up.Email = whitetable.WhiteTableUser.Email;
                else
                {
                    //error check here
                    up.Email = "";
                }
                db.WhiteTableUsers.Add(up);
                db.SaveChanges();

                //Set the UserProfileID to point to our new student
                whitetable.WhiteTableUser = up;
                whitetable.WhiteTableUserID = up.ID;
                whitetable.AbstractCourseID = ActiveCourseUser.AbstractCourseID;
                whitetable.WhiteTableUser.CourseID = ActiveCourseUser.AbstractCourseID;
                //emailWhiteTableUser(whitetable);
            }
                
            
            else //If the CourseUser already has a UserProfile..
            {
                if (whitetable.WhiteTableUser.Name1 != null)
                {
                    user.Name1 = whitetable.WhiteTableUser.Name1;
                    user.Name2 = whitetable.WhiteTableUser.Name2;

                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                }
                whitetable.WhiteTableUser = user;
                whitetable.WhiteTableUserID = user.ID;

                db.Entry(whitetable).State = EntityState.Modified;
                db.SaveChanges();
                //emailWhiteTableUser(whitetable);
            }
        }

        private void clearWhiteTableOnRosterImport()
        {
            var oldUsers = from d in db.WhiteTableUsers
                           where d.CourseID == ActiveCourseUser.AbstractCourseID
                           select d;

            foreach (var user in oldUsers) 
            {
                db.WhiteTableUsers.Remove(user);
                
            }

            db.SaveChanges();
        }

        private UserProfile getEntryUserProfile(RosterEntry entry)
        {
            UserProfile possibleUser = (from d in db.UserProfiles
                                        where d.Identification == entry.Identification 
                                        //&& d.UserName == entry.Email
                                        select d).FirstOrDefault();
            return possibleUser;
        }

        private CourseUser getPendingUserOnRoster(RosterEntry entry)
        {
            CourseUser pendingUser = (from d in db.CourseUsers
                                      where d.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                                      && d.UserProfile.Identification == entry.Identification
                                      && d.UserProfile.UserName == entry.Email
                                      && d.AbstractRoleID == (int)CourseRole.CourseRoles.Pending
                                      select d).FirstOrDefault();

            return pendingUser;
        }

        private void emailCourseUser(CourseUser user)
        {
            string subject = "Welcome to " + user.AbstractCourse.Name;
            string link = "https://osble.org";

            string message = "Dear " + user.UserProfile.FirstName + " " + user.UserProfile.LastName + @", <br/>
            <br/>
            Congratulations! You have been enrolled in the following course at osble.org: " + ActiveCourseUser.AbstractCourse.Name +
            "You may access this course by <a href='" + link + @"'>clicking on this link</a>. 
            <br/>
            <br/>
            ";

            message += @"Best regards,<br/>
            The OSBLE Team in the <a href='www.helplab.org'>HELP lab</a> at <a href='www.wsu.edu'>Washington State University</a>";

            Email.Send(subject, message, new List<MailAddress>() { new MailAddress(user.UserProfile.UserName) });
        }

        private void emailWhiteTableUser(WhiteTable whitetable)
        {
            var WTU = whitetable.WhiteTableUser;

            string subject = "Welcome to OSBLE";
            string link = "https://osble.org/Account/AcademiaRegister?email=" 
                + WTU.Email + "&firstname=" + WTU.Name2 + "&lastname=" + WTU.Name1 + "&identification=" + WTU.Identification; 

            string message = "Dear " + WTU.Name2 + " " + WTU.Name1 + @", <br/>
                <br/>
                Congratulations! You have been enrolled in the following course at osble.org: " + ActiveCourseUser.AbstractCourse.Name +
            " In order to access this course, please create an OSBLE account with OSBLE first by " +
            "<a href='" + link + @"'>clicking on this link</a>. 
                <br/>
                <br/>
                ";

            message += @"Best regards,<br/>
                The OSBLE Team in the <a href='www.helplab.org'>HELP lab</a> at <a href='www.wsu.edu'>Washington State University</a>";

            if(WTU.Email != null)
                Email.Send(subject, message, new List<MailAddress>() { new MailAddress(WTU.Email) });
            
        }

        /// <summary>
        /// yc: this is for an individual email to be resent
        /// this would occur when an instructor clicks the email button on a student
        /// </summary>
        /// <param name="wtUser"></param>
        /// <returns></returns>
        [CanModifyCourse]
        public ActionResult resendWhiteTableEmail(int wtUserId)
        {

            //find user
            WhiteTableUser wtUser = (from c in db.WhiteTableUsers
                                     where c.ID == wtUserId &&
                                     c.CourseID == ActiveCourseUser.AbstractCourseID
                                     select c).FirstOrDefault();

            if (wtUser != null)
            {
                string subject = "Welcome to OSBLE";
                string link = "https://osble.org/Account/AcademiaRegister?email="
                    + wtUser.Email + "&firstname=" + wtUser.Name2 + "&lastname=" + wtUser.Name1 + "&identification=" + wtUser.Identification;

                string message = "Dear " + wtUser.Name2 + " " + wtUser.Name1 + @", <br/>
                <br/>
                This email was sent to notify you that you have been added to " + ActiveCourseUser.AbstractCourse.Name +
                " To access this course you need to create an account with OSBLE first. You may create an account " +
                "by <a href='" + link + @"'>following this link</a>. 
                <br/>
                <br/>
                ";
                message += @"Best regards,<br/>
                The OSBLE Team in the <a href='www.helplab.org'>HELP lab</a> at <a href='www.wsu.edu'>Washington State University</a>";

                if (null != wtUser.Email)
                    Email.Send(subject, message, new List<MailAddress>() { new MailAddress(wtUser.Email) });

                return RedirectToAction("Index", "Roster", new { notice = wtUser.Name2 + " " + wtUser.Name1 + " has been sent an email to join this course" });
            }
            else
            {
                return View("Index");
            }
            
        }

        /// <summary>
        /// yc: batch email sending for white listed users, no params, grabs its from active course users's course id
        /// </summary>
        /// <returns>back to index</returns>
        [CanModifyCourse]
        public ActionResult BatchEmailWhiteTable()
        {
            //getusers
            List<WhiteTableUser> wtu = (from w in db.WhiteTableUsers
                                        where w.CourseID == ActiveCourseUser.AbstractCourseID &&
                                        w.Email != ""
                                        select w).ToList();

            foreach (WhiteTableUser wtUser in wtu)
            {
                string subject = "Welcome to OSBLE.org";
                string link = "https://osble.org/Account/AcademiaRegister?email="
                    + wtUser.Email + "&firstname=" + wtUser.Name2 + "&lastname=" + wtUser.Name1 + "&identification=" + wtUser.Identification;

                string message = "Dear " + wtUser.Name2 + " " + wtUser.Name1 + @", <br/>
                <br/>
                This email was sent to notify you that you have been added to " + ActiveCourseUser.AbstractCourse.Name +
                " To access this course you need to create an account with OSBLE first. You may create an account " +
                "by <a href='" + link + @"'>following this link</a>. 
                <br/>
                <br/>
                ";
                message += @"Best regards,<br/>
                The OSBLE Team in the <a href='www.helplab.org'>HELP lab</a> at <a href='www.wsu.edu'>Washington State University</a>";
                Email.Send(subject, message, new List<MailAddress>() { new MailAddress(wtUser.Email) });
            }


            return RedirectToAction("Index", "Roster", new { notice = "Whitelisted users have been sent an invintation to join the course" });
        }
    }
}
