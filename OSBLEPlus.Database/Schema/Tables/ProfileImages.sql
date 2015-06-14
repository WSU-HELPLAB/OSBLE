CREATE TABLE [dbo].[ProfileImages] (
    [UserID]  INT   NOT NULL,
    [Picture] IMAGE NULL,
    CONSTRAINT [PK_dbo.ProfileImages] PRIMARY KEY CLUSTERED ([UserID] ASC),
    CONSTRAINT [FK_dbo.ProfileImages_dbo.UserProfiles_UserID] FOREIGN KEY ([UserID]) REFERENCES [dbo].[UserProfiles] ([ID]) ON DELETE CASCADE
);

