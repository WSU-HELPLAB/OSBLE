-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE EventLogs
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical EventLogs table if not exits
IF NOT EXISTS(SELECT 1
              FROM   sys.tables
              WHERE  NAME = 'EventLogs')
  BEGIN
      CREATE TABLE EventLogs
        (
           Id          INT IDENTITY,
           EventTypeId INT NOT NULL,
           EventDate   DATETIME NOT NULL,
           DateReceived DATETIME NOT NULL,
           SenderId    INT NOT NULL,
           CONSTRAINT PK_EventLogs_Id PRIMARY KEY CLUSTERED (Id),
           CONSTRAINT FK_EventLogs_EventTypes FOREIGN KEY (EventTypeId) REFERENCES EventTypes(EventTypeId),
           CONSTRAINT FK_EventLogs_UserProfiles FOREIGN KEY (SenderId) REFERENCES UserProfiles(Id)
        )
  END

-- Select the targe data into a temp table 
IF Object_id('tempdb..#EventLogs') IS NOT NULL
  DROP TABLE #EventLogs

SELECT l.Id,
       l.EventTypeId,
       v.EventDate,
       CreatedDate=l.DateReceived,
       b.Id AS SenderId
INTO   #EventLogs
FROM   [OSBIDE.HelpLab].dbo.EventLogs l
       INNER JOIN [OSBIDE.HelpLab].dbo.EventTimeView v
               ON v.EventLogId = l.Id
       INNER JOIN [OSBIDE.HelpLab].dbo.OsbideUsers a
               ON a.Id = l.SenderId
       INNER JOIN dbo.UserProfiles b
               ON b.Identification = Cast(a.InstitutionID AS VARCHAR(32))
                  AND b.SchoolID = 1
                  --- OSBLE data is not clean
                  AND b.UserName <> 'deleted'
                  AND b.ID <> 955

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[EventLogs] ON

MERGE [dbo].[EventLogs] AS Target
USING [#EventLogs] AS Source
ON ( Target.[Id] = Source.[Id] )
WHEN NOT MATCHED BY Target THEN
  INSERT ([Id],
          [EventTypeId],
          [EventDate],
          [DateReceived],
          [SenderId])
  VALUES ( Source.[Id],
           Source.[EventTypeId],
           Source.[EventDate],
		   Source.[CreatedDate],
           Source.[SenderId] )
WHEN NOT MATCHED BY Source THEN
  DELETE;

SET IDENTITY_INSERT [dbo].[EventLogs] OFF

-- Clean up
DROP TABLE [#EventLogs] 
