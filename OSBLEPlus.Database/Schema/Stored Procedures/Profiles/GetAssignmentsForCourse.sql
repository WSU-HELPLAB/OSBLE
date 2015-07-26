CREATE PROCEDURE [dbo].[GetAssignmentsForCourse] @courseId    INT,
                                                 @currentDate DATETIME
AS
  BEGIN
      SET nocount ON;

      -- course assignments
      SELECT Id=a.ID,
             CourseId=a.CourseID,
             AssignmentType = a.AssignmentTypeID,
             NAME = a.AssignmentName,
             [Description]=a.AssignmentDescription,
             a.ReleaseDate,
             a.DueDate
      FROM   [dbo].[Assignments] a
	  INNER JOIN [dbo].[Deliverables] d on a.ID = d.AssignmentID
      WHERE  a.CourseID = @courseId
             AND a.IsDraft = 0
            -- AND a.ReleaseDate <= @currentDate
             AND @currentDate <= a.DueDate
			 AND a.AssignmentTypeID = 1 --Basic assignment


      -- course details
      SELECT Id=a.ID,
             NAME=a.Prefix + ' ' + a.Number + ', ' + a.Semester + ' '
                  + Cast(a.[Year] AS VARCHAR(4)),
             Number = Cast(a.Number AS VARCHAR(32)),
             NamePrefix=a.Prefix,
             a.[Description],
             a.Semester,
             [Year] = Cast(a.[Year] AS VARCHAR(4)),
             a.StartDate,
             a.EndDate
      FROM   [dbo].[AbstractCourses] a
      WHERE  a.ID = @courseId

  END 
