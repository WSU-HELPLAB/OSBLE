-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE EventLogSubscriptions
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical EventLogSubscriptions table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='EventLogSubscriptions')
BEGIN

	CREATE TABLE EventLogSubscriptions
	  (
		 UserId		INT NOT NULL,
		 LogId		INT NOT NULL,
		 CONSTRAINT PK_EventLogSubscriptions PRIMARY KEY CLUSTERED (UserId, LogId),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#EventLogSubscriptions') IS NOT NULL
    DROP TABLE #EventLogSubscriptions

SELECT	a.UserId,
		a.LogId
INTO	#EventLogSubscriptions
FROM [OSBIDE.Helplab].dbo.EventLogSubscriptions a
INNER JOIN [OSBIDE.HelpLab].dbo.OsbideUsers u
				ON u.Id = a.UserId
		INNER JOIN dbo.UserProfiles b
				ON b.Identification = CAST(u.InstitutionID AS VARCHAR(32))
			   AND b.SchoolID = 1
INNER JOIN dbo.EventLogs c ON c.Id = a.LogId

-- Merge data into the target table, only insert or delete records
MERGE [dbo].[EventLogSubscriptions] AS Target
USING [#EventLogSubscriptions] AS Source ON (Target.LogId = Source.LogId AND Target.UserId = Source.UserId)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (UserId, LogId)
	VALUES (UserId, LogId)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
