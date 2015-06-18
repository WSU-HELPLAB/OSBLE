/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
-- static data
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
:r "Static Data\AssignmentTypes.sql"
:r "Static Data\AbstractRoles.sql"
:r "Static Data\EventTypes.sql"
:r "Static Data\Schools.sql"

-- Move OSBLE dashboard posts to osbide
--:r "Activity Imports\MovePosts.sql"

--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
-- seeds
------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
:r "Seeds\Courses.sql"
:r "Seeds\UserProfiles.sql"
:r "Seeds\CourseUsers.sql"
:r "Seeds\EventLogs2014Jan1.sql"
:r "Seeds\EventLogs2014Jan2.sql"
:r "Seeds\EventLogs2014Jan3.sql"
:r "Seeds\EventLogs2014Jan4.sql"
:r "Seeds\EventLogs2014Jan5.sql"
:r "Seeds\EventLogs2014Jan6.sql"
:r "Seeds\EventLogs2014Feb1.sql"
:r "Seeds\EventLogs2014Feb2.sql"
:r "Seeds\EventLogs2014Feb3.sql"
:r "Seeds\EventLogs2014Feb4.sql"
:r "Seeds\EventLogs2014Feb5.sql"
:r "Seeds\EventLogs2014Feb6.sql"
:r "Seeds\EventLogs2014Feb7.sql"
:r "Seeds\EventLogs2014Feb8.sql"
:r "Seeds\EventLogs2014Feb9.sql"
:r "Seeds\DependencyEvents.sql"

--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
-- merge data from OSBIDE
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
-- the deployment takes 12+ minute with these events
--:r "Activity Imports\01_Assignments.sql"
--:r "Activity Imports\02_Course User and Roles.sql"
--:r "Activity Imports\03_Import OSBIDE EventLogs.sql"
--:r "Activity Imports\04_AskForHelpEvents.sql"
--:r "Activity Imports\05_BuildEvents.sql"
--:r "Activity Imports\14_ErrorTypes.sql"
--:r "Activity Imports\15_BuildErrors.sql"
--:r "Activity Imports\16_SaveEvents.sql"
--:r "Activity Imports\17_SubmitEvents.sql"
--:r "Activity Imports\18_CutCopyPasteEvents.sql"
--:r "Activity Imports\19_DebugEvents.sql"
--:r "Activity Imports\20_EditorActivityEvents.sql"
--:r "Activity Imports\21_ExceptionEvents.sql"
--:r "Activity Imports\22_FeedPostEvents.sql"
--:r "Activity Imports\23_LogCommentEvents.sql"
--:r "Activity Imports\24_HelpfulMarkGivenEvents.sql"
--:r "Activity Imports\25_EventLogSubscriptions.sql"

-- large slow table contents for EQ calculations
-- the deployment takes 42+ minute to finish with below contents
--:r "Activity Imports\06_CodeDocuments.sql"
--:r "Activity Imports\07_BuildDocuments.sql"
--:r "Activity Imports\08_BreakPoints.sql"
--:r "Activity Imports\09_BuildEventBreakPoints.sql"
--:r "Activity Imports\10_CodeDocumentBreakPoints.sql"
--:r "Activity Imports\11_ErrorListItems.sql"
--:r "Activity Imports\12_BuildEventErrorListItems.sql"
--:r "Activity Imports\13_CodeDocumentErrorListItems.sql"

--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
-- create indexes
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
:r "Indexes\Events.sql"
:r "Indexes\CourseUsers.sql"
