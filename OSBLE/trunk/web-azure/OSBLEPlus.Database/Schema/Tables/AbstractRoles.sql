CREATE TABLE [dbo].[AbstractRoles] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (50)  NOT NULL,
    [CanModify]      BIT            NOT NULL,
    [CanSeeAll]      BIT            NOT NULL,
    [CanGrade]       BIT            NOT NULL,
    [CanSubmit]      BIT            NOT NULL,
    [Anonymized]     BIT            NOT NULL,
    [CanUploadFiles] BIT            NOT NULL,
    [Discriminator]  NVARCHAR (128) NOT NULL,
    CONSTRAINT [PK_dbo.AbstractRoles] PRIMARY KEY CLUSTERED ([ID] ASC)
);

