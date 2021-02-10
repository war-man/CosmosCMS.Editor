CREATE TABLE [dbo].[Articles] (
    [Id]               INT            IDENTITY (1, 1) NOT NULL,
    [ArticleNumber]    INT            NOT NULL,
    [UrlPath]          NVARCHAR (128) NULL,
    [VersionNumber]    INT            NOT NULL,
    [Published]        DATETIME2 (7)  NULL,
    [Title]            NVARCHAR (254) NULL,
    [Content]          NVARCHAR (MAX) NULL,
    [Updated]          DATETIME2 (7)  DEFAULT (getutcdate()) NOT NULL,
    [StatusCode]       INT            DEFAULT ((0)) NOT NULL,
    [FontIconId]       INT            NULL,
    [FooterJavaScript] NVARCHAR (MAX) NULL,
    [HeaderJavaScript] NVARCHAR (MAX) NULL,
    [LayoutId]         INT            NULL,
    [TeamId]           INT            NULL,
    CONSTRAINT [PK_Articles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Articles_FontIcons_FontIconId] FOREIGN KEY ([FontIconId]) REFERENCES [dbo].[FontIcons] ([Id]),
    CONSTRAINT [FK_Articles_Layouts_LayoutId] FOREIGN KEY ([LayoutId]) REFERENCES [dbo].[Layouts] ([Id]),
    CONSTRAINT [FK_Articles_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Teams] ([Id])
);






GO
CREATE NONCLUSTERED INDEX [IX_Articles_FontIconId]
    ON [dbo].[Articles]([FontIconId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Articles_LayoutId]
    ON [dbo].[Articles]([LayoutId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Articles_UrlPath]
    ON [dbo].[Articles]([UrlPath] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Articles_UrlPath_Published_StatusCode]
    ON [dbo].[Articles]([UrlPath] ASC, [Published] ASC, [StatusCode] ASC) WHERE ([Published] IS NOT NULL);


GO

            CREATE   TRIGGER trg_Article_UpdateDateTime
            ON dbo.Articles
            AFTER UPDATE
            AS
            UPDATE dbo.Articles
            SET Updated = GETUTCDATE()
            WHERE Id IN(SELECT DISTINCT Id FROM Inserted)
GO
CREATE NONCLUSTERED INDEX [IX_Articles_TeamId]
    ON [dbo].[Articles]([TeamId] ASC);

