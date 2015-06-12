CREATE TABLE [dbo].[Assessments] (
    [ID]               INT            IDENTITY (1, 1) NOT NULL,
    [AssessmentName]   NVARCHAR (MAX) NOT NULL,
    [AssessmentTypeID] INT            NOT NULL,
    CONSTRAINT [PK_dbo.Assessments] PRIMARY KEY CLUSTERED ([ID] ASC)
);

