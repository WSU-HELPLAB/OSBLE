﻿CREATE TABLE [dbo].[AbstractCourses] (
    [ID]                                       INT            IDENTITY (1, 1) NOT NULL,
    [Name]                                     NVARCHAR (100) NOT NULL,
    [IsDeleted]                                BIT            NOT NULL,
    [AllowDashboardPosts]                      BIT            NOT NULL,
    [CalendarWindowOfTime]                     INT            NOT NULL,
    [Prefix]                                   NVARCHAR (8)   NULL,
    [Number]                                   NVARCHAR (8)   NULL,
    [Semester]                                 NVARCHAR (8)   NULL,
    [Year]                                     NVARCHAR (4)   NULL,
    [AllowDashboardReplies]                    BIT            NULL,
    [AllowEventPosting]                        BIT            NULL,
    [RequireInstructorApprovalForEventPosting] BIT            NULL,
    [Inactive]                                 BIT            NULL,
    [StartDate]                                DATETIME       NULL,
    [EndDate]                                  DATETIME       NULL,
    [MinutesLateWithNoPenalty]                 INT            NULL,
    [PercentPenalty]                           INT            NULL,
    [HoursLatePerPercentPenalty]               INT            NULL,
    [HoursLateUntilZero]                       INT            NULL,
    [TimeZoneOffset]                           INT            NULL,
    [ShowMeetings]                             BIT            NULL,
    [Description]                              NVARCHAR (100) NULL,
    [StartDate1]                               DATETIME       NULL,
    [Description1]                             NVARCHAR (100) NULL,
    [Nickname]                                 NVARCHAR (10)  NULL,
    [Discriminator]                            NVARCHAR (128) NOT NULL,
    CONSTRAINT [PK_dbo.AbstractCourses] PRIMARY KEY CLUSTERED ([ID] ASC)
);
