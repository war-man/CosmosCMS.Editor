CREATE TABLE [dbo].[Teams] (
    [Id]              INT             IDENTITY (1, 1) NOT NULL,
    [TeamName]        NVARCHAR (64)   NULL,
    [TeamDescription] NVARCHAR (1024) NULL,
    CONSTRAINT [PK_Teams] PRIMARY KEY CLUSTERED ([Id] ASC)
);

