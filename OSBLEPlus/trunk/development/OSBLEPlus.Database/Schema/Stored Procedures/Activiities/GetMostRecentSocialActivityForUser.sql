CREATE PROCEDURE [dbo].[GetMostRecentSocialActivityForUser] @userId INT
AS
  BEGIN
      SET nocount ON;

      SELECT TOP 1 a.EventDate
      FROM   [dbo].[EventLogs] a
             INNER JOIN [dbo].[EventTypes] b
                     ON b.EventTypeId = a.EventTypeId
                        AND b.IsSocialEvent=1
      WHERE  a.SenderId=@userId
	  ORDER BY a.EventDate DESC

  END

GO 
