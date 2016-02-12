CREATE PROCEDURE [dbo].[GetCourses]
AS
  BEGIN
      SET nocount ON;

      SELECT Value=a.ID,
             NAME=a.Prefix + ' ' + a.Number + ', ' + a.Semester + ' '
                  + Cast(a.[Year] AS VARCHAR(4))
      FROM   [dbo].[AbstractCourses] a
  END

go 
