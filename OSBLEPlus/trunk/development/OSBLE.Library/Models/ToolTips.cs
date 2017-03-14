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


        public static string CoursePrefix = "(Required) course prefix may have a maximum of 8 characters, e.g., \"CptS\"";
        public static string CourseNumber = "(Required) course number may have a maximum of 8 characters, e.g., \"443\"";
        public static string CourseName = "(Required) course name may have a maxiumum of 100 characters, e.g., \"Human-Computer Interaction\"";
        public static string CourseTerm = "(Required) the term in which this class is held: Fall, Winter, Spring, Summer";
        public static string CourseYear = "(Required) the year in which this class is held, e.g., 2011";
        public static string CourseStartDate = "(Required) the date of the first day of class";
        public static string CourseEndDate = "(Required) the date of the last day of class (or the final exam)";
        public static string CourseTimeZone = "Specify the local timezone for this course. All posts, emails, and assignment submissions will be time stamped based on this timezone";
        public static string CourseMeetingTimes = "Click on the \"+\" to specify regular meeting times for lectures, labs, etc. These can be optionally included on the course calendar.";
        public static string CourseBreaks = "Click on the \"+\" button to specify course breaks and holidays. These can optionally be included on the course calendar.";

        public static string CourseStudentPost = "Check this box to allow students to post new messages to the activity feed.";
        public static string CourseStudentReply = "Check this box to allow students to reply to messages posted to the activity feed.";
        public static string CourseStudentEvent = "Check this box to allow students to post items to the course calendar.";
        public static string CourseInstructorEventApproval = "Check this box to require instructor to approve items before they appear on the course calendar.";
        public static string CourseHideMail = "Check this box to hide the \"Mail\" links for this course. This can be done if you do not wish to use the OSBLE Mail system or wish to temporarily disable direct access to it.";
        public static string CourseInactive = "Check this box to make the course \"inactive.\"  Only instructors and observers can access a course when it is set to inactive.";
        public static string CourseCalendarEvents = "Specify the number of weeks of events to show in the course calendar by default. Events outside this window of time can still be accessed by clicking on the \"Show All Events\" link.";
        public static string CourseGradingScheme = "Using this section, you may define the grading scheme for the course. For each letter grade, you must specify the minimum percentage required to receive that grade.";
        public static string CourseLatePolicy = "Using this section, you specify the penalties that OSBLE will automatically apply to late assignments.";

        public static string CourseSearch = "A list of classes you may request enrollment for. Upon enrollment you will be notified via an email from the course instructor.";
        public static string CommunitiesSearch = "A list of communities you may request to join. Upon joining you will be notified via an email from the communitie leader.";        
        #endregion

        #region FeedToolTips

        public static string VisibilityEveryone = "Your post will be visible to all users in the course.";
        public static string VisibilitySection = "Your post will be visible to all users in your section (including instructors and TAs).";
        public static string VisibilityInstructors = "Your post will be visible to only yourself and instructors.";
        public static string VisibilityTAs = "Your post will be visible to only yourself and TAs.";
        public static string VisibilityInstructorsAndTAs = "Your post will be visible to only yourself, instructors, and TAs.";
        public static string VisibilitySelectedUsers = "Your post will be visible to only the selected users.";

        public static string AnonymousPost = "Your post will be anonymized by removing your name from public view. NOTE: Checking the anonymous option will reset post visibility to 'Everyone'";

        #endregion

        #region HomeToolTips

        // There are currently one tooltips in the Home section
        public static string FileUploader = "Right clicking \"Files and Links\" will bring up a dialog for the current course file manager";

        #endregion


        #region NotificationToolTips

        public static string NotificationMain = "This page displays all the notifications you have received, both read and unread.";

        #endregion


        #region AssignmentToolTips

        public static string AssignmentTitle = "When you click \"Create New Assignment\" below, a dialog box will prompt you for which type of assignment you would like to create, either a Basic Assignment or a Studio Assignment. <br /><br /> Basic Assignments have the final products turned in then graded by an official grader.  <br /><br /> Studio Assignments are typically workshopped by peers prior to final grading.";
        public static string AssignmentLoadingError = "In some cases, this section may fail to load. First try reloading it by opening and closing this section. <br /><br /> It may also fail to load due to a feature of AVG Free 2011, which can be disabled by following instructions at <a href='http://free.avg.com/us-en/faq.num-2964'>AVG's FAQ page</a>.";
        public static string DiscussionPostRole = "Check this box if you want to hide the role of discussion participants (e.g., student, instructor, moderator, observer).";

        #endregion

        #region CloningToolTips
        public static string CloneCourse = "Select a course to be cloned. General course information will be automatically generated. Files and Links will not be copied.";
        public static string CloneCourseInfo = "The following information has been generated from a previously existing course. The term, year and any subsequent course meetings/breaks need to be updated.";
        public static string CloneAssignments = "Select the following assignments to be cloned. All due/publishing dates will be shifted, and assignment teams will need to be adjusted.";
        public static string CloneAssignmentFromCourse = "Select the course that has the previous assignment to clone";

        #endregion

        #region BasicAssignmentToolTips

        public static string BasicAssignmentTitle = "A basic assignment is one that is not worked on with the specialized tools provided by OSBLE.";

        public static string BasicAssignmentReleaseDate = "This is the date and time that the assignment will first show up for users.";
        public static string BasicAssignmentDueDate = "This is the last date and time to turn in the assignment. See the <strong>Late Policy</strong> section below to determine how automatic deductions will be handled for assignments turned in after this date. ";
        public static string BasicAssignmentIsTeam = "Team assignments allow groups to collaborate on " +
            "the creation of deliverables. Only a single member of the team must turn in the deliverable " +
            "and it will be attributed to all members of the team. <br /><br /> OSBLE assumes that all " +
            "assignments are team-based.  If this assignment is not team-based, click the &quot;Next&quot; " +
            "button at the bottom of the screen.  Unassigned students will automatically be placed into " +
            "separate, individual teams.";
        public static string BasicAssignmentDeliverables = "Deliverables are the actual assignment types that are expected to be turned in. Each assignment can have multiple files and file types to turn in.";

        public static string BasicAssignmentUseRubric = "Check this box to create a rubric that will determine how this assignment is graded. (You have to click <em>Edit Rubric</em> to specify the rubric.)";
        public static string BasicAssignmentEnableInlineComments = "Inline comments are allowed to have up to six categories each with an unlimited number of options. This allows for specific comments to be created using this template on each deliverable associated with this assignment. <br /><br /> Create a new set below or select a previously configured set. When you creat your own custom set, it will be saved for future use once this assignment is created.";
        public static string BasicAssignmentAddToGradebook = "Adding an assignment to the gradebook means that the results of this assignment will affect the final grade of students.";

        public static string BasicAssignmentLatePolicy = "Read the following sentences and edit the textboxes to specify the late policy of this individual assignment.";
        public static string BasicAssignmentIsDraft = "Draft assignments are typically incomplete samples of a final assignment. This is just a mark placed on the assignment and grading is determined in the <strong>Grading</strong> section below.";

        #endregion


        #region RosterToolTips

        public static string RosterSingleUser = "This section allows you to add an individual user to the class, either by their school identification number or their email.";
        public static string RosterImport = "This section allows you to import a list of users from a comma-separated (<em>.csv</em>) file to the class.";
        public static string RosterQuickFilter = "Type in all or part of a user's name to filter this view to show only users who match that criteria.";
        //public static string RosterCommunityUser = "";

        // because in /Views/Roster/Index.cshtml there is a dynamically generated section, this data structure must be used (it will throw an errror if any of these are missing)
        public static IDictionary<string, string> RosterRolesDictionary = new Dictionary<string, string> { 
                {"Observer",    "Observers are allowed to view the course information without being allowed to participate as either instructors or students are."},
                {"Instructor",  "Instructors are the users who have control over the grades, assignments, and other activities in a course."},
                {"Student",     "Students are the individuals who participate in the course for turning in assignments, receiving grades, and other activities."},
                {"TA",          "Teaching assistant who helps with grading assignments."},
                {"Withdrawn",   "Students that have withdrawn from the course.  Any OSBLE content created by these users will remain in the system."},
                {"Pending",   "Pending users awaiting approval to join the course/community."},
                {"TrustedCommunityMember","Trusted community members can participate in communities and manage shared community files."},
                {"Moderator",   "Moderators can be injected into discussion assignments. They are responsible for prompting participants and keeping discussions on track."},
                {"Leader",      "Community leaders administrate the community by adding and removing users along with managing files."},
                {"Participant", "Community participants participate in communinty dicussions and download shared community files; however they do not have any file management privledges."},
                {"Assessment Committee Chair",""},
                {"Assessment Committee Member",""},
                {"ABET Evaluator",""}};

        public static string RosterWhitetableRoleRole = "Whitelisted users are approved to  enroll in the course, but they do not have an OSBLE account. <em><strong>Provided a valid email address</strong></em>, these students will be invited to register and enroll into this course. If an email address on the roster that was uploaded, these students will be notified. No e-mail address is on file for students highlighted in red. You will need to ask these students to create an OSBLE account before they can be enrolled in the course.";
        public static string RosterUserName = "This is the name this user will be publicly identified by in mail, posts, assignments, and other content.";
        public static string RosterEmail = "This must be the email address this user registered with for OSBLE.";
        public static string RosterSchoolID = "Input the school/ organization identification number for this user to indicate who is being added to this course.";
        public static string RosterRole = "Select what role this person has for this course. (Instructor, TA, Student, Moderator, and Observer explanations go here.)";
        public static string RosterSection = "Select multiple sections to add user to all selected sections.";
        public static string RosterIdentificationNumber = "This is users identification number given by the school";


        #endregion


        #region CommunityToolTips

        public static string CommunityNickname = "This is a 3-4 letter nickname (identifier) for thisw community to be shown where there is not enough space to display the whole name of the community. Ideally, this nickname will be connected with the name somehow, such as an acronym or abbreviation.";
        public static string CommunityAllowPosting = "If you want participants and not just leaders to be able to post events to this community, you must check the box.";
        public static string CommunityCalendarEvents = "For every user that is a part of this group, events will be displayed on their calendar this many weeks in advance.";
        public static string CommunitySave = "You must click the button to save any changes you made on this page.";

        #endregion


        #region MailToolTips

        public static string MailRecipient = "Type in a few letters of the recipient's name. You will be presented with a list of possible matches. Select the desired recipient from the list to add that recipient. You can repeat this process to add multiple recipients.";
        public static string MailCreateTo = "This is the person this message will be sent to. If you want to change the recipient, you must create a new message.";
        public static string MailDelete = "Click here to permanently delete this message. <em>Note: This action cannot be undone.</em>";
        public static string MailReply = "Click here to reply to this message. The previous message will be quoted in the message body. If you do not want to quote the original message, you can delete it while editing the message.";

        #endregion


        #region EventToolTips

        public static string EventPastEvents = "These are events that have already occurred that you were invited to or part of. (Past events are based on the date and time specified in creation).";
        public static string EventCurrentEvents = "These are events that have yet to occur that you were invited to or part of.";

        public static string EventDescription = "Use this section to completely describe this event, if you so desire. If there is not a section for a specific piece of information that you feel is important to include about this event, such as the length of time it will be or if there is a specific meeting place, mention it in this section.";
        public static string EventLinkTitle = "If you want to have a link to another web location, this is the title that the hyperlink will have, rather than the exact web address. If you want the hyperlink to be shown as its web address, place the address in this section and in the link section below.";

        #endregion


        #region AdminToolTips

        public static string AdminTitle = "In this section, you can edit, delete, view the details of, or impersonate a user using the links next to their name. <br /><br /> When you impersonate a user, you see what they would when they log in to OSBLE, such as messages, notifications, and assignment feedback, among other things. This feature is useful for checking how OSBLE looks and feels for users to ensure that they can utilize this system to its fullest.";

        public static string AdminUserName = "This is the name that these individuals use to log in to the system. It must be their email address.";
        public static string AdminUserSchool = "This is the school that this user is associated with.";
        public static string AdminUserID = "This is the identification number of the users. These are typically used by the system itself rather than users. ";
        public static string AdminUserFirstName = "This name and the last name will be displayed when users interact.";
        public static string AdminUserLastName = "This name and the first name will be displayed when users interact.";
        public static string AdminUserIsAdmin = "This checkbox indicates whether or not this user has administrator permissions on OSBLE.";
        public static string AdminUserIsCourseCreator = "This checkbox indicates whether or not the user can create courses.";

        public static string AdminDelete = "Click here to delete this user. <em>Note: This action can NOT be undone and this button offers no additional confirmation window.</em>";

        #endregion


        #region AccountToolTips

        public static string AccountProfilePicture = "Your profile picture will be displayed in a number of places such as your posts as a form of identification in addition to your name. <br /><br /> You must click \"Upload Picture\" after finding a picture to save it to the system. <br /><br /> If you no longer wish to have a profile picture, use the delete button to remove the picture from the server. <br /><br /> <em>Supported Formats: BMP, GIF, EXIF, JPG, PNG and TIFF</em>";
        public static string AccountChangePassword = "New passwords must be at least " + Membership.MinRequiredPasswordLength + " characters long. ";
        public static string AccountEmailNotifications = "The email address OSBLE uses to send information to your account is the email address you use to log in.";
        public static string AccountMenuOptions = "Hidden courses/communities will not appear in your course list or your activity feed and must be unchecked to appear again. <br /><br /> Your default course will be the active course each time you log on to OSBLE.";

        public static string AccountLogOnRemember = "If you allow this option, you will be automatically logged in when you access this system.";

        public static string AccountResetPassword = "This email address must be the same one used to log in to OSBLE. If you don't have an account, <a href='Register'>register now</a>.";

        public static string AccountContactEmail = "This is the email address you would like us to reply to. ";
        public static string AccountContactMessage = "Briefly describe the reason for contacting us below.";

        public static string AccountRegisterLogin = "Your email address will be the name you use to log in to OSBLE and will not be shared with anyone else using the system <br /><br /> Your password must be at least " + Membership.MinRequiredPasswordLength + " characters long.";
        public static string AccountRegisterID = "This identification number is your school identification number. This allows instructors to use OSBLE to identify users based on their school identification numbers. ";
        public static string CurrentuserInformation = "This is your current user information. You can verify your institution/name/email/ID and update your email address or school identification.";

        #endregion


        #region PeerReviewToolTips

        public static string PeerReviewName = "";
        public static string PeerReviewAbstractAssignmentID = "";
        public static string PeerReviewUseOnlySubmittedStudents = "If you want only students who submitted this assignment to participate, you must check this box.";
        public static string PeerReviewUseModerators = "If you want to use Moderators for this peer review, you must check this box.";
        public static string PeerReviewIsAuthorAnonymous = "If you want the author of this submission to be anonymous, you must check this box.";
        public static string PeerReviewIsReviewersAnonymous = "If you want the reviewer of this peer review to be anonymous, you must check this box.";
        public static string PeerReviewIsReviewersRoleAnonymous = "If you want the reviewers role in this peer review to be anonymous, you must check this box.";
        public static string PeerReviewUseInlineComments = "If you want reviewers of this submission to use inline comments to record issues, you must check this box.";
        public static string PeerReviewUseRubric = "If you would like reviewers to use a rubric to review this submission, you must check this box, if checked this will populate 2 additional options";
        public static string PeerReviewCanStudentAccessReviews = "If you want students (Authors) to be able to access the reviews of their submissions after the peer review deadline, you must check this box.";
        public static string PeerReviewHasStudentCompletedAssignedReviews = "If you want to require students (Authors) to complete their assigned peer reviews before they can access the reviews of their own submission, this box must be checked.";
        public static string PeerReviewCanReviewerViewOthersReviews = "If you want the reviewer to be able to see the reviews made by other reviewers on this submission , you must check this box.";
        public static string PeerReviewHasReviewerCompletedAssignedReviews = "If you want to require the reviewer to complete all of their assigned peer reviews before viewing others reviews of the current submission, this box must be checked.";
        public static string PeerReviewInstructorCompletesRubricRandomReview = "If you want the instructor to complete a rubric for a randomly selected review, you must check this box.";
        public static string PeerReviewInstructorCompletesRubricAllReviews = "If you want the instructor to complete rubrics for all the reviews, you must check this box.";

        #endregion


        #region AuthorRebuttalToolTips

        public static string AuthorRebuttalName = "";
        public static string AuthorRebuttalAbstractAssignmentID = "";
        public static string AuthorRebuttalPresentationOptionsPresentAllIssuesLogged = "If you would like to present all issues logged by team members irrespective of the voting results, you must select this button.";
        public static string AuthorRebuttalPresentationOptionsPresentIssuesXLogged = "If you want to present only issues that recieved at least X number of votes, you must select this button, if selected will populate an additional option. If you select this button you must put an integer in the box to the right.";
        public static string AuthorRebuttalPresentationOptionsPresentIssuesXPercentLogged = "If you want to present only issue that recieved votes from X percent of team members, you must select this button. If you select this button you must put an integer in the box to the right";
        public static string AuthorRebuttalPresentationOptionsPresentOnlyModeratorVoted = "If you only want to present the issues voted on by the moderator, you must select this button.";
        public static string AuthorRebuttalxlogged = "You must enter the minimum number of votes here (Integer Value).";
        public static string AuthorRebuttalxpercent = "You must enter the percent of votes here (Integer Value).";
        public static string AuthorRebuttalAuthorMustAcceptorRefuteEachIssue = "If you want to require the Author to change or refute each issue voted on, you must check this box, if checked will populate an additional option.";
        public static string AuthorRebuttalAuthorMustProvideRationale = "If you want the author to provide a written explanation for each issue they refute, you must check this box.";
        public static string AuthorRebuttalAuthorMustSayIfIssueWasAddressed = "If you want to require the author to specify whether each issue was addressed in the resubmission, you must check this box, if checked will populate an additional option.";
        public static string AuthorRebuttalAuthorMustDescribeHowAddressed = "If you want to require the author to describe how each issue was addressed, this box must be checked.";

        #endregion


        #region IssueVotingToolTips

        public static string IssueVotingName = "";
        public static string IssueVotingAbstractAssignmentID = "";
        public static string IssueVotingSetGradePercentOfIssues = "If you want to grade this activity based on the percent of issue's that were voted on, you must select this button.";
        public static string IssueVotingSetGradePercentAgreementWModerator = "If you would like to grade this activity based on the percent of issues that the moderator found, you must select this button.";
        public static string IssueVotingSetGradeManually = "If you would like to set the grade manually, you must select this button.";
        public static string IssueVotingUseOnlyStudentWhoCompletedPeerReview = "If you want to use only students who completed a peer review of this submission, you must check this box.";
        public static string IssueVotingEnableIssueVotingDiscussion = "If you would like to enable issue voting discussion on this submission, you must check this box, if checked this will populate another option.";
        public static string IssueVotingReviewerMustCompleteIssueVoting = "If you want to require reviewers to have completed issue voting prior to joining a discussion, you must check this box.";

        #endregion

        /*
         * TEMPLATE FOR ADDING TOOLTIPS (FOR ORGANIZATIONAL PURPOSES)
         *
        #region [ViewName]ToolTips

            public static string [ViewName][ToolTipName] = "";

        #endregion
         *
         * 
         * ALSO:
         *     To prevent additional unecessary whitespace in a tooltip, avoid using <p> tags at the beginning or end of the tooltip text.
         * 
         */


    }
}
