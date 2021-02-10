CREATE TABLE [dbo].[FontIcons] (
    [Id]       INT            IDENTITY (1, 1) NOT NULL,
    [IconCode] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_FontIcons] PRIMARY KEY CLUSTERED ([Id] ASC)
);

