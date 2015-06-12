---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- Merge OSBIDE assignment data into OSBLE assignments
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------

-- Select the targe data into a temp table 
IF Object_id('tempdb..#Assignments') IS NOT NULL
  DROP TABLE #Assignments

SELECT AssignmentTypeId=1,
       AssignmentName=a.NAME,
       AssignmentDescription=c.[Description],
       CourseId=ac.ID,
       a.ReleaseDate,
       a.DueDate,
       IsAnnotatable=0,
       HoursLateWindow=0,
       DeductionPerUnit=0,
       HoursPerDeduction=0,
       IsDraft=0
INTO   #Assignments
FROM   [OSBIDE.HelpLab].dbo.Assignments a
       INNER JOIN [OSBIDE.HelpLab].dbo.Courses c
               ON c.Id = a.CourseId
       INNER JOIN AbstractCourses ac
               ON ac.Prefix = c.Prefix
                  AND ac.Number = c.CourseNumber
                  AND ac.[Year] = c.[Year]
                  AND ac.Semester = c.Season
WHERE  a.IsDeleted = 0

-- Merge data into the target table, only insert when not exist in target table
MERGE [dbo].[Assignments] AS Target
USING #Assignments AS Source
ON ( Target.AssignmentName = Source.AssignmentName
     AND Target.CourseId = Source.CourseId )
WHEN NOT MATCHED BY Target THEN
  INSERT (AssignmentTypeId,
          AssignmentName,
          AssignmentDescription,
          CourseId,
          ReleaseDate,
          DueDate,
          IsAnnotatable,
          HoursLateWindow,
          DeductionPerUnit,
          HoursPerDeduction,
          IsDraft)
  VALUES ( Source.AssignmentTypeId,
           Source.AssignmentName,
           Source.AssignmentDescription,
           Source.CourseId,
           Source.ReleaseDate,
           Source.DueDate,
           Source.IsAnnotatable,
           Source.HoursLateWindow,
           Source.DeductionPerUnit,
           Source.HoursPerDeduction,
           Source.IsDraft ); 

DROP TABLE #Assignments;
