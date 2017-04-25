CREATE PROCEDURE [dbo].[GetAssignmentTeam] @assignmentId    INT,
                                            @userId INT
AS
  BEGIN
      SET nocount ON;

      -- course assignments
      SELECT a.TeamId
      FROM   [dbo].[AssignmentTeams] a
	  INNER JOIN [dbo].[TeamMembers] b on b.TeamID = a.TeamID
	  INNER JOIN [dbo].[CourseUsers] c on c.ID = b.CourseUserID
	  INNER JOIN [dbo].[Assignments] d on c.AbstractCourseID = d.CourseID
	  WHERE a.AssignmentID = @assignmentId AND c.UserProfileID = @userId AND d.ID = @assignmentId
  END 
