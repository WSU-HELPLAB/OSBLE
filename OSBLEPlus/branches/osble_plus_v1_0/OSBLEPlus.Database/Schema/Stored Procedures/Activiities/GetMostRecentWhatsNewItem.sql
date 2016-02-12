CREATE PROCEDURE [dbo].[GetMostRecentWhatsNewItem] @courseId INT
AS
  BEGIN
      SET nocount ON;

      SELECT TOP 1 Id, DatePosted, NewsHeader, Content
      FROM   [dbo].[WhatsNewItems] a
	  ORDER BY a.DatePosted DESC

  END

GO 
