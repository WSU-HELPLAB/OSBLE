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

            public static string CourseInformation = "Required course information. The prefix is typically the abbreviation of the department the course categorized with.";
            public static string CourseSchedule = "This section allows the instructor to set specifically the range of dates this course is held. Typically these values will be the beginning and end of the semester or quarter the course is being taught, but allows for any desired range of dates.";
            public static string CourseMeetingTimes = "This section allows for the addition of multiple meeting times and/ or places each week, such as a lab section and a lecture section.";
            public static string CourseBreaks = "This section allows for days to be taken off of the course during the normal meetings times, such as for national holidays.";

            public static string CourseStudentPost = "This option allows for students to post items to the activity feed of the course, which will show up for every member of that course.    ";
            public static string CourseStudentReply = "This option allows students to reply to items posted to the activity feed.";
            public static string CourseStudentEvent = "Events that are posted in the course calendar will show up for every member of the course.";
            public static string CourseInstructorEventApproval = "Any event created by a student will require the instructor to review and approve it before it shows up on the calendar.";
            public static string CourseInactive = "Only instructors and observers can log in while a course is set to inactive. ";
            public static string CourseCalendarEvents = "Events outside this window of time will not show up on the course calendar.";
        
            public static string CourseGradingScheme = "Using this section, you may change the cutoff for grades. This is something like automatic grade curving.";
            public static string CourseLatePolicy = "This determines how the system will penalize for late assignments. Read the sentences below and set the options as desired.";

        #endregion


        #region HomeToolTips

            public static string HomeNotifications = "This is where you get information about things that have occurred.";
            public static string HomeEvents = "This is where you can see upcoming events and deadlines.";

            public static string HomeActivities = "The <em>Activity Feed</em> is where informational updates can be posted, read, and replied to.";

            public static string HomeLinks = "These are items pertinent to this class, such as files and websites.";

        #endregion


        #region NotificationToolTips

            public static string NotificationMain = "This page displays all the notifications you have received, both read and unread.";

        #endregion


        #region AssignmentToolTips

            public static string AssignmentTitle = "When you click \"Create New Assignment\" below, a dialog box will prompt you for which type of assignment you would like to create with .";
            public static string AssignmentBasic = "Basic Assignments are...";
            public static string AssignmentStudio = "Studio Assignments are...";

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

        
        #region RosterToolTips

            public static string RosterAddUpdate = "Click this link to view the interface for adding users to the database either using a file or manually.";
            public static string RosterSingleUser = "This section allows you to add an individual user to the class, either by their school identification number or their email.";
            public static string RosterImport = "This section allows you to import a list of users from a comma-separated (<em>.csv</em>) file to the class.";
            public static string RosterQuickFilter = "Type in all or part of a user's name to filter this view to show only users who match that criteria.";
            public static string RosterCommunityUser = "";

            // because in /Views/Roster/Index.cshtml there is a dynamically generated section, this data structure must be used
            public static IDictionary<string, string> RosterRolesDictionary = new Dictionary<string, string> { 
                {"Observer","Observers are allowed to view the course information without being allowed to participate as either instructors or students are."},
                {"Instructor","Instructors are the users who have control over the grades, assignments, and other activities in a course."},
                {"Student","Students are the individuals who participate in the course for turning in assignments, receiving grades, and other activities."},
                {"TA","Teaching assistant who helps with grading assignments."},
                {"Moderator","Moderators are...?"},
                {"Leader","Community leader...?"},
                {"Participant","Community participant...?"} };

            public static string RosterUserName = "This is the name this user will be publicly identified by in mail, posts, assignments, and other content.";
            public static string RosterEmail = "This must be the email address this user will use with OSBLE.";
            public static string RosterSchoolID = "Input the identification number for this user to indicate who is being added to this course.";
            public static string RosterRole = "Select what role this person has for this course. ";
            public static string RosterSection = "This is the section (<em>Enter 0 to add this user to all sections</em>).";
            public static string RosterSave = "To save the changes made on this page, you must click this submit button.";
            public static string RosterBack = "To discard changes made here and go back to the main roster page, click this link.";

        #endregion
        

        #region CommunityToolTips
        
            public static string CommunityName = "This is the full name of this community.";
            public static string CommunityDescription = "Describe what this community is and why you made it here. ";
            public static string CommunityNickname = "This is a 3-4 letter nickname (identifier) for thisw community to be shown where there is not enough space to display the whole name of the community. Ideally, this nickname will be connected with the name somehow, such as an acronym or abbreviation.";
            public static string CommunityAllowPosting = "If you want participants and not just leaders to be able to post events to this community, you must check the box.";
            public static string CommunityCalendarEvents = "For every user that is a part of this group, events will be displayed on their calendar this many weeks in advance.";
            public static string CommunitySave = "You must click the button to save any changes you made on this page.";

        #endregion

        
        #region MailToolTips

            public static string MailNewMessage = "Use the search box below to create a new message to a user of OSBLE.";
            public static string MailRecipient = "Type in some letters in the user's name and a list of users that match that search term will pop up. Select the desired user from that list to continue and compose your message.";
            public static string MailInbox = "Your mail inbox shows all messages that have been sent to you. Messages that have yet to be read are bold. There is an (X) on the right of each message to allow you to delete messages.";
            public static string MailOutbox = "Your mail outbox shows all messages that you have sent to other users. Messages that have yet to be read are bold. There is an (X) on the right of each message to allow you to delete messages.";

            public static string MailCreateTo = "This is the person this message will be sent to. If you want to change the recipient, you must create a new message. This message will not be saved if you clikc the back button.";
            public static string MailCreateSubject = "This is the title of the message you are sending.";
            public static string MailCreateMessage = "This is the body of the message you are sending.";
            public static string MailSave = "In order to send this message, you must click here. If you click any other link, this message will not be saved.";
            public static string MailBack = "If you don't want to send this message, click here.";

            public static string MailDelete = "Click here to permanently delete this message. <em>Note: This action cannot be undone.</em>";
            public static string MailReply = "Click here to reply to this message. The previous message will be quoted in the message body. If you do not want to quote the original message, you can delete it while editing the message.";

        #endregion

        
        #region EventToolTips

            public static string EventCreate = "Click here to create a new event.";
            public static string EventPastEvents = "These are events that have already occurred that you were invited to or part of. (Past events are based on the date and time specified in creation).";
            public static string EventCurrentEvents = "These are events that have yet to occur that you were invited to or part of.";

            public static string EventTitle = "This is the title this event will have. This is what will show up on the event information box as bold";
            public static string EventDescription = "Use this section to completely describe this event, if you so desire. If there is not a section for a specific piece of information that you feel is important to include about this event, such as the length of time it will be or if there is a specific meeting place, mention it in this section.";
            public static string EventDate = "This is the day of the year that the event will occur.";
            public static string EventTime = "This is the time of day that the event will occur.";
            public static string EventLinkTitle = "If you want to have a link to another web location, this is the title that the hyperlink will have, rather than the exact web address. If you want the hyperlink to be shown as its web address, place the address in this section and in the link section below.";
            public static string EventLink = "Place the web location you want to link to in this section.";
            public static string EventSave = "You must click here to save any modifications made on this page.";
            public static string EventBack = "If you don't want to save any of the modifications you made on this page, or if you accidentally managed to come to this page, click here.";

        #endregion

        
        #region AdminToolTips

            public static string AdminTitle = "In this section, you can edit, delete, view the details of, or impersonate a user using the links next to their name. When you impersonate a user, you see what they would when they log in to OSBLE, such as messages, notifications, and assignment feedback, among other things. This feature is useful for checking how OSBLE looks and feels for users to ensure that they can utilize this system to its fullest.";

            public static string AdminUserName = "This is the name that these individuals use to log in to the system. It must be their email address.";
            public static string AdminUserSchool = "This is the school that this user is associated with.";
            public static string AdminUserID = "This is the identification number of the users. These are typically used by the system itself rather than users. ";
            public static string AdminUserFirstName = "This name and the last name will be displayed when users interact.";
            public static string AdminUserLastName = "This name and the first name will be displayed when users interact.";
            public static string AdminUserIsAdmin = "This checkbox indicates whether or not this user has administrator permissions on OSBLE.";
            public static string AdminUserIsCourseCreator = "This checkbox indicates whether or not the user can create courses.";

            public static string AdminSave = "You must click this button in order to save any changes you made above.";
            public static string AdminBack = "If you don't want to save any changes made on this page or you just want to navigate back to the list of users, click here.";
            public static string AdminEdit = "You can edit features of this user by following this link.";
            public static string AdminDelete = "Click here to delete this user. <em>Note: This action can NOT be undone and this button offers no additional confirmation window.</em>";

        #endregion

        
        #region AccountToolTips

            public static string AccountProfilePicture = "Your profile picture will be displayed in a number of places such as your posts as a form of identification in addition to your name. <br /><br /> You must click \"Upload Picture\" after finding a picture to save it to the system. <br /><br /> If you no longer wish to have a profile picture, use the delete button to remove the picture from the server. <br /><br /> <em>Supported Formats: BMP, GIF, EXIF, JPG, PNG and TIFF</em>";
            public static string AccountChangePassword = "New passwords must be at least " + Membership.MinRequiredPasswordLength + " characters long. ";
            public static string AccountEmailNotifications = "The email address OSBLE uses to send information to your account is the email address you use to log in.";
            public static string AccountMenuOptions = "Hidden courses/communities will not appear in your course list or your activity feed and must be unchecked to appear again. <br /><br /> Your default course will be the active course each time you log on to OSBLE.";

            public static string AccountLogOnName = "This is the email address you used when registering your account with this system.";
            public static string AccountLogOnPassword = "If you have forgotten your password, <a href='ResetPassword'>reset your password</a>.  ";
            public static string AccountLogOnRemember = "If you allow this option, you will be automatically logged in when you access this system.";

            public static string AccountResetPassword = "This email address must be the same one used to log in to OSBLE. If you don't have an account, <a href='Register'>register now</a>.";

            public static string AccountContactEmail = "This is the email address you would like us to reply to. ";
            public static string AccountContactMessage = "Briefly describe the reason for contacting us below.";

            public static string AccountRegisterLogin = "Your email address will be the name you use to log in to OSBLE and will not be shared with anyone else using the system <br /><br /> Your password must be at least " + Membership.MinRequiredPasswordLength + " characters long.";
            public static string AccountRegisterID = "This identification number is your school identification number. This allows instructors to use OSBLE to identify users based on their school identification numbers. ";

        #endregion


        /*
         * TEMPLATE FOR ADDING TOOLTIPS (FOR ORGANIZATIONAL PURPOSES)
         *
        #region ToolTips

            public static string  = "";

        #endregion
         *
         * 
         * ALSO:
         *     To prevent additional unecessary whitespace in a tooltip, avoid using <p> tags at the beginning or end of the tooltip text.
         * 
         */


    }
}