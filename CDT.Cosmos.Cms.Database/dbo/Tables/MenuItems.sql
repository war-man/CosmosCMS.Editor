CREATE TABLE [dbo].[MenuItems] (
    [Id]          INT              IDENTITY (1, 1) NOT NULL,
    [MenuText]    NVARCHAR (100)   NULL,
    [Url]         NVARCHAR (256)   NULL,
    [IconCode]    NVARCHAR (256)   NULL,
    [ParentId]    INT              NULL,
    [ArticleId]   INT              NULL,
    [SortOrder]   INT              DEFAULT ((0)) NOT NULL,
    [HasChildren] BIT              DEFAULT (CONVERT([bit],(0))) NOT NULL,
    [Guid]        UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MenuItems_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles] ([Id]),
    CONSTRAINT [FK_MenuItems_MenuItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[MenuItems] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_MenuItems_ArticleId]
    ON [dbo].[MenuItems]([ArticleId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_MenuItems_ParentId]
    ON [dbo].[MenuItems]([ParentId] ASC);

