CREATE PROCEDURE [dbo].[GetLastSubmitDateForAssignment] @assignmentId INT, @userId INT
AS
  BEGIN
      SET nocount ON;

      SELECT MAX (a.EventDate)
      FROM   [dbo].[SubmitEvents] a
	  INNER JOIN [dbo].[EventLogs] b ON b.Id=a.EventLogId AND b.SenderId=@userId
      WHERE  a.AssignmentId=@assignmentId

  END 
