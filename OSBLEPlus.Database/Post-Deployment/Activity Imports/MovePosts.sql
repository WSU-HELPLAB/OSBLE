IF NOT EXISTS(SELECT 1
              FROM   EventLogs a
                     INNER JOIN FeedPostEvents b
                             ON b.EventLogId = a.Id
                                AND b.EventDate = a.EventDate
                                AND ( a.EventTypeId = 7
                                       OR a.EventTypeId = 9 ))
BEGIN

SET IDENTITY_INSERT [dbo].[EventLogs] ON;


INSERT INTO [dbo].[EventLogs]
           ([Id], [EventTypeId], [EventDate], [SenderId])
SELECT Id = a.ID, 
EventTypeId = CASE WHEN a.Parent_ID IS NULL THEN 7 ELSE 9 END,
EventDate = a.Posted,
SenderId = u.UserProfileID
FROM [dbo].[AbstractDashboards] a
INNER JOIN [dbo].[CourseUsers] u
		ON a.CourseUserID = u.ID


SET IDENTITY_INSERT [dbo].[EventLogs] OFF;


INSERT INTO [dbo].[FeedPostEvents]
			([EventLogId], [EventDate], [SolutionName], [Comment])
SELECT EventLogId = a.Id,
EventDate = a.Posted,
SolutionName = '',
Comment = a.Content
FROM [dbo].[AbstractDashboards] a
WHERE Parent_ID IS NULL


INSERT INTO [dbo].[LogCommentEvents]
            ([EventLogId], [SourceEventLogId], [EventDate], [SolutionName], [Content])
SELECT EventLogID=a.Id,
SourceEventLogId=a.parent_ID,
EventDate=a.Posted,
[SolutionName]='',
[Comment]=a.Content
FROM   [dbo].[AbstractDashboards] a
WHERE  a.Parent_ID > 0

END