CREATE PROCEDURE [dbo].[GetMostRecentSocialActivityForUser] @userId INT
AS
  BEGIN
      SET nocount ON;

      SELECT EventLogId=a.Id, a.SenderId, a.EventDate, a.EventTypeId
      FROM   [dbo].[EventLogs] a
             INNER JOIN [dbo].[EventTypes] b
                     ON b.EventTypeId = a.EventTypeId
                        AND b.IsSocialEvent=1
      WHERE  a.SenderId=@userId

  END

GO 
