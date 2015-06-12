CREATE TABLE [dbo].[UserProfiles] (
    [ID]                      INT            IDENTITY (1, 1) NOT NULL,
    [UserName]                NVARCHAR (MAX) NULL,
    [Password]                NVARCHAR (MAX) NULL,
    [AuthenticationHash]      NVARCHAR (MAX) NULL,
    [IsApproved]              BIT            NOT NULL,
    [SchoolID]                INT            NOT NULL,
    [FirstName]               NVARCHAR (MAX) NULL,
    [LastName]                NVARCHAR (MAX) NULL,
    [Identification]          NVARCHAR (MAX) NULL,
    [IsAdmin]                 BIT            NOT NULL,
    [CanCreateCourses]        BIT            NOT NULL,
    [DefaultCourse]           INT            NOT NULL,
    [EmailAllNotifications]   BIT            NOT NULL,
    [EmailAllActivityPosts]   BIT            NOT NULL,
    [EmailNewDiscussionPosts] BIT            NOT NULL,
    CONSTRAINT [PK_dbo.UserProfiles] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.UserProfiles_dbo.Schools_SchoolID] FOREIGN KEY ([SchoolID]) REFERENCES [dbo].[Schools] ([ID]) ON DELETE CASCADE
);

