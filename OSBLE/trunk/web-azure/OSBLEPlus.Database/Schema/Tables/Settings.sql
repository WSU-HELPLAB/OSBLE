CREATE TABLE [dbo].[Settings] (
    [ID]    INT            IDENTITY (1, 1) NOT NULL,
    [Key]   NVARCHAR (MAX) NOT NULL,
    [Value] NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_dbo.Settings] PRIMARY KEY CLUSTERED ([ID] ASC)
);

