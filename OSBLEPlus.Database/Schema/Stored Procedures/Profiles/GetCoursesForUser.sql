CREATE PROCEDURE [dbo].[GetCoursesForUser] @userId      INT,
                                           @currentDate DATETIME
AS
  BEGIN
      SET nocount ON;

      SELECT CourseId=a.ID,
             Name=a.Prefix + ' ' + a.Number + ', ' + a.Semester + ' '
                  + Cast(a.[Year] AS VARCHAR(4)),
             Number = Cast(a.Number AS VARCHAR(32)),
             NamePrefix=a.Prefix,
             a.[Description],
             a.Semester,
             [Year] = Cast(a.[Year] AS VARCHAR(4)),
             a.StartDate,
             a.EndDate
      FROM   [dbo].[AbstractCourses] a
             INNER JOIN [dbo].CourseUsers b
                     ON b.AbstractCourseID = a.ID
                        AND b.Hidden = 0
      WHERE  a.IsDeleted = 0
             AND a.Inactive = 0
             AND a.StartDate <= @currentDate
             AND @currentDate <= a.EndDate
			 AND UserProfileID = @userId
  END

GO 
