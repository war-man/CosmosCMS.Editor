CREATE TABLE [dbo].[Templates] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Title]       NVARCHAR (128) NULL,
    [Description] NVARCHAR (MAX) NULL,
    [Content]     NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_Templates] PRIMARY KEY CLUSTERED ([Id] ASC)
);

