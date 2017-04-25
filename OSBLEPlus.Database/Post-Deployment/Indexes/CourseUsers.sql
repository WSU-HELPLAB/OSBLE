CREATE NONCLUSTERED INDEX IX_CourseUsers_Role
  ON [dbo].[CourseUsers] ([AbstractRoleID])
  INCLUDE ([AbstractCourseID], [UserProfileID])

GO

;