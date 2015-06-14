namespace OSBLEPlus.Logic.DataAccess.Profiles
{
    public class UserQuery
    {
        public const string SelectByUserId =
@"SELECT [ID] AS UserId
    ,[UserName] AS Email
    ,[Password]
    ,[AuthenticationHash]
    ,[IsApproved]
    ,[SchoolID]
    ,[FirstName]
    ,[LastName]
    ,[Identification]
    ,[IsAdmin]
    ,[CanCreateCourses]
    ,[DefaultCourse] AS DefaultCourseId
    ,[EmailAllNotifications]
    ,[EmailAllActivityPosts]
    ,[EmailNewDiscussionPosts]
FROM [dbo].[UserProfiles]
WHERE ID = @Id";

        public const string SelectByUserName =
@"SELECT [ID] AS UserId
    ,[UserName] AS Email
    ,[Password]
    ,[AuthenticationHash]
    ,[IsApproved]
    ,[SchoolID]
    ,[FirstName]
    ,[LastName]
    ,[Identification]
    ,[IsAdmin]
    ,[CanCreateCourses]
    ,[DefaultCourse] AS DefaultCourseId
    ,[EmailAllNotifications]
    ,[EmailAllActivityPosts]
    ,[EmailNewDiscussionPosts]
FROM [dbo].[UserProfiles]
WHERE UserName = @UserName";
    }

    public class CourseQuery
    {
        public const string SelectById =
 @"SELECT [ID] AS CourseId
      ,[Name]
      ,[Prefix] AS NamePrefix
      ,[Number]
      ,[Semester]
      ,[Year]
      ,[StartDate]
      ,[EndDate]
      ,[Description]
FROM [dbo].[AbstractCourses]
WHERE ID=@Id AND IsDeleted=0";
    }
}
