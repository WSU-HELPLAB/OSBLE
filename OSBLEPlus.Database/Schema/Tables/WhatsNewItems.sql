CREATE TABLE [dbo].[WhatsNewItems]
  (
     [Id]         INT IDENTITY(1, 1) NOT NULL,
     [DatePosted] DATETIME NOT NULL,
     [NewsHeader] VARCHAR(MAX) NOT NULL,
     [Content]    VARCHAR(MAX) NOT NULL,
     CONSTRAINT [PK_WhatsNewItems] PRIMARY KEY CLUSTERED (Id)
  ) 
