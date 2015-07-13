-------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------
-- sproc [GetStudentList]
-------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------
create procedure [dbo].[GetStudentList]

@courseId int

as
begin

	set nocount on;
	select  u.ID, u.FirstName, u.LastName
	from [dbo].[UserProfiles] u
	inner join [dbo].[CourseUsers] c on c.UserProfileID = u.ID
	where c.AbstractCourseID = @courseId 
	-- and AbstractRoleID = 3
end

