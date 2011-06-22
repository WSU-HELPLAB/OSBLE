using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

/*
 * JQUERY TOOL TIP
 * 
 * USAGE:
 * place 
 *   @Helpers.CreateToolTip(ToolTips.[variableName])
 * wherever you want a tooltip icon. You must place
 *   @using OSBLE.Models
 * once in the cshtml file you are using to get access
 * to this class and intellisense. HTML is supported.
 * 
 * 
 * jquery-tooltip.css and jquery-tooltip.js are included 
 *   in the /Views/Shared/_Layout.cshtml file
 * 
 * See /App_Code/Helpers.cshtml for code
 * 
 * KNOWN BUGS:
 *   in IE, if an individual item is wider than 312 px (such as a string with no spaces), the sides are not visible 
 * 
 */

namespace OSBLE.Models
{
    public static class ToolTips
    {

        // used to test if html, etc works
        public static string test = "<p>this is the first thing</p><p>this <a href='http://xkcd.com/627/'>is the</a> second thing</p> " +
            "<p>kahdsfjjdaadsfdshskfjhksajhflkadsfdsfasfdfffasdjsahlfdkjsahkjhdflfkjfads</p> abcdef ghijkl <em>m no p asdf</em> asdf asdf <strong>asdf " +
            "</strong>asdf asdf asdf  asdf sadf <u>sadf <i>asdf <b>asdf</b> sadf</i> asd</u> fasdf <br />asdf asdf asdf asd fasdf asdf";



        #region CourseToolTips

            public static string CourseInformation = "Basic course information.";
            public static string CoursePrefix = "The prefix for the department of the course.";
            public static string CourseNumber = "Such as 101.";
            public static string CourseName = "Full title of course.";
            public static string CourseSemester = "Semester the course is being taught.";
            public static string CourseYear = "Year the course is being taught.";

            public static string CourseSchedule = "This section allows the instructor to set specifically the range of dates this course is held.";
            public static string CourseStartDate = "This is when the course begins, most likely the beginning of the semester.";
            public static string CourseEndDate = "This is the date when the course ends, most likely the end of the semester.";

            public static string CourseMeetingTimes = "This section allows for the addition of multiple meeting times/ places each week.";

            public static string CourseBreaks = "This section allows for days to be taken off of the course during the normal meetings times, such as for national holidays.";
            public static string CourseShowMeetings = "Showing course meetings and breaks on the calendar may or may not be necessary.";

            public static string CourseSettings = "This section allows the instructor to set some advanced features for the students' permissions for this course.";
            public static string CourseStudentPost = "Allow students to post new items to the dashboard or not.";
            public static string CourseStudentReply = "Allow students to post replies to posts on the dashboard or not.";
            public static string CourseStudentEvent = "Allow students to create events or not.";
            public static string CourseInstructorEventApproval = "If this is checked, any event created by a student will require the instructor to review and approve it before it shows up on the calendar";
            public static string CourseInactive = "Only instructors and observers can log in while a course is set to inactive. ";
            public static string CourseCalendarEvents = "The amount of time in advance that this course shows events in the course calendar. ";
        
            public static string CourseGradingScheme = "Allows you to curve the grades.";

            public static string CourseLatePolicy = "This determines how the automatic grading will handle late assignments.";

            public static string CourseSave = "You must click this button to save the changes made in the above form, otherwise all changes will be lost.";

        #endregion


        #region HomeToolTips

            public static string HomeNotifications = "This is where you get information about things that have occurred.";
            public static string HomeEvents = "This is where you can see upcoming events and deadlines.";

            public static string HomeActivities = "This is where informational updates can be read and posted.";
            public static string HomePostActivity = "Submit this post to be created.";
            public static string HomePostToAllCourses = "Post this activity to all courses that you currently teach.";
            public static string HomeEmailToClass = "If this option is checked, this post will be emailed to all students affected.";
            public static string HomeShowAll = "Show all posts from all courses in the feed.";
            public static string HomeShowOnly = "Show only posts from the current course in the feed.";

            public static string HomeLinks = "These are items pertinent to this class, such as files and websites.";
            public static string HomeLinksEdit = "Click here to edit the items shown in this area.";

        #endregion


        #region NotificationToolTips

            public static string NotificationMain = "This page displays all the notifications you have received, both read and unread.";
            public static string NotificationRead = "These are notificaitons that you have already read.";
            public static string NotificationUnread = "These are notifications that you have yet to mark as read.";

        #endregion


        #region AssignmentToolTips

            public static string AssignmentTitle = "Create one of two types of assignments using this interface, Basic or Studio. ";
            public static string AssignmentCreate = "Click here to create an assignment. You will select the type of assignment through a dialog box.";
            public static string AssignmentCurrent = "This is a list of all existing assignment for this course.";

        #endregion


        #region BasicAssignmentToolTips

            public static string BasicAssignmentTitle = "A basic assignment is one that is not worked on with the specialized tools provided by OSBLE.";

            public static string BasicAssignmentName = "The name that will be displayed to a user when looking at the list of assignments.";
            public static string BasicAssignmentDescription = "Describe the assignment as completely as possible.";
            public static string BasicAssignmentReleaseDate = "This is the date and time that the assignment will first show up for users.";
            public static string BasicAssignmentDueDate = "This is the last date and time to turn in the assignment. See the <strong>Late Policy</strong> section below to determine how automatic deductions will be handled for assignments after this date. ";
            public static string BasicAssignmentIsDraft = "Is this assignment a draft or not? <what is a draft?>";
            public static string BasicAssignmentIsTeam = "Is this assignment done in teams or not? (Selecting this option displays the method for specifying teams.)";

            public static string BasicAssignmentTeamsPreviouslyDefined = "Select this is teams have been already defined";
            public static string BasicAssignmentTeamsRandom = "Randomly create teams.";
            public static string BasicAssignmentTeamsManual = "Manually select which students are on which teams.";

            public static string BasicAssignmentInstructorLineReview = "Check this box to allow the instructor to review the assignments on a per-line basis.";
            public static string BasicAssignmentUseRubric = "Check this box to create a rubric that will determine how this assignment is graded. (You have to click <em>Edit Rubric</em> to specify the rubric.)";

            public static string BasicAssignmentDeliverables = "Deliverables are intermediary submissions of an assignment before the final submission.";
            public static string BasicAssignmentDeliverableFileName = "Determine the name of this deliverable.";
            public static string BasicAssignmentDeliverableFileType = "Determine the type of this deliverable.";
            public static string BasicAssignmentDeliverableAdd = "You must click this button to save the settings from this section. When you click this button, the settings you chose will show up below, confirming that they were added to this assignment.";

            public static string BasicAssignmentGrading = "This section is used to specify specific methods of grading for htis assignment.";
            public static string BasicAssignmentGradingCategory = "Select a predefined category to place this assignment into.";
            public static string BasicAssignmentAddToGradebook = "Check this to add this assignment to the gradebook.";
            public static string BasicAssignmentGradingPoints = "This setting determines how much this indiviual assignment is worth when graded.";

            public static string BasicAssignmentLatePolicy = "Read the following sentences and edit the textboxes to specify the late policy of this individual assignment.";

            public static string BasicAssignmentSave = "You must click this button to save the changes you made above.";

        #endregion


        /*
         * TEMPLATE FOR ADDING TOOLTIPS (FOR ORGANIZATIONAL PURPOSES)
         *
        #region ToolTips

            public static string  = "";

        #endregion
         *
         */


    }
}