CREATE TABLE [dbo].[FeedPostUserTags] (
    [ID]				INT IDENTITY (1, 1) NOT NULL,
	[FeedPostID]		INT NOT NULL,
	[UserID]			INT NOT NULL,
	[Viewed]			BIT NOT NULL,
	CONSTRAINT [PK_dbo.FeedPostUserTags] PRIMARY KEY CLUSTERED ([ID] ASC),
	CONSTRAINT [FK_dbo.FeedPostUserTags_FeedPostEvents] FOREIGN KEY([FeedPostID]) REFERENCES [dbo].[FeedPostEvents] ([ID]),
	CONSTRAINT [FK_dbo.FeedPostUserTags_UserProfiles] FOREIGN KEY([FeedPostID]) REFERENCES [dbo].[UserProfiles] ([ID]),
)
