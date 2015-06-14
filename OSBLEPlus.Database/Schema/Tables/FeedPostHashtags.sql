CREATE TABLE [dbo].[FeedPostHashtags] (
    [ID]				INT IDENTITY (1, 1) NOT NULL,
	[FeedPostID]		INT NOT NULL,
	[HashtagID]			INT NOT NULL,
	CONSTRAINT [PK_dbo.FeedPostHashtags] PRIMARY KEY CLUSTERED ([ID] ASC),
	CONSTRAINT [FK_dbo.FeedPostHashtags_FeedPostEvents] FOREIGN KEY([FeedPostID]) REFERENCES [dbo].[FeedPostEvents] ([ID]),
	CONSTRAINT [FK_dbo.FeedPostHashtags_Hashtags] FOREIGN KEY([HashtagID]) REFERENCES [dbo].[HashTags] ([ID])
)
