-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE SubmitEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical SubmitEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='SubmitEvents')
BEGIN

	CREATE TABLE SubmitEvents
	  (
		 Id				INT IDENTITY NOT NULL,
		 EventLogId		INT NOT NULL,
		 EventDate		DATETIME NOT NULL,
		 SolutionName	VARCHAR(MAX) NOT NULL,
		 AssignmentId	INT NOT NULL,
		 SolutionData	IMAGE NOT NULL,
		 CONSTRAINT PK_SubmitEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_SubmitEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
		 CONSTRAINT FK_SubmitEvents_Assignments FOREIGN KEY (AssignmentId) REFERENCES Assignments(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#SubmitEvents') IS NOT NULL
    DROP TABLE #SubmitEvents

SELECT	a.Id,
		a.EventLogId,
		a.EventDate,
		a.SolutionName,
		AssignmentId = d.ID,
		a.SolutionData
INTO #SubmitEvents
FROM [OSBIDE.Helplab].dbo.SubmitEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId
INNER JOIN [OSBIDE.Helplab].dbo.Assignments e ON e.Id = a.AssignmentId
INNER JOIN [OSBIDE.HelpLab].dbo.Courses c ON c.Id = e.CourseId
INNER JOIN dbo.AbstractCourses ac ON ac.Prefix = c.Prefix
                  AND ac.Number = c.CourseNumber
                  AND ac.[Year] = c.[Year]
                  AND ac.Semester = c.Season
INNER JOIN dbo.Assignments d ON d.CourseID = ac.ID AND d.AssignmentName = e.Name

-- Merge data into the target table, only insert
SET IDENTITY_INSERT [dbo].[SubmitEvents] ON
MERGE [dbo].[SubmitEvents] AS Target
USING [#SubmitEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, EventDate, SolutionName, AssignmentId, SolutionData)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.EventDate,
		Source.SolutionName,
		Source.AssignmentId,
		Source.SolutionData
	);
SET IDENTITY_INSERT [dbo].[SubmitEvents] OFF
